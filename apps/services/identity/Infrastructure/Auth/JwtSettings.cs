namespace SaaS.Identity.Service.Infrastructure.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "SaaS.Identity";
    public string Audience { get; set; } = "SaaS.Platform";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
