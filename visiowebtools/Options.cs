using System.Text.Json.Serialization;

namespace VisioWebTools
{
    public class CipherOptions
    {
        public bool EnableCipherShapeText { get; set; }
        public bool EnableCipherShapeFields { get; set; }
        public bool EnableCipherPageNames { get; set; }
        public bool EnableCipherPropertyValues { get; set; }
        public bool EnableCipherPropertyLabels { get; set; }
    }

    [JsonSerializable(typeof(CipherOptions))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class CipherOptionsJsonContext : JsonSerializerContext
    {
    }
    
    public class TranslateOptions
    {
        public bool EnableTranslateShapeText { get; set; }
        public bool EnableTranslateShapeFields { get; set; }
        public bool EnableTranslatePageNames { get; set; }
        public bool EnableTranslatePropertyValues { get; set; }
        public bool EnableTranslatePropertyLabels { get; set; }
    }

    [JsonSerializable(typeof(TranslateOptions))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class TranslateOptionsJsonContext : JsonSerializerContext
    {
    }
}
