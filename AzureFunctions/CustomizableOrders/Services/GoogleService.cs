using System.Net.Http.Headers;
using System.Text.Json;
using CustomizableOrders.Models;

namespace CustomizableOrders.Services
{
    public sealed class GoogleService(IHttpClientFactory httpClientFactory)
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly HttpClient _googleApiClient = httpClientFactory.CreateClient(Config.GOOGLE_API_CLIENT);
        
        private const int TOKEN_EXPIRATION_BUFFER = 600; // 10 minutes
        private string _token = string.Empty;
        private int _accessTokenExpirationInSeconds;
        private DateTime _accessTokenLastCreationDate;

        public async Task<bool> SendPdfToFolderAsync(string fileUrl, Guid orderGuid, string printer, long? sheetNo = null)
        {
            string url = sheetNo is null
                ? $"exec?q={fileUrl}&q={orderGuid}.pdf&q={printer}"
                : $"exec?q={fileUrl}&q=[{sheetNo}]{orderGuid}.pdf&q={printer}";

            var requestMsg = new HttpRequestMessage(HttpMethod.Get, url);
            requestMsg.Headers.Authorization = await GetAuthenticationHeaderAsync();

            var response = await _googleApiClient.SendAsync(requestMsg);
            return response.IsSuccessStatusCode;
        }

        private async Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync()
        {
            _token = NeedsNewToken() ? await GetTokenAsync() : _token;
            return new AuthenticationHeaderValue("Bearer", _token);
        }

        private bool NeedsNewToken()
        {
            if (!string.IsNullOrEmpty(_token) && (DateTime.Now - _accessTokenLastCreationDate).TotalSeconds > _accessTokenExpirationInSeconds - TOKEN_EXPIRATION_BUFFER)
                _token = null;

            if (string.IsNullOrEmpty(_token))
                return true;

            return false;
        }

        private async Task<string> GetTokenAsync()
        {
            using var http = _httpClientFactory.CreateClient(Config.GOOGLE_OAUTH_CLIENT);
            var data = new
            {
                grant_type = "refresh_token",
                client_id = Config.GoogleClientId,
                client_secret = Config.GoogleClientSecret,
                refresh_token = Config.GoogleRefreshToken
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "token") { Content = new StringContent(data.ToJson()) };
            var httpResponse = await http.SendAsync(request);
            using var stream = httpResponse.Content.ReadAsStream();
            var oauth = await JsonSerializer.DeserializeAsync<OAuthResponse>(stream);

            _accessTokenExpirationInSeconds = oauth.ExpiresIn;
            _accessTokenLastCreationDate = DateTime.Now;
            return oauth.AccessToken;
        }
    }
}
