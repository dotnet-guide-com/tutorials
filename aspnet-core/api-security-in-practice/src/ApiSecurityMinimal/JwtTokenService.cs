using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ApiSecurityMinimal;

public interface IJwtTokenService
{
    string CreateToken(DemoUser user);
}

public sealed class JwtTokenService(
    IConfiguration configuration) : IJwtTokenService
{
    private const int TokenExpirationMinutes = 30;

    public string CreateToken(DemoUser user)
    {
        string? keyString = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured. " +
                "Set Jwt:Key via user-secrets or environment variable.");

        if (keyString.Length < 32)
            throw new InvalidOperationException(
                "JWT signing key must be at least 32 bytes.");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyString));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Name, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}