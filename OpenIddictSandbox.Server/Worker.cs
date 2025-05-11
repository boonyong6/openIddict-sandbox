using OpenIddict.Abstractions;
using OpenIddictSandbox.DataContext;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddictSandbox.Server;

public class Worker : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public Worker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        // Seed the database with a default scope.
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        string customScopeName = "demo_api";

        object? openIddictScope = await scopeManager.FindByNameAsync(customScopeName);
        if (openIddictScope is not null)
        {
            await scopeManager.DeleteAsync(openIddictScope);
        }

        await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Description = "Demo API",
            Name = customScopeName
        });

        // Seed the database with a default application.
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        object? application = await applicationManager.FindByClientIdAsync("service-worker");
        if (application is not null)
        {
            await applicationManager.DeleteAsync(application);
        }

        await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "service-worker",
            ClientSecret = "60607a2c-3514-460e-b3c3-83a298c3129f",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + customScopeName,
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
