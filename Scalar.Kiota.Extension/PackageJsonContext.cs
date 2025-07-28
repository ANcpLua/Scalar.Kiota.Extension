using System.Text.Json.Serialization;

namespace Scalar.Kiota.Extension;

[JsonSerializable(typeof(PackageJson))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class PackageJsonContext : JsonSerializerContext;

internal class PackageJson
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Type { get; set; } = "module";
    [JsonPropertyName("private")] public bool Private { get; set; } = true;
    public Dictionary<string, string> Dependencies { get; set; } = new();
}