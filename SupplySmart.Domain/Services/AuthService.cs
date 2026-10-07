using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SupplySmart.Integrations.Models.Data;
using SupplySmart.Integrations.Models.DTO;
using SupplySmart.Integrations.Repositories;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SupplySmart.Domain.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<List<LocationDto>> GetLocationsForUserAsync(int organizationId);
    }

    public class AuthService : IAuthService
    {
        private readonly IBaseRepository<Organization> _orgRepo;
        private readonly IBaseRepository<User> _userRepo;
        private readonly IBaseRepository<Location> _locationRepo;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

        public AuthService(
            IBaseRepository<Organization> orgRepo,
            IBaseRepository<User> userRepo,
            IBaseRepository<Location> locationRepo,
            IConfiguration configuration)
        {
            _orgRepo = orgRepo;
            _userRepo = userRepo;
            _locationRepo = locationRepo;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userRepo.GetByConditionAsync(u => u.Email == dto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            // 1. Create Organization Tenant
            var organization = new Organization { Name = dto.OrganizationName };
            await _orgRepo.AddAsync(organization);

            // 2. Automatically seed default primary location
            var defaultLocation = new Location
            {
                OrganizationId = organization.Id,
                Name = "Main Facility",
                IsPrimary = true
            };
            await _locationRepo.AddAsync(defaultLocation);

            // 3. Create Root Admin User
            var user = new User
            {
                OrganizationId = organization.Id,
                FullName = dto.AdminFullName,
                Email = dto.Email,
                Role = UserRole.Admin
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
            await _userRepo.AddAsync(user);

            // 4. Generate JWT Token
            var token = GenerateJwtToken(user, organization.Id);
            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Role = user.Role.ToString(),
                OrganizationId = organization.Id,
                Locations = new List<LocationDto>
                {
                    new LocationDto { Id = defaultLocation.Id, Name = defaultLocation.Name, IsPrimary = defaultLocation.IsPrimary }
                }
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userRepo.GetByConditionAsync(u => u.Email == dto.Email, includes: new[] { "Organization" });
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            var locationsList = await _locationRepo.GetAllByConditionAsync(l => l.OrganizationId == user.OrganizationId);
            var locationDtos = locationsList.Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                IsPrimary = l.IsPrimary
            }).ToList();

            var token = GenerateJwtToken(user, user.OrganizationId);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Role = user.Role.ToString(),
                OrganizationId = user.OrganizationId,
                Locations = locationDtos
            };
        }

        public async Task<List<LocationDto>> GetLocationsForUserAsync(int organizationId)
        {
            var locations = await _locationRepo.GetAllByConditionAsync(l => l.OrganizationId == organizationId);
            return locations.Select(l => new LocationDto
            {
                Id = l.Id,
                Name = l.Name,
                IsPrimary = l.IsPrimary
            }).ToList();
        }

        private string GenerateJwtToken(User user, int organizationId)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");
            var key = Encoding.ASCII.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("OrganizationId", organizationId.ToString()),
                new Claim(JwtRegisteredClaimNames.GivenName, user.FullName)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}