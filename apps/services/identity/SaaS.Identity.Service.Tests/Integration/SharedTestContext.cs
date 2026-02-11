using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SaaS.Identity.Service.Tests.Integration;

public class SharedTestContext : WebApplicationFactory<Program>, IAsyncLifetime
{
    public Task InitializeAsync()
    {
        // Perform async initialization here (e.g., DB migration) unique to the suite
        return Task.CompletedTask;
    }

    public new Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

[CollectionDefinition("IdentityIntegrationTests")]
public class SharedTestContextCollection : ICollectionFixture<SharedTestContext>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
