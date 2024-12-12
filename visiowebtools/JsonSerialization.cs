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
    public partial class DocumentInfoJsonContext : JsonSerializerContext
    {
    }

    [JsonSerializable(typeof(ChatResponse))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    public partial class ChatResponseJsonContext : JsonSerializerContext
    {
    }

    [JsonSerializable(typeof(ChatRequest))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    public partial class ChatRequestJsonContext : JsonSerializerContext
    {
    }
}
