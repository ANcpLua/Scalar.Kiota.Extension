using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Scalar.Kiota.Extension.Tests.IntegrationTests;

/// <summary>
///     Integration tests for Scalar Kiota extension
/// </summary>
[ArgumentDisplayFormatter<UniversalArgumentFormatter>]
public class ScalarKiotaIntegrationTests : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public ScalarKiotaIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.AddEndpointsApiExplorer();
                    services.AddScalarWithKiota(options =>
                    {
                        options.WithTitle("Test API")
                            .WithSdkName("TestClient");
                    });
                });
            });
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Test]
    [DisplayName("AddScalarWithKiota_RegistersRequiredServices")]
    public async Task AddScalarWithKiota_RegistersRequiredServices()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var options = services.GetService<ScalarKiotaOptions>();
        await Assert.That(options).IsNotNull();
        await Assert.That(options!.SdkName).IsEqualTo("TestClient");
        await Assert.That(options.Title).IsEqualTo("Test API");

        var sdkService = services.GetService<SdkGenerationService>();
        await Assert.That(sdkService).IsNotNull();
    }

    [Test]
    [DisplayName("MapScalarWithKiota_RedirectsRootPath_ToDocsPath")]
    public async Task MapScalarWithKiota_RedirectsRootPath_ToDocsPath()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        await Assert.That((int)response.StatusCode).IsEqualTo(302);
        await Assert.That(response.Headers.Location!.ToString()).IsEqualTo("/api");
    }

    [Test]
    [DisplayName("MapScalarWithKiota_ReturnsNotFound_ForNonMappedPath")]
    public async Task MapScalarWithKiota_ReturnsNotFound_ForNonMappedPath()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/non-existent-path");

        await Assert.That((int)response.StatusCode).IsEqualTo(404);
    }
}

/// <summary>
/// Integration tests for MapScalarWithKiota in Production environment
/// </summary>
[ArgumentDisplayFormatter<UniversalArgumentFormatter>]
public class ScalarKiotaProductionIntegrationTests : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public ScalarKiotaProductionIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureServices(services =>
                {
                    services.AddEndpointsApiExplorer();
                    services.AddScalarWithKiota();
                });
            });
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Test]
    [DisplayName("MapScalarWithKiota_DoesNotRedirect_WhenNotDevelopment")]
    public async Task MapScalarWithKiota_DoesNotRedirect_WhenNotDevelopment()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        // In production, root path should return 404 (no redirect configured)
        await Assert.That((int)response.StatusCode).IsEqualTo(404);
    }
}