# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --verbosity normal
dotnet pack Scalar.Kiota.Extension/Scalar.Kiota.Extension.csproj --configuration Release --no-build --output ./nupkg
```

Run a single test:
```bash
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

## Prerequisites

- .NET 10.0 Preview SDK
- Node.js (for TypeScript bundling during SDK generation)
- Kiota CLI: `dotnet tool install -g Microsoft.OpenApi.Kiota`

## Architecture

This library automates Kiota SDK generation and integrates it with Scalar API documentation for ASP.NET Core minimal APIs.

### Projects

- **Scalar.Kiota.Extension** - Main NuGet package with public API
- **Scalar.Kiota.Extension.Tests** - TUnit-based tests with code coverage
- **WebApiNativeAOT** - Demo app for integration testing (native AOT enabled)

### Core Components

**ScalarKiotaExtensions.cs** - Entry points:
- `AddScalarWithKiota()` - Registers services including `SdkGenerationService` as `IHostedService`
- `MapScalarWithKiota()` - Maps Scalar UI routes (development only)

**SdkGenerationService.cs** - Background hosted service that:
1. Downloads OpenAPI spec from `/openapi/v1.json` on app startup
2. Computes SHA256 hash for cache validation (stored in `.spec.hash`)
3. Runs Kiota CLI to generate SDKs for configured languages
4. For TypeScript: creates package.json, runs npm install, bundles with esbuild
5. Creates `config.js` for Scalar integration

**ScalarKiotaOptions.cs** - Fluent configuration builder with properties:
- `Title`, `Theme`, `SdkName`, `Languages`, `OutputPath`
- `BundleTypeScript` (default: true), `OpenDocsOnStartup` (default: false)

### SDK Output

Generated files go to `wwwroot/.scalar-kiota/`:
```
openapi.json      # Downloaded spec
.spec.hash        # SHA256 cache validation
config.js         # Scalar integration
sdk.js            # Bundled TypeScript (if enabled)
[language]/       # Generated SDK sources
```

## Testing

Uses TUnit framework (not xUnit/NUnit). Test structure:
- `UnitTests/` - Core logic tests
- `IntegrationTests/` - WebApplicationFactory-based tests

Coverage output: `TestResults/coverage.cobertura.xml`

## Code Standards

- Nullable reference types enabled with `TreatWarningsAsErrors`
- Language version: preview
- Test project suppresses CA2252 warnings
