using System.Net.Http.Headers;

namespace Frontend.Services
{
    public class AuthenticatedHttpClientService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;

        public AuthenticatedHttpClientService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        private async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(HttpMethod method, string requestUri, HttpContent? content = null)
        {
            var request = new HttpRequestMessage(method, requestUri);
            
            // Always get fresh token for each request
            var token = await _authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            if (content != null)
            {
                request.Content = content;
            }
            
            return request;
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, requestUri);
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            var request = await CreateAuthenticatedRequestAsync(HttpMethod.Post, requestUri, content);
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content)
        {
            var request = await CreateAuthenticatedRequestAsync(HttpMethod.Put, requestUri, content);
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string requestUri)
        {
            var request = await CreateAuthenticatedRequestAsync(HttpMethod.Delete, requestUri);
            return await _httpClient.SendAsync(request);
        }
    }
}