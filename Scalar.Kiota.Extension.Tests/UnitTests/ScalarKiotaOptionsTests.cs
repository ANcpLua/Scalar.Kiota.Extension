using Scalar.AspNetCore;

namespace Scalar.Kiota.Extension.Tests.UnitTests;

[ArgumentDisplayFormatter<UniversalArgumentFormatter>]
public class ScalarKiotaOptionsTests
{
    [Test]
    [DisplayName("ScalarKiotaOptions - Default values are correctly initialized")]
    public async Task Constructor_InitializesDefaultValues_WhenCreated()
    {
        var sut = new ScalarKiotaOptions();

        using (Assert.Multiple())
        {
            await Assert.That(sut.Title).IsNull();
            await Assert.That(sut.Theme).IsEqualTo(ScalarTheme.Saturn);
            await Assert.That(sut.SdkName).IsEqualTo("ApiClient");
            await Assert.That(sut.Languages).IsEquivalentTo(["TypeScript"]);
            await Assert.That(sut.OpenDocsOnStartup).IsFalse();
            await Assert.That(sut.OutputPath).IsNull();
            await Assert.That(sut.DocumentationPath).IsNull();
            await Assert.That(sut.BundleTypeScript).IsTrue();
        }
    }

    [Test]
    [Arguments("My API Title")]
    [Arguments("")]
    [Arguments("Very Long API Title That Exceeds Normal Length Expectations But Should Still Work")]
    [DisplayName("WithTitle - Sets title correctly: '$title'")]
    public async Task WithTitle_SetsTitle_WhenTitleIsProvided(string title)
    {
        var sut = new ScalarKiotaOptions();

        var result = sut.WithTitle(title);

        await Assert.That(sut.Title).IsEqualTo(title);
        await Assert.That(result).IsSameReferenceAs(sut);
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.AllThemes))]
    [DisplayName("WithTheme - Sets theme correctly: $theme")]
    public async Task WithTheme_SetsTheme_WhenThemeIsValid(ScalarTheme theme)
    {
        var sut = new ScalarKiotaOptions();

        var result = sut.WithTheme(theme);

        await Assert.That(sut.Theme).IsEqualTo(theme);
        await Assert.That(result).IsSameReferenceAs(sut);
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.SdkNames))]
    [DisplayName("WithSdkName - Sets SDK name correctly: '$sdkName'")]
    public async Task WithSdkName_SetsSdkName_WhenNameIsProvided(string sdkName)
    {
        var sut = new ScalarKiotaOptions();

        var result = sut.WithSdkName(sdkName);

        await Assert.That(sut.SdkName).IsEqualTo(sdkName);
        await Assert.That(result).IsSameReferenceAs(sut);
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.SdkLanguages))]
    [DisplayName("WithLanguages - Sets languages correctly")]
    public async Task WithLanguages_UpdatesLanguages_WhenLanguagesAreValid(string[] languages)
    {
        var sut = new ScalarKiotaOptions();

        var result = sut.WithLanguages(languages);

        await Assert.That(sut.Languages).IsEquivalentTo(languages);
        await Assert.That(result).IsSameReferenceAs(sut);
    }

    [Test]
    [DisplayName("WithLanguages - Empty array does not update languages")]
    public async Task WithLanguages_DoesNotUpdate_WhenEmptyArray()
    {
        var sut = new ScalarKiotaOptions();
        var originalLanguages = sut.Languages;

        var result = sut.WithLanguages();

        await Assert.That(sut.Languages).IsSameReferenceAs(originalLanguages);
        await Assert.That(result).IsSameReferenceAs(sut);
    }

    [Test]
    [DisplayName("WithOpenDocsOnStartup - Sets flag to true")]
    public async Task WithOpenDocsOnStartup_SetsOpenDocsToTrue_WhenCalled()
    {
        var sut = new ScalarKiotaOptions();

        var result = sut.WithOpenDocsOnStartup();

        await Assert.That(sut.OpenDocsOnStartup).IsTrue();
        await Assert.That(result).IsSameReferenceAs(sut);
    }

    [Test]
    [Arguments("/custom/output/path")]
    [Arguments("relative/path")]
    [Arguments("")]
    [DisplayName("WithOutputPath - Sets path correctly: '$path'")]
    public async Task WithOutputPath_SetsOutputPath_WhenPathIsProvided(string path)
    {
        var sut = new ScalarKiotaOptions();

        var result = sut.WithOutputPath(path);

        await Assert.That(sut.OutputPath).IsEqualTo(path);
        await Assert.That(result).IsSameReferenceAs(sut);
    }

    [Test]
    [DisplayName("Fluent API - Can chain multiple methods")]
    public async Task FluentApi_Chains_WhenMethodsCalledInSequence()
    {
        var sut = new ScalarKiotaOptions()
            .WithTitle("Test API")
            .WithTheme(ScalarTheme.Mars)
            .WithSdkName("TestClient")
            .WithLanguages("C#", "Python", "Go")
            .WithOpenDocsOnStartup()
            .WithOutputPath("/test/output");

        using (Assert.Multiple())
        {
            await Assert.That(sut.Title).IsEqualTo("Test API");
            await Assert.That(sut.Theme).IsEqualTo(ScalarTheme.Mars);
            await Assert.That(sut.SdkName).IsEqualTo("TestClient");
            await Assert.That(sut.Languages).IsEquivalentTo(["C#", "Python", "Go"]);
            await Assert.That(sut.OpenDocsOnStartup).IsTrue();
            await Assert.That(sut.OutputPath).IsEqualTo("/test/output");
        }
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.CompleteOptions))]
    [DisplayName("Complete options configurations work correctly")]
    public async Task CompleteOptions_AllPropertiesAssignCorrectly_WhenCopiedFromSource(
        ScalarKiotaOptions sourceOptions)
    {
        var sut = new ScalarKiotaOptions
        {
            Title = sourceOptions.Title,
            Theme = sourceOptions.Theme,
            SdkName = sourceOptions.SdkName,
            Languages = sourceOptions.Languages,
            OpenDocsOnStartup = sourceOptions.OpenDocsOnStartup,
            OutputPath = sourceOptions.OutputPath,
            DocumentationPath = sourceOptions.DocumentationPath,
            BundleTypeScript = sourceOptions.BundleTypeScript
        };

        using (Assert.Multiple())
        {
            await Assert.That(sut.Title).IsEqualTo(sourceOptions.Title);
            await Assert.That(sut.Theme).IsEqualTo(sourceOptions.Theme);
            await Assert.That(sut.SdkName).IsEqualTo(sourceOptions.SdkName);
            await Assert.That(sut.Languages).IsEquivalentTo(sourceOptions.Languages);
            await Assert.That(sut.OpenDocsOnStartup).IsEqualTo(sourceOptions.OpenDocsOnStartup);
            await Assert.That(sut.OutputPath).IsEqualTo(sourceOptions.OutputPath);
            await Assert.That(sut.DocumentationPath).IsEqualTo(sourceOptions.DocumentationPath);
            await Assert.That(sut.BundleTypeScript).IsEqualTo(sourceOptions.BundleTypeScript);
        }
    }
}