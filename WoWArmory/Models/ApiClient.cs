namespace WoWArmory.Models;

public abstract class ApiClient
{
    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";
    public string Token { get; set; } = "";
}

public class ArmoryApiClient : ApiClient
{
}

public class WarcraftLogsApiClient : ApiClient
{
}