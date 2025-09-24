using System.Text.Json;
using SkillSnap.Shared.Models;

namespace Frontend.Services
{
    public class PortfolioUserService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public PortfolioUserService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<PortfolioUser>> GetAllPortfolioUsersAsync()
        {
            var response = await _httpClient.GetAsync("api/portfoliousers");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<PortfolioUser>>(json, _jsonOptions) ?? new List<PortfolioUser>();
        }

        public async Task<PortfolioUser?> GetPortfolioUserByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/portfoliousers/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PortfolioUser>(json, _jsonOptions);
            }

            return null;
        }

        public async Task<PortfolioUser> CreatePortfolioUserAsync(PortfolioUser portfolioUser)
        {
            var json = JsonSerializer.Serialize(portfolioUser, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/portfoliousers", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PortfolioUser>(responseJson, _jsonOptions) ?? portfolioUser;
        }

        public async Task<bool> UpdatePortfolioUserAsync(int id, PortfolioUser portfolioUser)
        {
            var json = JsonSerializer.Serialize(portfolioUser, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/portfoliousers/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeletePortfolioUserAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/portfoliousers/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}