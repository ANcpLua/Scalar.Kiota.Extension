using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Scalar.Kiota.Extension;

internal class SdkGenerationService : IHostedService
{
    public SdkGenerationService(
        IWebHostEnvironment environment,
        IHostApplicationLifetime lifetime,
        ILogger<SdkGenerationService> logger,
        ScalarKiotaOptions options,
        IHttpClientFactory httpClientFactory,
        IServer server,
        IProcessRunner processRunner)
    {
        Environment = environment;
        Lifetime = lifetime;
        Logger = logger;
        Options = options;
        HttpClientFactory = httpClientFactory;
        Server = server;
        ProcessRunner = processRunner;
    }

    private IWebHostEnvironment Environment { get; }
    private IHostApplicationLifetime Lifetime { get; }
    private ILogger<SdkGenerationService> Logger { get; }
    private ScalarKiotaOptions Options { get; }
    private IHttpClientFactory HttpClientFactory { get; }
    private IServer Server { get; }
    private IProcessRunner ProcessRunner { get; }

    public string RootPath =>
        Options.OutputPath ?? Path.Combine(Environment.ContentRootPath, "wwwroot", ".scalar-kiota");

    public string SpecPath => Path.Combine(RootPath, "openapi.json");
    public string HashPath => Path.Combine(RootPath, ".spec.hash");
    public string ConfigPath => Path.Combine(RootPath, "config.js");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!Environment.IsDevelopment())
            return Task.CompletedTask;

        Lifetime.ApplicationStarted.Register(() => _ = GenerateSdksIfNeededAsync());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task GenerateSdksIfNeededAsync()
    {
        Directory.CreateDirectory(RootPath);

        var specContent = await DownloadSpecAsync();
        var currentHash = ComputeHash(specContent);

        if (await IsCachedAndValidAsync(currentHash))
        {
            Logger.LogInformation("SDKs are up-to-date");
        }
        else
        {
            await File.WriteAllTextAsync(SpecPath, specContent);
            await File.WriteAllTextAsync(HashPath, currentHash);

            await EnsureKiotaInstalledAsync();

            foreach (var language in Options.Languages)
                await GenerateSdkAsync(language);

            await EnsureConfigFileAsync();

            Logger.LogInformation("SDK generation completed");
        }

        if (Options.OpenDocsOnStartup)
            OpenBrowser($"/{Options.DocumentationPath ?? "api"}");
    }

    public async Task<bool> IsCachedAndValidAsync(string currentHash)
    {
        if (!File.Exists(HashPath) || !File.Exists(ConfigPath))
            return false;

        var cachedHash = await File.ReadAllTextAsync(HashPath);
        if (cachedHash != currentHash)
            return false;

        foreach (var language in Options.Languages)
        {
            var sdkPath = GetSdkPath(language);
            if (language.Equals("TypeScript", StringComparison.OrdinalIgnoreCase))
            {
                if (!File.Exists(Path.Combine(RootPath, "sdk.js")))
                    return false;
            }
            else if (!Directory.Exists(sdkPath) ||
                     !Directory.EnumerateFiles(sdkPath, "*", SearchOption.AllDirectories).Any())
            {
                return false;
            }
        }

        return true;
    }

    public string GetSdkPath(string language)
    {
        return Path.Combine(RootPath, language.ToLowerInvariant());
    }

    public async Task<string> DownloadSpecAsync()
    {
        var url = $"{GetServerUrl()}/openapi/v1.json";
        using var client = HttpClientFactory.CreateClient();
        return await client.GetStringAsync(url);
    }

    public static string ComputeHash(string content)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }

    private async Task GenerateSdkAsync(string language)
    {
        var outputDir = GetSdkPath(language);

        await ProcessRunner.RunAsync("kiota",
            $"generate --openapi \"{SpecPath}\" " +
            $"--language {language} " +
            $"--class-name {Options.SdkName} " +
            $"--namespace-name {Options.SdkName} " +
            $"--output \"{outputDir}\" " +
            "--clean-output --exclude-backward-compatible");

        if (language.Equals("TypeScript", StringComparison.OrdinalIgnoreCase) && Options.BundleTypeScript)
            await BundleTypeScriptAsync(outputDir);
    }

    private async Task BundleTypeScriptAsync(string tsPath)
    {
        await EnsurePackageJsonAsync(tsPath);
        await EnsureNpmDependenciesAsync(tsPath);
        await BundleWithEsbuildAsync(tsPath);
    }

    public async Task EnsurePackageJsonAsync(string tsPath)
    {
        var packageJsonPath = Path.Combine(tsPath, "package.json");

        var packageJson = new PackageJson
        {
            Name = Options.SdkName.ToLowerInvariant().Replace(" ", "-"),
            Dependencies = new Dictionary<string, string>
            {
                ["@microsoft/kiota-abstractions"] = "^1.0.0-preview.96",
                ["@microsoft/kiota-http-fetchlibrary"] = "^1.0.0-preview.96",
                ["@microsoft/kiota-serialization-json"] = "^1.0.0-preview.96",
                ["@microsoft/kiota-serialization-text"] = "^1.0.0-preview.96",
                ["@microsoft/kiota-serialization-form"] = "^1.0.0-preview.96",
                ["@microsoft/kiota-serialization-multipart"] = "^1.0.0-preview.96"
            }
        };

        var json = JsonSerializer.Serialize(packageJson, PackageJsonContext.Default.PackageJson);

        if (!File.Exists(packageJsonPath) || await File.ReadAllTextAsync(packageJsonPath) != json)
            await File.WriteAllTextAsync(packageJsonPath, json);
    }

    internal async Task EnsureNpmDependenciesAsync(string tsPath)
    {
        var nodeModulesPath = Path.Combine(tsPath, "node_modules");
        var packageLockPath = Path.Combine(tsPath, "package-lock.json");

        if (Directory.Exists(nodeModulesPath) && File.Exists(packageLockPath))
        {
            var packageJsonTime = File.GetLastWriteTimeUtc(Path.Combine(tsPath, "package.json"));
            var lockTime = File.GetLastWriteTimeUtc(packageLockPath);

            if (lockTime >= packageJsonTime)
            {
                Logger.LogDebug("NPM dependencies are up-to-date");
                return;
            }
        }

        Logger.LogInformation("Installing NPM dependencies...");
        var command = File.Exists(packageLockPath) ? "ci" : "install";
        await ProcessRunner.RunAsync("npm", $"{command} --no-audit --no-fund", tsPath);
    }

    public async Task BundleWithEsbuildAsync(string tsPath)
    {
        var entry = Directory.GetFiles(tsPath, "*.ts", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(f => !f.EndsWith(".d.ts"))
                    ?? throw new InvalidOperationException($"No TypeScript entry point found in {tsPath}");

        var outputPath = Path.Combine(RootPath, "sdk.js");

        Logger.LogInformation("Bundling TypeScript SDK...");
        await ProcessRunner.RunAsync("npx",
            $"--yes esbuild \"{entry}\" --bundle --format=esm --platform=browser " +
            "--target=es2020 --minify --tree-shaking=true " +
            $"--outfile=\"{outputPath}\"",
            tsPath);
    }

    public async Task EnsureConfigFileAsync()
    {
        if (File.Exists(ConfigPath))
            return;

        var configContent = $$"""
                                  export default {
                                      async load() {
                                          try {
                                              const module = await import('./sdk.js');
                                              const createClient = module.createApiClient || module.{{Options.SdkName}} || module.default;
                                              if (typeof createClient === 'function') {
                                                  const { FetchRequestAdapter } = await import('@microsoft/kiota-http-fetchlibrary');
                                                  window.apiClient = createClient(new FetchRequestAdapter({ baseUrl: window.location.origin }));
                                                  console.log('Scalar-Kiota: SDK loaded successfully');
                                                  return {};
                                              }
                                              console.warn('Scalar-Kiota: No client factory found in SDK');
                                              return {};
                                          } catch (error) {
                                              console.error('Scalar-Kiota: SDK load failed:', error);
                                              return {};
                                          }
                                      }
                                  };
                              """;

        await File.WriteAllTextAsync(ConfigPath, configContent);
    }

    internal async Task EnsureKiotaInstalledAsync()
    {
        try
        {
            await ProcessRunner.RunAsync("kiota", "--version");
        }
        catch
        {
            Logger.LogWarning("Installing Kiota CLI...");
            await ProcessRunner.RunAsync("dotnet", "tool install -g Microsoft.OpenApi.Kiota");
        }
    }

    public string GetServerUrl()
    {
        var addresses = Server.Features.Get<IServerAddressesFeature>()?.Addresses;
        return addresses?.FirstOrDefault() ?? "http://localhost:5000";
    }

    internal void OpenBrowser(string path)
    {
        var url = $"{GetServerUrl()}{path}";
        ProcessRunner.OpenUrl(url);
    }
}
