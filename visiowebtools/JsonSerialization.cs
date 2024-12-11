using System.Text.Json.Serialization;

namespace VisioWebTools
{
    [JsonSerializable(typeof(CipherOptions))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class CipherOptionsJsonContext : JsonSerializerContext
    {
    }

    [JsonSerializable(typeof(TranslateOptions))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class TranslateOptionsJsonContext : JsonSerializerContext
    {
    }

    [JsonSerializable(typeof(DocumentInfo))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    public partial class DocumentInfoJsonContext : JsonSerializerContext
    {
    }
}
