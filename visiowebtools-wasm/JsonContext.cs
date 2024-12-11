using VisioWebTools;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(ChipherOptions))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class ChipherOptionsJsonContext : JsonSerializerContext
{
}
