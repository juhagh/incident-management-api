using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth;

public class TokenService : ITokenService
{
    private readonly string _jwtKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;
    
    public TokenService(IConfiguration config)
    {
        _jwtKey = config.GetValue<string>("Jwt:Key")
                  ?? throw new InvalidOperationException("JWT key not configured");
        _issuer = config.GetValue<string>("Jwt:Issuer")
                  ?? throw new InvalidOperationException("JWT issuer not configured");
        _audience = config.GetValue<string>("Jwt:Audience")
                    ?? throw new InvalidOperationException("JWT audience not configured");
        _accessTokenMinutes = config.GetValue<int>("Jwt:AccessTokenMinutes", 10);
    }
    
    public string GenerateToken(User user)
    {
        IEnumerable<Claim> claims = new List<Claim> { 
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };
        
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtKey));
        
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
            
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}