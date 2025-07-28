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
}