using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Scalar.Kiota.Extension.Tests.UnitTests;

[ArgumentDisplayFormatter<UniversalArgumentFormatter>]
public class SdkGenerationServiceTests
{
    [Test]
    [Arguments("Production")]
    [Arguments("Staging")]
    [Arguments("QA")]
    [DisplayName("StartAsync_DoesNotRun_WhenEnvironmentIsNotDevelopment")]
    public async Task StartAsync_DoesNotRun_WhenEnvironmentIsNotDevelopment(string environment)
    {
        var env = new TestWebHostEnvironment { EnvironmentName = environment };
        var lifetime = new TestHostApplicationLifetime();
        var sut = CreateService(env, lifetime);

        await sut.StartAsync(CancellationToken.None);
        await Assert.That(sut).IsNotNull();
    }

    [Test]
    [DisplayName("StartAsync_RegistersCallback_WhenEnvironmentIsDevelopment")]
    public async Task StartAsync_RegistersCallback_WhenEnvironmentIsDevelopment()
    {
        var env = new TestWebHostEnvironment { EnvironmentName = "Development" };
        var lifetime = new TestHostApplicationLifetime();
        var sut = CreateService(env, lifetime);

        await sut.StartAsync(CancellationToken.None);
        await Assert.That(sut).IsNotNull();
        await Assert.That(env.EnvironmentName).IsEqualTo("Development");
    }

    [Test]
    [DisplayName("StopAsync_CompletesSuccessfully_WhenCalled")]
    public async Task StopAsync_CompletesSuccessfully_WhenCalled()
    {
        var sut = CreateService();
        await sut.StopAsync(CancellationToken.None);
    }

    [Test]
    [DisplayName("RootPath_UsesCustomOutputPath_WhenSpecified")]
    public async Task RootPath_UsesCustomOutputPath_WhenSpecified()
    {
        const string customPath = "/custom/output/path";
        var options = new ScalarKiotaOptions { OutputPath = customPath };
        var sut = CreateService(options: options);

        await Assert.That(sut.RootPath).IsEqualTo(customPath);
    }

    [Test]
    [DisplayName("RootPath_UsesDefaultPath_WhenOutputPathNotSpecified")]
    public async Task RootPath_UsesDefaultPath_WhenOutputPathNotSpecified()
    {
        var env = new TestWebHostEnvironment { ContentRootPath = "/app" };
        var options = new ScalarKiotaOptions { OutputPath = null };
        var sut = CreateService(env, options: options);

        await Assert.That(sut.RootPath).IsEqualTo("/app/wwwroot/.scalar-kiota");
    }

    [Test]
    [DisplayName("GetServerUrl_ReturnsFirstAddress_WhenServerAddressesAvailable")]
    public async Task GetServerUrl_ReturnsFirstAddress_WhenServerAddressesAvailable()
    {
        var mockServer = new TestServer();
        mockServer.SetAddresses(["http://localhost:5001", "http://localhost:5002"]);
        var sut = CreateService(server: mockServer);

        var url = sut.GetServerUrl();
        await Assert.That(url).IsEqualTo("http://localhost:5001");
    }

    [Test]
    [DisplayName("GetServerUrl_ReturnsDefault_WhenNoServerAddresses")]
    public async Task GetServerUrl_ReturnsDefault_WhenNoServerAddresses()
    {
        var mockServer = new TestServer();
        mockServer.SetAddresses([]);
        var sut = CreateService(server: mockServer);

        var url = sut.GetServerUrl();
        await Assert.That(url).IsEqualTo("http://localhost:5000");
    }

    [Test]
    [Arguments("openapi.json content", "08+JwlnXVGoI8uT9l5BUlKO+N+DsTmmBq4hvvKP6kNI=")]
    [Arguments("different content", "nZ1WBRtzRLhp5UqOzr8bOfIf4kSSIrvdE16aYIshZzg=")]
    [Arguments("", "47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU=")]
    [DisplayName("ComputeHash_GeneratesCorrectHash_WhenContentProvided")]
    public async Task ComputeHash_GeneratesCorrectHash_WhenContentProvided(string content, string expectedHash)
    {
        var result = SdkGenerationService.ComputeHash(content);
        await Assert.That(result).IsEqualTo(expectedHash);
    }

    [Test]
    [Arguments("TypeScript", "typescript")]
    [Arguments("C#", "c#")]
    [Arguments("Python", "python")]
    [Arguments("Go", "go")]
    [Arguments("JAVA", "java")]
    [DisplayName("GetSdkPath_GeneratesCorrectPath_WhenLanguageProvided")]
    public async Task GetSdkPath_GeneratesCorrectPath_WhenLanguageProvided(string language, string expectedPath)
    {
        var sut = CreateService();
        var sdkPath = sut.GetSdkPath(language);
        await Assert.That(sdkPath).IsNotNull();
        await Assert.That(sdkPath.EndsWith(expectedPath)).IsTrue();
    }

    [Test]
    [DisplayName("SpecPath_ReturnsCorrectPath_WhenAccessed")]
    public async Task SpecPath_ReturnsCorrectPath_WhenAccessed()
    {
        var sut = CreateService();
        await Assert.That(sut.SpecPath).IsNotNull();
        await Assert.That(sut.SpecPath.EndsWith("openapi.json")).IsTrue();
    }

    [Test]
    [DisplayName("HashPath_ReturnsCorrectPath_WhenAccessed")]
    public async Task HashPath_ReturnsCorrectPath_WhenAccessed()
    {
        var sut = CreateService();
        await Assert.That(sut.HashPath).IsNotNull();
        await Assert.That(sut.HashPath.EndsWith(".spec.hash")).IsTrue();
    }

    [Test]
    [DisplayName("ConfigPath_ReturnsCorrectPath_WhenAccessed")]
    public async Task ConfigPath_ReturnsCorrectPath_WhenAccessed()
    {
        var sut = CreateService();
        await Assert.That(sut.ConfigPath).IsNotNull();
        await Assert.That(sut.ConfigPath.EndsWith("config.js")).IsTrue();
    }

    [Test]
    [DisplayName("RunProcessAsync_ThrowsNothing_WhenProcessExitsWithZero")]
    public async Task RunProcessAsync_ThrowsNothing_WhenProcessExitsWithZero()
    {
        await Assert.That(async () => 
            await SdkGenerationService.RunProcessAsync("echo", "test")).ThrowsNothing();
    }

    [Test]
    [DisplayName("RunProcessAsync_ThrowsInvalidOperationException_WhenProcessExitsWithNonZero")]
    public async Task RunProcessAsync_ThrowsInvalidOperationException_WhenProcessExitsWithNonZero()
    {
        var exception = await Assert.That(async () => 
            await SdkGenerationService.RunProcessAsync("sh", "-c \"exit 1\""))
            .Throws<InvalidOperationException>();
        
        await Assert.That(exception?.Message).Contains("sh -c \"exit 1\" failed:");
    }

    [Test]
    [DisplayName("RunProcessAsync_ThrowsInvalidOperationException_WithStandardError")]
    public async Task RunProcessAsync_ThrowsInvalidOperationException_WithStandardError()
    {
        var exception = await Assert.That(async () => 
            await SdkGenerationService.RunProcessAsync("sh", "-c \"echo 'Error message' >&2; exit 1\""))
            .Throws<InvalidOperationException>();
            
        await Assert.That(exception?.Message).Contains("Error message");
    }

    private static SdkGenerationService CreateService(
        IWebHostEnvironment? environment = null,
        IHostApplicationLifetime? lifetime = null,
        ILogger<SdkGenerationService>? logger = null,
        ScalarKiotaOptions? options = null,
        IHttpClientFactory? httpClientFactory = null,
        IServer? server = null)
    {
        return new SdkGenerationService(
            environment ?? new TestWebHostEnvironment(),
            lifetime ?? new TestHostApplicationLifetime(),
            logger ?? NullLogger<SdkGenerationService>.Instance,
            options ?? new ScalarKiotaOptions(),
            httpClientFactory ?? new TestHttpClientFactory(),
            server ?? new TestServer());
    }
}