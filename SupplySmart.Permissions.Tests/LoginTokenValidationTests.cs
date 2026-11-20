using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.Tokens;
using SupplySmart.Integrations.DbContexts;

namespace SupplySmart.Tests;

public class LoginTokenValidationTests
{
    [Theory]
    [InlineData("unsigned")]
    [InlineData("wrong-key")]
    [InlineData("expired")]
    [InlineData("tampered-payload")]
    [InlineData("legacy-without-stamp")]
    public async Task InvalidTokensCannotAuthenticate(string kind)
    {
        using var app = new SettingsApp();
        using var client = await app.SignIn(1);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/settings/users/")).StatusCode);
        var handler = new JwtSecurityTokenHandler();
        var issued = handler.ReadJwtToken(client.DefaultRequestHeaders.Authorization!.Parameter);
        var claims = issued.Claims.Where(c => c.Type is not ("exp" or "nbf" or "iat"));
        if (kind == "legacy-without-stamp") claims = claims.Where(c => c.Type != "SecurityStamp");
        var secret = kind == "wrong-key" ? new string('x', 64) : SettingsApp.JwtSecret;
        var signing = kind == "unsigned" ? null : new SigningCredentials(
            new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)), SecurityAlgorithms.HmacSha256);
        var altered = new JwtSecurityToken(claims: claims, notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddHours(kind == "expired" ? -1 : 1), signingCredentials: signing);
        var token = handler.WriteToken(altered);
        if (kind == "tampered-payload")
        {
            var parts = token.Split('.');
            altered.Payload["nonce"] = "changed-without-resigning";
            parts[1] = Base64UrlEncoder.Encode(altered.Payload.SerializeToJson());
            token = string.Join('.', parts);
        }
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/settings/users/")).StatusCode);
    }

    [Fact]
    public void SqlServerModelAndSecurityStampMigrationAreConsistentWithoutConnecting()
    {
        using var db = new SupplySmartDbContext(new DbContextOptionsBuilder<SupplySmartDbContext>().UseSqlServer().Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var migration = Assert.Single(db.Database.GetMigrations(), name => name.EndsWith("_AddUserSecurityStamp"));
        var sql = db.GetService<IMigrator>().GenerateScript("20261111030044_AddNotificationTemplates", migration);
        Assert.Contains("ADD [SecurityStamp] uniqueidentifier NOT NULL", sql);
        Assert.Contains("NEWID()", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
    }
}
