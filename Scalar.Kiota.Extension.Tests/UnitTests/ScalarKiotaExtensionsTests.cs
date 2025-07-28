using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Scalar.Kiota.Extension.Tests.UnitTests;

[ArgumentDisplayFormatter<UniversalArgumentFormatter>]
public class ScalarKiotaExtensionsTests
{
    [Test]
    [DisplayName("AddScalarWithKiota - Registers all required services")]
    public async Task AddScalarWithKiota_RegistersAllServices_WhenCalled()
    {
        var sut = new ServiceCollection();
        sut.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        sut.AddSingleton<IHostApplicationLifetime>(new TestHostApplicationLifetime());
        sut.AddSingleton<IServer>(new TestServer());
        sut.AddLogging();
        sut.AddRouting();
        sut.AddEndpointsApiExplorer();

        sut.AddScalarWithKiota();
        var provider = sut.BuildServiceProvider();

        using (Assert.Multiple())
        {
            var httpClientFactory = provider.GetService<IHttpClientFactory>();
            await Assert.That(httpClientFactory).IsNotNull()
                .Because("HttpClientFactory should be registered");

            var scalarOptions = provider.GetService<ScalarKiotaOptions>();
            await Assert.That(scalarOptions).IsNotNull()
                .Because("ScalarKiotaOptions should be registered as singleton");

            var sdkService = provider.GetService<SdkGenerationService>();
            await Assert.That(sdkService).IsNotNull()
                .Because("SdkGenerationService should be registered as singleton");

            var hostedServices = provider.GetServices<IHostedService>();
            await Assert.That(hostedServices.Any(s => s is SdkGenerationService)).IsTrue()
                .Because("SdkGenerationService should be registered as hosted service");
        }
    }

    [Test]
    [DisplayName("AddScalarWithKiota - Applies configuration callback")]
    public async Task AddScalarWithKiota_AppliesConfiguration_WhenConfigureProvided()
    {
        var sut = new ServiceCollection();
        sut.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        sut.AddSingleton<IHostApplicationLifetime>(new TestHostApplicationLifetime());
        sut.AddSingleton<IServer>(new TestServer());
        sut.AddLogging();

        const string expectedTitle = "Test API";
        const ScalarTheme expectedTheme = ScalarTheme.Mars;
        const string expectedSdkName = "TestClient";

        sut.AddScalarWithKiota(options =>
        {
            options.WithTitle(expectedTitle)
                .WithTheme(expectedTheme)
                .WithSdkName(expectedSdkName);
        });

        var provider = sut.BuildServiceProvider();
        var options = provider.GetRequiredService<ScalarKiotaOptions>();

        using (Assert.Multiple())
        {
            await Assert.That(options.Title).IsEqualTo(expectedTitle);
            await Assert.That(options.Theme).IsEqualTo(expectedTheme);
            await Assert.That(options.SdkName).IsEqualTo(expectedSdkName);
        }
    }

    [Test]
    [DisplayName("AddScalarWithKiota - Works without configuration callback")]
    public async Task AddScalarWithKiota_UsesDefaults_WhenNoConfigurationProvided()
    {
        var sut = new ServiceCollection();
        sut.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        sut.AddSingleton<IHostApplicationLifetime>(new TestHostApplicationLifetime());
        sut.AddSingleton<IServer>(new TestServer());
        sut.AddLogging();

        sut.AddScalarWithKiota();

        var provider = sut.BuildServiceProvider();
        var options = provider.GetRequiredService<ScalarKiotaOptions>();

        using (Assert.Multiple())
        {
            await Assert.That(options.Theme).IsEqualTo(ScalarTheme.Saturn);
            await Assert.That(options.SdkName).IsEqualTo("ApiClient");
            await Assert.That(options.Languages).IsEquivalentTo(["TypeScript"]);
        }
    }
}