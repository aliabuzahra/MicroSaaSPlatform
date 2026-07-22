using SaaS.Identity.Service.Domain.Entities;

namespace SaaS.Identity.Service.Infrastructure.Auth;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token);
}
