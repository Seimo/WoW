using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using WarcraftLogsClient.Response;

namespace WarcraftLogsClient;

public class WarcraftLogsClient
{
    static WarcraftLogsClient()
    {
        JsonSerializerOptions = new JsonSerializerOptions();
        //JsonSerializerOptions.Converters.Add(new EpochConverter());
        //JsonSerializerOptions.Converters.Add(new MillisecondTimeSpanConverter());
    }

    public WarcraftLogsClient(string clientId, string clientSecret)
    {
        Client = new HttpClient();
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        ClientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
    }

    private string ClientId { get; }
    private string ClientSecret { get; }
    private OAuthAccessToken Token { get; set; }
    private DateTimeOffset TokenExpiration { get; set; }
    private HttpClient Client { get; }

    private static JsonSerializerOptions JsonSerializerOptions { get; }

    private string GetHost()
    {
        return "https://www.warcraftlogs.com";
    }
    
    public Uri GetReportsUri(string reportCode)
    {
        return new Uri($"{GetHost()}/reports/{reportCode}");
    }
    
    private Uri GetPublicApi()
    {
        return new Uri($"{GetHost()}/api/v2/client");
    }
    
    private Uri GetPrivateApi()
    {
        return new Uri($"{GetHost()}/api/v2/user");
    }

    public async Task<RequestResult<WarcraftLogsResponse>> GetReport(string reportCode)
    {
        return await GetAsync<WarcraftLogsResponse>(reportCode);
    }

    private async Task<RequestResult<WarcraftLogsResponse>> GetAsync<T>(string reportCode)
    {
        if (Token == null || DateTimeOffset.UtcNow >= TokenExpiration)
        {
            Token = await GetToken().ConfigureAwait(false);
            TokenExpiration = DateTimeOffset.UtcNow.AddSeconds(Token.ExpiresIn).AddSeconds(-30);
        }

        return await GetReport(reportCode, Token.AccessToken).ConfigureAwait(false);
    }

    private GraphQLHttpClient GetGraphQlClient(string accessToken)
    {
        var client = new GraphQLHttpClient(GetPublicApi(), new SystemTextJsonSerializer());
        
        // Add an authentication header with the token.
        client.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task<RequestResult<WarcraftLogsResponse>> GetReport(string reportCode, string accessToken)
    {
        using var client = GetGraphQlClient(accessToken);
        var request = new GraphQLRequest
        {
            Query = @"query($code: String){
                            reportData{
                                report(code: $code)
                                {
                                    startTime,
                                    title,
                                    fights(difficulty: 3)
                                    {
                                        id
                                        name
                                        startTime
                                        endTime
                                    },
                                    guild
                                    {
                                        id
                                    },
                                    owner
                                    {
                                        id
                                        name
                                    },
                                    masterData
                                    {
                                        logVersion
                                        gameVersion
                                    }
                                }
                            }
                        }",
            Variables = new
            {
                code = reportCode
            }
        };

        try
        {
            var response = await client.SendQueryAsync<WarcraftLogsResponse>(request);
            return response.Data;
        }
        catch (Exception e)
        {
            var requestError = new RequestError
            {
                Code = null,
                Detail = e.Message,
                Type = typeof(JsonException).ToString()
            };
            return new RequestResult<WarcraftLogsResponse>(requestError);
        }
    }
    
    private async Task<RequestResult<WarcraftLogsResponse>> GetReportData(string id, string accessToken)
    {
        using var client = GetGraphQlClient(accessToken);
        var request = new GraphQLRequest
        {
            Query = @"query($code: String){
                            reportData{
                                report(code: $code)
                                {
                                    fights
                                    {
                                        id
                                        name
                                        startTime
                                        endTime
                                    }
                                }
                            }
                        }",
            Variables = new
            {
                code = id
            }
        };

        try
        {
            var response = await client.SendQueryAsync<WarcraftLogsResponse>(request);
            return response.Data;
        }
        catch (Exception e)
        {
            var requestError = new RequestError
            {
                Code = null,
                Detail = e.Message,
                Type = typeof(JsonException).ToString()
            };
            return new RequestResult<WarcraftLogsResponse>(requestError);
        }
    }

    private async Task<OAuthAccessToken?> GetToken()
    {
        var credentials = $"{ClientId}:{ClientSecret}";

        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));

        var requestBody = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var responseMessage = await Client.PostAsync($"{GetHost()}/oauth/token", requestBody);
        responseMessage.EnsureSuccessStatusCode();
        var response = await responseMessage.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OAuthAccessToken>(response);
    }
}