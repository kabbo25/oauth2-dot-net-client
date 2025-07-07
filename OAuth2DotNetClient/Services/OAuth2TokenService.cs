using System.Text;
using System.Text.Json;

namespace OAuth2DotNetClient.Services
{
    public class OAuth2TokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OAuth2TokenService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<TokenResponse> ExchangeCodeForTokenAsync(string authorizationCode)
        {
            try
            {
                var clientId = _configuration["OAuth2:ClientId"];
                var clientSecret = _configuration["OAuth2:ClientSecret"];
                var redirectUri = _configuration["OAuth2:RedirectUri"];
                var tokenUri = _configuration["OAuth2:TokenUri"];

                // Prepare Basic Authentication
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                
                // Prepare form data
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", authorizationCode),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri)
                });

                // Prepare request
                var request = new HttpRequestMessage(HttpMethod.Post, tokenUri)
                {
                    Content = formData
                };
                request.Headers.Add("Authorization", $"Basic {credentials}");

                // Send request
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var tokenData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                    return new TokenResponse
                    {
                        AccessToken = tokenData.GetProperty("access_token").GetString(),
                        RefreshToken = tokenData.TryGetProperty("refresh_token", out var refreshToken) ? refreshToken.GetString() : null,
                        TokenType = tokenData.GetProperty("token_type").GetString(),
                        ExpiresIn = tokenData.GetProperty("expires_in").GetInt32(),
                        IdToken = tokenData.TryGetProperty("id_token", out var idToken) ? idToken.GetString() : null
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to exchange authorization code for token: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error during token exchange", ex);
            }
        }
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public string IdToken { get; set; }
    }
}