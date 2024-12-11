using System.Text.Json.Serialization;

namespace VisioWebTools
{
    [JsonSerializable(typeof(ChipherOptions))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class ChipherOptionsJsonContext : JsonSerializerContext
    {
    }
}
