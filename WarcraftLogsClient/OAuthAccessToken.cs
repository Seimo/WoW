using System.Text.Json.Serialization;

namespace WarcraftLogsClient;

public class OAuthAccessToken
{
    [JsonPropertyName("access_token")] public string AccessToken { get; init; } = "";

    [JsonPropertyName("token_type")] public string TokenType { get; init; } = "";

    [JsonPropertyName("expires_in")] public long ExpiresIn { get; init; }
}