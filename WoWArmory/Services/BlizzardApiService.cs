using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ArgentPonyWarcraftClient;
using WoWArmory.Models;

namespace WoWArmory.Services;

public class BlizzardApiService
{

    public BlizzardApiService(string clientId, string clientSecret, Region region)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        Region = region;
    }

    public BlizzardApiService()
    {
        Region = Region.Europe;  
    }

    protected string ClientId { get; set; }
    protected string ClientSecret { get; set; }
    protected Region Region { get; set; } 
    //public Enums.Region Region { get; set; } =  Enums.Region.Europe;

    private string BaseNamespace
    {
        get
        {
            return Region switch
            {
                Region.Europe => "eu",
                Region.Korea => "kr",
                Region.Taiwan => "tw",
                Region.China => "cn",
                Region.US => "us",
                _ => "eu"
            };
        }
    } 

    protected string ProfileNamespace => $"profile-{BaseNamespace}";
    protected string StaticNamespace => $"static-{BaseNamespace}";
    protected string DynamicNamespace => $"dynamic-{BaseNamespace}";
    
    protected async Task<BlizzardAccessToken?> GetOAuthTokenAsync()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}")));
        var requestBody = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var responseMessage = await httpClient.PostAsync($"{GetOAuthHost()}/oauth/token", requestBody);
        responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BlizzardAccessToken>(response);
    }
    
    private string GetOAuthHost()
    {
        return Region switch
        {
            Region.China => "https://www.battlenet.com.cn",
            Region.Europe => "https://eu.battle.net",
            Region.Korea => "https://kr.battle.net",
            Region.Taiwan => "https://apac.battle.net",
            _ => "https://us.battle.net",
        };
    }
    
    private string GetHost()
    {
        return Region switch
        {
            Region.China => "https://gateway.battlenet.com.cn",
            Region.Europe => "https://eu.api.blizzard.com",
            Region.Korea => "https://kr.api.blizzard.com",
            Region.Taiwan => "https://tw.api.blizzard.com",
            _ => "https://us.api.blizzard.com",
        };
    }
    
}