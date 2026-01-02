using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Scalar.Kiota.Extension.Tests;

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Test Helpers                                                                                             //
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
public static class TestFixture
{
    /// <summary>
    ///     Returns a random lowercase string of specified length using stackalloc.
    /// </summary>
    [Pure]
    public static string NextString(int length = 8)
    {
        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < length; i++)
            buffer[i] = (char)Random.Shared.Next('a', 'z' + 1);
        return new string(buffer);
    }
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Data Sources for Parameterized Testing                                                                   //
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
public class TestDataSources
{
    public static IEnumerable<Func<string[]>> SdkLanguages()
    {
        yield return () => ["C#", "TypeScript"];
        yield return () => ["Python", "Go"];
        yield return () => ["Java", "TypeScript", "Ruby"];
    }

    public static IEnumerable<(string name, ScalarTheme theme, bool flag)> SdkOptions()
    {
        yield return ("MyApi", ScalarTheme.Saturn, true);
        yield return ("WeatherClient", ScalarTheme.BluePlanet, false);
    }

    public static IEnumerable<(string name, ScalarTheme theme, bool flag)> RandomSdkOptions()
    {
        yield return ("ApiClient", ScalarTheme.Saturn, Random.Shared.Next(2) == 0);
        yield return ($"Client{Random.Shared.Next(100, 999)}", ScalarTheme.BluePlanet, Random.Shared.Next(2) == 0);
        yield return ("Weather", ScalarTheme.Saturn, true);
        yield return ("Weather", ScalarTheme.BluePlanet, false);
    }

    public static IEnumerable<string> SdkNames()
    {
        yield return TestFixture.NextString(10);
        yield return TestFixture.NextString(15);
        yield return "StaticTestName";
    }

    public static IEnumerable<string> EdgeCaseSdkNames()
    {
        yield return "A";
        yield return "SDK-Name-With-Dashes";
        yield return "SDK_Name_With_Underscores";
        yield return "SDK Name With Spaces";
        yield return "SDKNameThatIsExtremelyLongAndExceedsNormalExpectationsForNamingButShouldStillWork";
        yield return "123NumericStart";
        yield return "UPPERCASE";
        yield return "lowercase";
        yield return "CamelCase";
        yield return "snake_case";
        yield return "kebab-case";
        yield return "SDK.With.Dots";
        yield return "SDK@Special#Characters";
    }

    /// <summary>Edge-case language arrays for parameterized tests.</summary>
    public static IEnumerable<Func<string[]>> EdgeCaseLanguages()
    {
        yield return () => [];
        yield return () => [""];
        yield return () => ["Unknown"];
        yield return () => ["C#", "TypeScript", "Python", "Go", "Java", "Ruby", "PHP", "Swift"];
        yield return () => ["typescript"];
        yield return () => ["TYPESCRIPT"];
        yield return () => ["Type Script"];
        yield return () => ["C++", "C#"];
        yield return () => Enumerable.Range(1, 20).Select(i => $"Language{i}").ToArray();
    }

    public static IEnumerable<string?> PathConfigurations()
    {
        yield return null;
        yield return "/absolute/path";
        yield return "relative/path";
        yield return "./current/path";
        yield return "../parent/path";
        yield return @"C:\Windows\Path";
        yield return "/path with spaces/";
        yield return "";
        yield return "/very/long/path/that/goes/deep/into/directory/structure";
    }

    public static IEnumerable<ScalarTheme> AllThemes()
    {
        return Enum.GetValues<ScalarTheme>();
    }

    public static IEnumerable<Func<ScalarKiotaOptions>> CompleteOptions()
    {
        yield return () => new ScalarKiotaOptions();

        yield return () => new ScalarKiotaOptions()
            .WithTitle("Minimal API")
            .WithSdkName("MinimalClient");

        yield return () => new ScalarKiotaOptions()
            .WithTitle("Full Configuration API")
            .WithTheme(ScalarTheme.Mars)
            .WithSdkName("FullClient")
            .WithLanguages("TypeScript", "C#", "Python")
            .WithOutputPath("/custom/output");

        yield return () => new ScalarKiotaOptions
        {
            Title = "Direct Property API",
            Theme = ScalarTheme.BluePlanet,
            SdkName = "DirectClient",
            Languages = ["Go", "Java"],
            OutputPath = "/direct/path",
            BundleTypeScript = false
        };
    }
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Argument Formatter                                                                                       //
//////////////////////////////////////////////////////////////////////////////////////////////////////////////
public sealed class UniversalArgumentFormatter : ArgumentDisplayFormatter
{
    [Pure]
    public override bool CanHandle(object? value)
    {
        return value switch
        {
            string => true,
            IEnumerable<string> => true,
            object[] { Length: > 0 } arr => arr.All(x => x is string),
            ScalarTheme => true,
            ScalarKiotaOptions => true,
            _ => false
        };
    }

    [Pure]
    public override string FormatValue(object? value)
    {
        return value switch
        {
            string s => $"'{s}'",
            string[] a => FormatLanguages(a),
            IEnumerable<string> e => FormatLanguages(e.ToArray()),
            object[] arr when arr.All(x => x is string) => FormatLanguages(arr.Cast<string>().ToArray()),
            ScalarTheme theme => theme.ToString(),
            ScalarKiotaOptions opts => $"{opts.SdkName} ({opts.Theme})",
            _ => throw new ArgumentException($"Unsupported type: {value?.GetType().Name ?? "null"}")
        };
    }

    [Pure]
    private static string FormatLanguages(params ReadOnlySpan<string> languages)
    {
        return languages.Length == 0 ? "[]" : $"[{string.Join(", ", languages.ToArray())}]";
    }
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Test Doubles / Fakes                                                                                     //
//////////////////////////////////////////////////////////////////////////////////////////////////////////////

[DebuggerDisplay("TestWebHostEnvironment: {EnvironmentName}")]
public class TestWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "TestApp";
    public string EnvironmentName { get; set; } = "Development";
    public string WebRootPath { get; set; } = "/test/wwwroot";
    public string ContentRootPath { get; set; } = "/test/content";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

[DebuggerDisplay("TestHostApplicationLifetime: Callbacks={RegisteredCallbacks.Count}")]
public class TestHostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _startedSource = new();
    private readonly CancellationTokenSource _stoppedSource = new();
    private readonly CancellationTokenSource _stoppingSource = new();

    public List<Action> RegisteredCallbacks { get; } = [];

    public CancellationToken ApplicationStarted => _startedSource.Token;
    public CancellationToken ApplicationStopping => _stoppingSource.Token;
    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    public void StopApplication()
    {
        _stoppingSource.Cancel();
        _stoppedSource.Cancel();
    }
}

/// <summary>
///     Minimal fake <see cref="IServer" /> for test scenarios.
/// </summary>
[DebuggerDisplay("TestServer: {GetServerUrl()}")]
public class TestServer : IServer
{
    public TestServer()
    {
        Features = new FeatureCollection();
        var addresses = new ServerAddressesFeature();
        addresses.Addresses.Add("http://localhost:5000");
        Features.Set<IServerAddressesFeature>(addresses);
    }

    public IFeatureCollection Features { get; }

    public Task StartAsync<TContext>(IHttpApplication<TContext> application,
        CancellationToken cancellationToken) where TContext : notnull
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void SetAddresses(IEnumerable<string> addresses)
    {
        var feature = Features.Get<IServerAddressesFeature>() ?? new ServerAddressesFeature();
        feature.Addresses.Clear();
        foreach (var address in addresses) feature.Addresses.Add(address);

        Features.Set(feature);
    }

    [Pure]
    private string GetServerUrl()
    {
        return Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault() ?? "http://localhost:5000";
    }
}

/// <summary>
///     Holds server addresses for test server instances.
/// </summary>
public class ServerAddressesFeature : IServerAddressesFeature
{
    public ICollection<string> Addresses { get; } = new List<string>();
    public bool PreferHostingUrls { get; set; }
}

/// <summary>
///     Minimal fake <see cref="IHttpClientFactory" /> for test scenarios.
/// </summary>
[DebuggerDisplay("TestHttpClientFactory")]
public class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var handler = new TestMessageHandler();
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
    }
}

/// <summary>
///     Minimal fake message handler for <see cref="HttpClient" /> in tests.
/// </summary>
public class TestMessageHandler : HttpMessageHandler
{
    /// <summary>
    /// Standard OpenAPI spec content used for testing. Use this constant when pre-creating cache files.
    /// </summary>
    public const string StandardSpecContent = """{"openapi":"3.1.0","info":{"title":"Test","version":"1.0.0"},"paths":{}}""";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        if (request.RequestUri?.PathAndQuery.Contains("/openapi/v1.json") == true)
            response.Content = new StringContent(StandardSpecContent);

        return Task.FromResult(response);
    }
}