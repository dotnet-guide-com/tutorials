using System.Text.Json.Serialization;
using NativeAotEssentialsMinimal.Models;

namespace NativeAotEssentialsMinimal.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy =
        JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(
    typeof(SampleInfo))]
[JsonSerializable(
    typeof(RuntimeInfo))]
[JsonSerializable(
    typeof(EchoRequest))]
[JsonSerializable(
    typeof(EchoResponse))]
[JsonSerializable(
    typeof(ApiError))]
public partial class AppJsonSerializerContext :
    JsonSerializerContext
{
}