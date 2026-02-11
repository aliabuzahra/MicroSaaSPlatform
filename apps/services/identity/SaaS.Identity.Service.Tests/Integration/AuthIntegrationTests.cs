namespace SaaS.Identity.Service.Tests.Integration;

[Collection("IdentityIntegrationTests")]
public class AuthIntegrationTests
{
    private readonly SharedTestContext _factory;

    public AuthIntegrationTests(SharedTestContext factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new 
        { 
            Email = $"test_{Guid.NewGuid()}@example.com", 
            Password = "Password123!", 
            FullName = "Test User",
            TenantId = Guid.NewGuid()
        };

        // Act
        var response = await client.PostAsJsonAsync("/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
