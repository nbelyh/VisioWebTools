using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VisioWebTools
{
    [JsonSerializable(typeof(CipherOptions))]
    public partial class CipherOptionsJsonContext : JsonSerializerContext
    {
        public static readonly CipherOptionsJsonContext Context = new(JsonSerializationHelper.CreateJsonJsonSerializerOptions(JsonNamingPolicy.CamelCase));
    }

    [JsonSerializable(typeof(TranslateOptions))]
    public partial class TranslateOptionsJsonContext : JsonSerializerContext
    {
        public static readonly TranslateOptionsJsonContext Context = new(JsonSerializationHelper.CreateJsonJsonSerializerOptions(JsonNamingPolicy.CamelCase));
    }

    [JsonSerializable(typeof(JsonExportOptions))]
    public partial class JsonExportOptionsJsonContext : JsonSerializerContext
    {
        public static readonly JsonExportOptionsJsonContext Context = new(JsonSerializationHelper.CreateJsonJsonSerializerOptions(JsonNamingPolicy.CamelCase));
    }

    [JsonSerializable(typeof(DocumentInfo))]
    public partial class DocumentInfoJsonContext : JsonSerializerContext
    {
        public static readonly DocumentInfoJsonContext Context = new(JsonSerializationHelper.CreateJsonJsonSerializerOptions(JsonNamingPolicy.CamelCase));
    }

    [JsonSerializable(typeof(ChatResponse))]
    public partial class ChatResponseJsonContext : JsonSerializerContext
    {
        public static readonly ChatResponseJsonContext Context = new(JsonSerializationHelper.CreateJsonJsonSerializerOptions(JsonNamingPolicy.SnakeCaseLower));
    }

    [JsonSerializable(typeof(ChatRequest))]
    public partial class ChatRequestJsonContext : JsonSerializerContext
    {
        public static readonly ChatRequestJsonContext Context = new(JsonSerializationHelper.CreateJsonJsonSerializerOptions(JsonNamingPolicy.SnakeCaseLower));
    }

    public static class JsonSerializationHelper
    {
        public static JsonSerializerOptions CreateJsonJsonSerializerOptions(JsonNamingPolicy policy)
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = policy,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
        }
    }
}
