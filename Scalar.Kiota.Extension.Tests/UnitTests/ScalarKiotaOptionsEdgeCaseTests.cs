namespace Scalar.Kiota.Extension.Tests.UnitTests;

[ArgumentDisplayFormatter<UniversalArgumentFormatter>]
public class ScalarKiotaOptionsEdgeCaseTests
{
    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.EdgeCaseSdkNames))]
    [DisplayName("WithSdkName - Handles edge case names correctly: '$sdkName'")]
    public async Task WithSdkName_HandlesEdgeCases(string sdkName)
    {
        var options = new ScalarKiotaOptions();

        var result = options.WithSdkName(sdkName);

        await Assert.That(options.SdkName).IsEqualTo(sdkName);
        await Assert.That(result).IsSameReferenceAs(options);
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.EdgeCaseLanguages))]
    [DisplayName("WithLanguages - Handles edge case language arrays")]
    public async Task WithLanguages_HandlesEdgeCases(string[] languages)
    {
        var options = new ScalarKiotaOptions();

        if (languages.Length == 0)
        {
            options.WithLanguages(languages);
            await Assert.That(options.Languages).IsEquivalentTo(["TypeScript"]);
        }
        else
        {
            options.WithLanguages(languages);
            await Assert.That(options.Languages).IsEquivalentTo(languages);
        }
    }

    [Test]
    [MethodDataSource(typeof(TestDataSources), nameof(TestDataSources.PathConfigurations))]
    [DisplayName("Path configurations - Handles various path formats")]
    public async Task PathConfigurations_HandleVariousFormats(string? outputPath)
    {
        var options = new ScalarKiotaOptions();

        if (outputPath is not null)
            options.WithOutputPath(outputPath);

        await Assert.That(options.OutputPath).IsEqualTo(outputPath);
    }

    [Test]
    [DisplayName("Null validation - Only WithSdkName requires non-null value")]
    public async Task NullValidation_OnlyWithSdkNameRequiresNonNull()
    {
        var options = new ScalarKiotaOptions();

        options.WithTitle(null!);
        await Assert.That(options.Title).IsNull();

        options.WithTitle("");
        await Assert.That(options.Title).IsEqualTo("");

        options.WithSdkName(null!);
        await Assert.That(options.SdkName).IsNull();

        options.WithOutputPath(null!);
        await Assert.That(options.OutputPath).IsNull();
    }

    [Test]
    [DisplayName("Unicode and international characters in strings")]
    public async Task UnicodeAndInternationalCharacters()
    {
        const string unicodeTitle = "API 名前 测试";
        const string unicodeSdkName = "SDK_名前_";
        const string unicodePath = "/путь/到/файла/測試";

        var options = new ScalarKiotaOptions()
            .WithTitle(unicodeTitle)
            .WithSdkName(unicodeSdkName)
            .WithOutputPath(unicodePath);

        using (Assert.Multiple())
        {
            await Assert.That(options.Title).IsEqualTo(unicodeTitle);
            await Assert.That(options.SdkName).IsEqualTo(unicodeSdkName);
            await Assert.That(options.OutputPath).IsEqualTo(unicodePath);
        }
    }
}