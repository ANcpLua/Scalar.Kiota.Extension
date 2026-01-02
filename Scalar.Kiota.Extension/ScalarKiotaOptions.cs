using Scalar.AspNetCore;

namespace Scalar.Kiota.Extension;

/// <summary>Options for configuring Scalar Kiota integration.</summary>
public class ScalarKiotaOptions
{
    /// <summary>
    ///     Gets or sets the title for the API documentation. If null, defaults to "{ApplicationName} API".
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    ///     Gets or sets the visual theme for the Scalar documentation UI. Defaults to <see cref="ScalarTheme.Saturn"/>.
    /// </summary>
    public ScalarTheme Theme { get; set; } = ScalarTheme.Saturn;

    /// <summary>
    ///     Gets or sets the name for the generated SDK client. Defaults to "ApiClient".
    /// </summary>
    public string SdkName { get; set; } = "ApiClient";

    /// <summary>
    ///     Gets or sets the programming languages for SDK generation. Defaults to ["TypeScript"].
    /// </summary>
    public string[] Languages { get; set; } = ["TypeScript"];

    /// <summary>
    ///     Gets or sets the output directory for generated SDKs. If null, defaults to "wwwroot/.scalar-kiota".
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    ///     Gets or sets whether to bundle TypeScript SDK with esbuild. Defaults to true.
    /// </summary>
    public bool BundleTypeScript { get; set; } = true;

    /// <summary>
    ///     Sets the title for the API documentation.
    /// </summary>
    /// <param name="title">The title to display. If null, defaults to "{ApplicationName} API".</param>
    /// <returns>The current instance for method chaining.</returns>
    public ScalarKiotaOptions WithTitle(string? title)
    {
        Title = title;
        return this;
    }

    /// <summary>
    ///     Sets the visual theme for the Scalar documentation UI.
    /// </summary>
    /// <param name="theme">The theme to apply. See <see cref="ScalarTheme"/> for available options.</param>
    /// <returns>The current instance for method chaining.</returns>
    public ScalarKiotaOptions WithTheme(ScalarTheme theme)
    {
        Theme = theme;
        return this;
    }

    /// <summary>
    ///     Sets the name for the generated SDK client.
    /// </summary>
    /// <param name="name">The SDK client name.</param>
    /// <returns>The current instance for method chaining.</returns>
    public ScalarKiotaOptions WithSdkName(string name)
    {
        SdkName = name;
        return this;
    }

    /// <summary>
    ///     Sets the programming languages for SDK generation.
    /// </summary>
    /// <param name="languages">The languages to generate SDKs for (e.g., "TypeScript", "CSharp", "Python").</param>
    /// <returns>The current instance for method chaining.</returns>
    public ScalarKiotaOptions WithLanguages(params string[] languages)
    {
        if (languages.Length > 0) Languages = languages;
        return this;
    }

    /// <summary>
    ///     Sets the output directory for generated SDKs.
    /// </summary>
    /// <param name="path">The output directory path. Defaults to "wwwroot/.scalar-kiota".</param>
    /// <returns>The current instance for method chaining.</returns>
    public ScalarKiotaOptions WithOutputPath(string path)
    {
        OutputPath = path;
        return this;
    }
}