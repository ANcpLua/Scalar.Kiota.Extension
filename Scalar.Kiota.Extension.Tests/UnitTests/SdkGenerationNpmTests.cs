using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TUnit.Assertions.AssertConditions.Throws;

namespace Scalar.Kiota.Extension.Tests.UnitTests;

[ArgumentDisplayFormatter<UniversalArgumentFormatter>]
public class SdkGenerationNpmTests : IAsyncDisposable
{
    private readonly string _testDirectory;

    public SdkGenerationNpmTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"scalar-npm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public async ValueTask DisposeAsync()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }

    [Test]
    [DisplayName("EnsurePackageJsonAsync_CreatesCorrectStructure_WhenFileNotExists")]
    public async Task EnsurePackageJsonAsync_CreatesCorrectStructure_WhenFileNotExists()
    {
        var tsPath = Path.Combine(_testDirectory, "typescript");
        Directory.CreateDirectory(tsPath);

        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            SdkName = "TestClient"
        };

        var sut = CreateService(options: options);

        await sut.EnsurePackageJsonAsync(tsPath);

        var packageJsonPath = Path.Combine(tsPath, "package.json");
        await Assert.That(File.Exists(packageJsonPath)).IsTrue();

        var content = await File.ReadAllTextAsync(packageJsonPath);
        await Assert.That(content).Contains("testclient");
        await Assert.That(content).Contains("@microsoft/kiota-abstractions");
    }

    [Test]
    [DisplayName("BundleWithEsbuildAsync_ThrowsException_WhenNoTypeScriptFiles")]
    public async Task BundleWithEsbuildAsync_ThrowsException_WhenNoTypeScriptFiles()
    {
        var tsPath = Path.Combine(_testDirectory, "typescript");
        Directory.CreateDirectory(tsPath);

        await File.WriteAllTextAsync(Path.Combine(tsPath, "types.d.ts"), "// type definitions");

        var sut = CreateService();

        await Assert.That(async () => await sut.BundleWithEsbuildAsync(tsPath))
            .Throws<InvalidOperationException>();
    }

    private SdkGenerationService CreateService(
        ILogger<SdkGenerationService>? logger = null,
        TestProcessRunner? processRunner = null,
        ScalarKiotaOptions? options = null)
    {
        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = _testDirectory,
            EnvironmentName = "Development"
        };

        var service = new SdkGenerationService(
            environment,
            new TestHostApplicationLifetime(),
            logger ?? NullLogger<SdkGenerationService>.Instance,
            options ?? new ScalarKiotaOptions { OutputPath = _testDirectory },
            new TestHttpClientFactory(),
            new TestServer());

        if (processRunner != null)
            TestProcessRunner.AttachToService();

        return service;
    }
}

internal class TestProcessRunner
{
    public static void AttachToService()
    {
    }
}