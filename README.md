[![.NET 10](https://img.shields.io/badge/.NET-10.0_Preview-7C3AED)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![NuGet](https://img.shields.io/nuget/v/Scalar.Kiota.Extension?label=NuGet&color=0891B2)](https://www.nuget.org/packages/Scalar.Kiota.Extension/)
[![License](https://img.shields.io/github/license/ANcpLua/Scalar.Kiota.Extension?label=License&color=white)](https://github.com/ANcpLua/Scalar.Kiota.Extension/blob/main/LICENSE)
[![codecov](https://codecov.io/gh/ANcpLua/Scalar.Kiota.Extension/branch/main/graph/badge.svg?token=lgxIXBnFrn)](https://codecov.io/gh/ANcpLua/Scalar.Kiota.Extension)

![demogif](https://github.com/user-attachments/assets/09d06cdb-3123-4ac7-8b5e-6afe8a967467)

# Scalar.Kiota.Extension

Automates Kiota SDK generation and integrates it with Scalar API documentation for ASP.NET Core minimal APIs with native AOT support.

## Installation

```bash
dotnet add package Scalar.Kiota.Extension --prerelease
```

## Quick Start

```csharp
using Scalar.Kiota.Extension;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScalarWithKiota();

var app = builder.Build();

app.MapScalarWithKiota("/api");

await app.RunAsync();
```

## Configuration

```csharp
builder.Services.AddScalarWithKiota(options =>
{
    options.WithTitle("My API")                    // Default: "{ApplicationName} API"
           .WithTheme(ScalarTheme.Saturn)          // Default: Saturn
           .WithSdkName("MyApiClient")             // Default: "ApiClient"
           .WithLanguages("TypeScript", "CSharp")  // Default: ["TypeScript"]
           .WithOutputPath("./custom-output")      // Default: "wwwroot/.scalar-kiota"
           .WithOpenDocsOnStartup();               // Default: false
           
    options.BundleTypeScript = true;               // Default: true
    options.DocumentationPath = "/docs";           // Default: null (uses pattern)
});
```

### Available Languages

- `TypeScript` - Bundled with esbuild for browser usage
- `CSharp` - .NET client
- `Python` - Python client
- `Java` - Java client
- `Go` - Go client
- `PHP` - PHP client
- `Ruby` - Ruby client
- `Swift` - Swift client
- `CLI` - Command line client

### Available Themes

`ScalarTheme.Default`, `ScalarTheme.Alternate`, `ScalarTheme.Moon`, `ScalarTheme.Purple`, `ScalarTheme.Solarized`,
`ScalarTheme.BluePlanet`, `ScalarTheme.Saturn`, `ScalarTheme.Kepler`, `ScalarTheme.Mars`, `ScalarTheme.DeepSpace`

## How It Works

1. Downloads OpenAPI spec from `/openapi/v1.json`
2. Generates SDKs using Kiota CLI (auto-installed if missing)
3. For TypeScript: creates package.json, installs dependencies, bundles with esbuild
4. Creates config.js that exposes the SDK to Scalar's "Try It" feature
5. Only regenerates when OpenAPI spec changes (SHA256 hash-based caching)

## Generated Structure

```
wwwroot/.scalar-kiota/
├── openapi.json      # Downloaded spec
├── config.js         # Scalar integration
├── sdk.js           # Bundled TypeScript (if enabled)
├── .spec.hash       # Cache validation
└── [language]/      # Generated SDK sources
```

## Requirements

- Download [.NET 10.0 Preview](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Install [Node.js](https://nodejs.org/) (for TypeScript bundling)

## License
This project is licensed under the [MIT License](LICENSE).
