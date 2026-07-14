using SaaS.Identity.Service.Domain.Entities;

namespace SaaS.Identity.Service.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token);
}
