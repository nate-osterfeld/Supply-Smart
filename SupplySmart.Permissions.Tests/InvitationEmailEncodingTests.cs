using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using SupplySmart.Domain.Services;

namespace SupplySmart.Tests;

public class InvitationEmailEncodingTests
{
    [Fact]
    public async Task InvitationNameCannotInjectALinkIntoTheSentEmail()
    {
        const string name = "</p><a href=\"https://phish.example.test\">Reset password</a><p>";
        var body = await CaptureInvitation(name, "person@example.test", "ValidPassword42!");

        Assert.DoesNotContain(name, body);
        Assert.Contains("&lt;/p&gt;&lt;a href=&quot;https://phish.example.test&quot;&gt;Reset password&lt;/a&gt;&lt;p&gt;", body);
        Assert.Contains("Access SupplySmart", body);
        Assert.Contains("<strong>ValidPassword42!</strong>", body);
    }

    [Fact]
    public async Task InvitationDisplaysCredentialsAndNamesAsLiteralText()
    {
        var body = await CaptureInvitation("Taylor & Team", "person&team@example.test", "Aa&copy;'<pw>");

        Assert.Contains("Hello Taylor &amp; Team,", body);
        Assert.Contains("<strong>person&amp;team@example.test</strong>", body);
        Assert.Contains("<strong>Aa&amp;copy;&#39;&lt;pw&gt;</strong>", body);
        Assert.Contains("href='https://supplysmart.example.test/login?team=one&amp;next=two'", body);
    }

    // Capture SMTP on loopback only, so this exercises the actual message that is sent.
    private static async Task<string> CaptureInvitation(string name, string email, string password)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var capture = ReceiveMessage(listener, timeout.Token);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EmailSettings:Host"] = "127.0.0.1",
            ["EmailSettings:Port"] = port.ToString(),
            ["EmailSettings:BaseUrl"] = "https://supplysmart.example.test/login?team=one&next=two"
        }).Build();

        await new EmailService(configuration).SendInviteEmailAsync(email, name, password).WaitAsync(timeout.Token);
        var message = await capture;
        var separator = message.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        Assert.True(separator >= 0, "Expected MIME headers and a body.");
        var headers = message[..separator];
        var body = message[(separator + 4)..];
        if (headers.Contains("Content-Transfer-Encoding: base64", StringComparison.OrdinalIgnoreCase))
            return Encoding.UTF8.GetString(Convert.FromBase64String(body));
        if (headers.Contains("Content-Transfer-Encoding: quoted-printable", StringComparison.OrdinalIgnoreCase))
        {
            body = body.Replace("=\r\n", "");
            body = Regex.Replace(body, "=([0-9A-Fa-f]{2})", match => ((char)Convert.ToByte(match.Groups[1].Value, 16)).ToString());
        }
        return body;
    }

    private static async Task<string> ReceiveMessage(TcpListener listener, CancellationToken cancellationToken)
    {
        using var socket = await listener.AcceptTcpClientAsync(cancellationToken);
        await using var stream = socket.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
        await writer.WriteLineAsync("220 localhost test SMTP");
        var message = new StringBuilder();
        var inData = false;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (inData && line != ".")
            {
                message.Append(line.StartsWith("..") ? line[1..] : line).Append("\r\n");
                continue;
            }
            if (inData) { inData = false; await writer.WriteLineAsync("250 accepted"); }
            else if (line == "DATA") { inData = true; await writer.WriteLineAsync("354 send message"); }
            else if (line == "QUIT") { await writer.WriteLineAsync("221 goodbye"); break; }
            else await writer.WriteLineAsync("250 OK");
        }
        return message.ToString();
    }
}
