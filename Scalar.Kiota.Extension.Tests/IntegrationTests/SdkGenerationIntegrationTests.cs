using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Scalar.Kiota.Extension.Tests.Mocks;

namespace Scalar.Kiota.Extension.Tests.IntegrationTests;

[ArgumentDisplayFormatter<UniversalArgumentFormatter>]
public class SdkGenerationIntegrationTests : IAsyncDisposable
{
    private readonly string _testDirectory;

    public SdkGenerationIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"scalar-kiota-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();

        if (Directory.Exists(_testDirectory))
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete test directory {_testDirectory}: {ex.Message}");
            }

        GC.SuppressFinalize(this);
    }

    [Test]
    [DisplayName("GenerateSdksIfNeededAsync_GeneratesSDKs_WhenNoCache")]
    public async Task GenerateSdksIfNeededAsync_GeneratesSDKs_WhenNoCache()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            Languages = ["TypeScript"],
            SdkName = "TestClient"
        };

        var sut = CreateService(options);

        await sut.GenerateSdksIfNeededAsync();

        await Assert.That(File.Exists(Path.Combine(_testDirectory, "openapi.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(_testDirectory, ".spec.hash"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(_testDirectory, "config.js"))).IsTrue();
    }

    [Test]
    [DisplayName("GenerateSdksIfNeededAsync_SkipsGeneration_WhenCacheValid")]
    public async Task GenerateSdksIfNeededAsync_SkipsGeneration_WhenCacheValid()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            Languages = ["TypeScript"],
            SdkName = "TestClient"
        };

        // Use TestMessageHandler.StandardSpecContent for hash match with downloaded spec
        var expectedHash = SdkGenerationService.ComputeHash(TestMessageHandler.StandardSpecContent);

        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "openapi.json"), TestMessageHandler.StandardSpecContent);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, ".spec.hash"), expectedHash);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "config.js"), "export default {}");
        Directory.CreateDirectory(Path.Combine(_testDirectory, "typescript"));
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "sdk.js"), "// bundled");

        var mockRunner = new MockProcessRunner();
        var sut = CreateService(options, mockRunner);

        await sut.GenerateSdksIfNeededAsync();

        // No process calls should be made when cache is valid
        await Assert.That(mockRunner.RunCalls.Count).IsEqualTo(0);
        await Assert.That(await File.ReadAllTextAsync(Path.Combine(_testDirectory, ".spec.hash")))
            .IsEqualTo(expectedHash);
    }

    [Test]
    [DisplayName("IsCachedAndValidAsync_ReturnsFalse_WhenNoCacheFiles")]
    public async Task IsCachedAndValidAsync_ReturnsFalse_WhenNoCacheFiles()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            Languages = ["TypeScript"]
        };

        var sut = CreateService(options);
        const string hash = "test-hash";

        var result = await sut.IsCachedAndValidAsync(hash);

        await Assert.That(result).IsFalse();
    }

    [Test]
    [DisplayName("IsCachedAndValidAsync_ReturnsFalse_WhenHashMismatch")]
    public async Task IsCachedAndValidAsync_ReturnsFalse_WhenHashMismatch()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            Languages = ["TypeScript"]
        };

        await File.WriteAllTextAsync(Path.Combine(_testDirectory, ".spec.hash"), "old-hash");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "config.js"), "export default {}");

        var sut = CreateService(options);

        var result = await sut.IsCachedAndValidAsync("new-hash");

        await Assert.That(result).IsFalse();
    }

    [Test]
    [DisplayName("IsCachedAndValidAsync_ReturnsTrue_WhenCacheValid")]
    public async Task IsCachedAndValidAsync_ReturnsTrue_WhenCacheValid()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            Languages = ["TypeScript"]
        };

        const string hash = "valid-hash";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, ".spec.hash"), hash);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "config.js"), "export default {}");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "sdk.js"), "// bundled");

        var sut = CreateService(options);

        var result = await sut.IsCachedAndValidAsync(hash);

        await Assert.That(result).IsTrue();
    }

    [Test]
    [Arguments("C#", "c#")]
    [Arguments("Python", "python")]
    [Arguments("Go", "go")]
    [DisplayName("IsCachedAndValidAsync_ChecksLanguageDirectories_WhenNotTypeScript")]
    public async Task IsCachedAndValidAsync_ChecksLanguageDirectories_WhenNotTypeScript(string language,
        string expectedDir)
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            Languages = [language]
        };

        const string hash = "valid-hash";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, ".spec.hash"), hash);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "config.js"), "export default {}");

        var sut = CreateService(options);

        var result1 = await sut.IsCachedAndValidAsync(hash);

        var langDir = Path.Combine(_testDirectory, expectedDir);
        Directory.CreateDirectory(langDir);
        await File.WriteAllTextAsync(Path.Combine(langDir, "client.cs"), "// generated");

        var result2 = await sut.IsCachedAndValidAsync(hash);

        await Assert.That(result1).IsFalse();
        await Assert.That(result2).IsTrue();
    }

    [Test]
    [DisplayName("IsCachedAndValidAsync_ReturnsFalse_WhenTypeScriptSdkJsMissing")]
    public async Task IsCachedAndValidAsync_ReturnsFalse_WhenTypeScriptSdkJsMissing()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            Languages = ["TypeScript"]
        };

        const string hash = "valid-hash";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, ".spec.hash"), hash);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "config.js"), "export default {}");

        var sut = CreateService(options);

        var result = await sut.IsCachedAndValidAsync(hash);

        await Assert.That(result).IsFalse();
        await Assert.That(File.Exists(Path.Combine(_testDirectory, "sdk.js"))).IsFalse();
    }

    [Test]
    [DisplayName("EnsurePackageJsonAsync_CreatesPackageJson_WhenNotExists")]
    public async Task EnsurePackageJsonAsync_CreatesPackageJson_WhenNotExists()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            SdkName = "TestClient"
        };

        var tsPath = Path.Combine(_testDirectory, "typescript");
        Directory.CreateDirectory(tsPath);

        var sut = CreateService(options);

        await sut.EnsurePackageJsonAsync(tsPath);

        var packageJsonPath = Path.Combine(tsPath, "package.json");
        await Assert.That(File.Exists(packageJsonPath)).IsTrue();

        var content = await File.ReadAllTextAsync(packageJsonPath);
        var packageJson = JsonSerializer.Deserialize<PackageJson>(content, PackageJsonContext.Default.PackageJson);

        await Assert.That(packageJson).IsNotNull();
        await Assert.That(packageJson!.Name).IsEqualTo("testclient");
        await Assert.That(packageJson.Dependencies).Count().IsEqualTo(6);
        await Assert.That(packageJson.Dependencies.ContainsKey("@microsoft/kiota-abstractions")).IsTrue();
    }

    [Test]
    [Arguments("Test Client", "test-client")]
    [Arguments("My SDK Name", "my-sdk-name")]
    [Arguments("UPPERCASE", "uppercase")]
    [DisplayName("EnsurePackageJsonAsync_NormalizesPackageName_WhenSpacesInSdkName")]
    public async Task EnsurePackageJsonAsync_NormalizesPackageName_WhenSpacesInSdkName(string sdkName,
        string expectedName)
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            SdkName = sdkName
        };

        var tsPath = Path.Combine(_testDirectory, "typescript");
        Directory.CreateDirectory(tsPath);

        var sut = CreateService(options);

        await sut.EnsurePackageJsonAsync(tsPath);

        var content = await File.ReadAllTextAsync(Path.Combine(tsPath, "package.json"));
        var packageJson = JsonSerializer.Deserialize<PackageJson>(content, PackageJsonContext.Default.PackageJson);

        await Assert.That(packageJson!.Name).IsEqualTo(expectedName);
    }

    [Test]
    [DisplayName("EnsurePackageJsonAsync_DoesNotOverwrite_WhenContentSame")]
    public async Task EnsurePackageJsonAsync_DoesNotOverwrite_WhenContentSame()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            SdkName = "TestClient"
        };

        var tsPath = Path.Combine(_testDirectory, "typescript");
        Directory.CreateDirectory(tsPath);

        var sut = CreateService(options);

        await sut.EnsurePackageJsonAsync(tsPath);

        var packageJsonPath = Path.Combine(tsPath, "package.json");
        var originalTime = File.GetLastWriteTimeUtc(packageJsonPath);

        await Task.Delay(100);

        await sut.EnsurePackageJsonAsync(tsPath);

        var newTime = File.GetLastWriteTimeUtc(packageJsonPath);
        await Assert.That(newTime).IsEqualTo(originalTime);
    }

    [Test]
    [DisplayName("EnsureConfigFileAsync_CreatesConfigFile_WhenNotExists")]
    public async Task EnsureConfigFileAsync_CreatesConfigFile_WhenNotExists()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            SdkName = "TestClient"
        };

        var sut = CreateService(options);

        await sut.EnsureConfigFileAsync();

        var configPath = Path.Combine(_testDirectory, "config.js");
        await Assert.That(File.Exists(configPath)).IsTrue();

        var content = await File.ReadAllTextAsync(configPath);
        await Assert.That(content).Contains("TestClient");
        await Assert.That(content).Contains("export default");
        await Assert.That(content).Contains("async load()");
        await Assert.That(content).Contains("import('./sdk.js')");
    }

    [Test]
    [DisplayName("EnsureConfigFileAsync_DoesNotOverwrite_WhenExists")]
    public async Task EnsureConfigFileAsync_DoesNotOverwrite_WhenExists()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory,
            SdkName = "TestClient"
        };

        var configPath = Path.Combine(_testDirectory, "config.js");
        const string existingContent = "// Custom config";
        await File.WriteAllTextAsync(configPath, existingContent);

        var sut = CreateService(options);

        await sut.EnsureConfigFileAsync();

        var content = await File.ReadAllTextAsync(configPath);
        await Assert.That(content).IsEqualTo(existingContent);
    }

    [Test]
    [DisplayName("DownloadSpecAsync_ReturnsOpenApiSpec_WhenServerResponds")]
    public async Task DownloadSpecAsync_ReturnsOpenApiSpec_WhenServerResponds()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory
        };

        var sut = CreateService(options);

        var spec = await sut.DownloadSpecAsync();

        await Assert.That(spec).IsNotNull();
        await Assert.That(spec).Contains("openapi");
        await Assert.That(spec).Contains("3.1.0");
    }

    [Test]
    [DisplayName("EnsureKiotaInstalledAsync_ChecksKiotaVersion_BeforeInstalling")]
    public async Task EnsureKiotaInstalledAsync_ChecksKiotaVersion_BeforeInstalling()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory
        };

        var mockRunner = new MockProcessRunner();
        var sut = CreateService(options, mockRunner);

        await sut.EnsureKiotaInstalledAsync();

        await Assert.That(mockRunner.RunCalls.Count).IsEqualTo(1);
        await Assert.That(mockRunner.RunCalls[0].FileName).IsEqualTo("kiota");
        await Assert.That(mockRunner.RunCalls[0].Arguments).IsEqualTo("--version");
    }

    [Test]
    [DisplayName("EnsureKiotaInstalledAsync_InstallsKiota_WhenNotFound")]
    public async Task EnsureKiotaInstalledAsync_InstallsKiota_WhenNotFound()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory
        };

        var mockRunner = new MockProcessRunner { ThrowOnFileName = "kiota" };
        var sut = CreateService(options, mockRunner);

        await sut.EnsureKiotaInstalledAsync();

        await Assert.That(mockRunner.RunCalls.Count).IsEqualTo(2);
        await Assert.That(mockRunner.RunCalls[1].FileName).IsEqualTo("dotnet");
        await Assert.That(mockRunner.RunCalls[1].Arguments).Contains("tool install -g Microsoft.OpenApi.Kiota");
    }

    [Test]
    [DisplayName("EnsureNpmDependenciesAsync_SkipsInstall_WhenNodeModulesUpToDate")]
    public async Task EnsureNpmDependenciesAsync_SkipsInstall_WhenNodeModulesUpToDate()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory
        };

        var tsPath = Path.Combine(_testDirectory, "typescript");
        Directory.CreateDirectory(tsPath);
        await File.WriteAllTextAsync(Path.Combine(tsPath, "package.json"), "{}");

        // Create package-lock.json with a newer timestamp than package.json
        await Task.Delay(100);
        await File.WriteAllTextAsync(Path.Combine(tsPath, "package-lock.json"), "{}");
        Directory.CreateDirectory(Path.Combine(tsPath, "node_modules"));

        var mockRunner = new MockProcessRunner();
        var sut = CreateService(options, mockRunner);

        await sut.EnsureNpmDependenciesAsync(tsPath);

        // No npm calls should be made
        await Assert.That(mockRunner.RunCalls.Count).IsEqualTo(0);
    }

    [Test]
    [DisplayName("EnsureNpmDependenciesAsync_RunsNpmInstall_WhenNoLockFile")]
    public async Task EnsureNpmDependenciesAsync_RunsNpmInstall_WhenNoLockFile()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory
        };

        var tsPath = Path.Combine(_testDirectory, "typescript");
        Directory.CreateDirectory(tsPath);
        await File.WriteAllTextAsync(Path.Combine(tsPath, "package.json"), "{}");

        var mockRunner = new MockProcessRunner();
        var sut = CreateService(options, mockRunner);

        await sut.EnsureNpmDependenciesAsync(tsPath);

        await Assert.That(mockRunner.RunCalls.Count).IsEqualTo(1);
        await Assert.That(mockRunner.RunCalls[0].FileName).IsEqualTo("npm");
        await Assert.That(mockRunner.RunCalls[0].Arguments).Contains("install");
    }

    [Test]
    [DisplayName("EnsureNpmDependenciesAsync_RunsNpmCi_WhenLockFileExists")]
    public async Task EnsureNpmDependenciesAsync_RunsNpmCi_WhenLockFileExists()
    {
        var options = new ScalarKiotaOptions
        {
            OutputPath = _testDirectory
        };

        var tsPath = Path.Combine(_testDirectory, "typescript");
        Directory.CreateDirectory(tsPath);
        await File.WriteAllTextAsync(Path.Combine(tsPath, "package-lock.json"), "{}");

        // Create package.json with a newer timestamp than lock file
        await Task.Delay(100);
        await File.WriteAllTextAsync(Path.Combine(tsPath, "package.json"), "{}");

        var mockRunner = new MockProcessRunner();
        var sut = CreateService(options, mockRunner);

        await sut.EnsureNpmDependenciesAsync(tsPath);

        await Assert.That(mockRunner.RunCalls.Count).IsEqualTo(1);
        await Assert.That(mockRunner.RunCalls[0].FileName).IsEqualTo("npm");
        await Assert.That(mockRunner.RunCalls[0].Arguments).Contains("ci");
    }

    private SdkGenerationService CreateService(ScalarKiotaOptions? options = null, IProcessRunner? processRunner = null)
    {
        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = _testDirectory,
            EnvironmentName = "Development"
        };

        return new SdkGenerationService(
            environment,
            new TestHostApplicationLifetime(),
            NullLogger<SdkGenerationService>.Instance,
            options ?? new ScalarKiotaOptions(),
            new TestHttpClientFactory(),
            new TestServer(),
            processRunner ?? new DefaultProcessRunner());
    }
}
