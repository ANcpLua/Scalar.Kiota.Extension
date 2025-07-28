using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Scalar.Kiota.Extension;

/// <summary>Extensions for registering Scalar + Kiota support in ASP.NET Core.</summary>
public static class ScalarKiotaExtensions
{
    /// <summary>
    ///     Adds Scalar API documentation and Kiota SDK generation services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for <see cref="ScalarKiotaOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddScalarWithKiota(
        this IServiceCollection services,
        Action<ScalarKiotaOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOpenApi(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);
        services.AddHttpClient();
        var options = new ScalarKiotaOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<SdkGenerationService>();
        services.AddHostedService<SdkGenerationService>();
        return services;
    }

    /// <summary>
    ///     Maps Scalar API documentation UI with Kiota SDK integration. Only active in Development environment.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="pattern">The URL pattern for the documentation UI. Defaults to "/api".</param>
    /// <returns>The web application for chaining.</returns>
    /// <remarks>
    /// This method configures the Scalar UI using settings from <see cref="ScalarKiotaOptions"/> registered via <see cref="AddScalarWithKiota"/>.
    /// </remarks>
    public static WebApplication MapScalarWithKiota(
        this WebApplication app,
        string pattern = "/api")
    {
        if (!app.Environment.IsDevelopment()) return app;
        app.UseStaticFiles();

        IEndpointRouteBuilder routeBuilder = app;
        if (!routeBuilder.DataSources.Any(ds => ds.Endpoints.Any(e => e.DisplayName?.Contains("/openapi/") == true)))
            app.MapOpenApi();

        var options = app.Services.GetRequiredService<ScalarKiotaOptions>();
        app.MapScalarApiReference(pattern, scalar =>
        {
            scalar.Title = options.Title ?? $"{app.Environment.ApplicationName} API";
            scalar.Theme = options.Theme;
            scalar.OpenApiRoutePattern = "/openapi/v1.json";
            scalar.JavaScriptConfiguration = "/.scalar-kiota/config.js";
        });

        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/")
            {
                context.Response.Redirect(pattern);
                return;
            }

            await next();
        });

        return app;
    }
}