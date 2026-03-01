using System.Text.Json.Serialization;

namespace ZPv2.Common.Models;

public sealed class ZPv2Config
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("serviceUrl")]
    public string ServiceUrl { get; set; } = "http://127.0.0.1:16262";

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
