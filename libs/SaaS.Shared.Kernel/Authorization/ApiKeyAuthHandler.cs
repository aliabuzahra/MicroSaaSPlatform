using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SaaS.Shared.Kernel.Authorization;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string HeaderName { get; set; } = "X-API-Key";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeyValidator _apiKeyValidator;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator apiKeyValidator)
        : base(options, logger, encoder)
    {
        _apiKeyValidator = apiKeyValidator;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyHeaderValues.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        var validationResult = await _apiKeyValidator.ValidateAsync(apiKey);
        if (!validationResult.IsValid)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, validationResult.ApiKeyId.ToString()),
            new("tenant_id", validationResult.TenantId.ToString()),
            new("api_key_name", validationResult.KeyName ?? ""),
            new(ClaimTypes.AuthenticationMethod, "ApiKey")
        };

        foreach (var scope in validationResult.Scopes ?? new List<string>())
        {
            claims.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}

public interface IApiKeyValidator
{
    Task<ApiKeyValidationResult> ValidateAsync(string apiKey);
}

public record ApiKeyValidationResult(
    bool IsValid,
    Guid ApiKeyId = default,
    Guid TenantId = default,
    string? KeyName = null,
    List<string>? Scopes = null)
{
    public static ApiKeyValidationResult Invalid() => new(false);
    
    public static ApiKeyValidationResult Valid(Guid apiKeyId, Guid tenantId, string? keyName, List<string> scopes) 
        => new(true, apiKeyId, tenantId, keyName, scopes);
}

public class NoOpApiKeyValidator : IApiKeyValidator
{
    public Task<ApiKeyValidationResult> ValidateAsync(string apiKey)
    {
        return Task.FromResult(ApiKeyValidationResult.Invalid());
    }
}

public static class ApiKeyAuthExtensions
{
    public static AuthenticationBuilder AddApiKeyAuthentication(
        this AuthenticationBuilder builder,
        Action<ApiKeyAuthenticationOptions>? configure = null)
    {
        builder.Services.AddSingleton<IApiKeyValidator, NoOpApiKeyValidator>();
        
        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationOptions.DefaultScheme,
            configure ?? (_ => { }));
    }
}
