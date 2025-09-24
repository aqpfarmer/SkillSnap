using System.Text.Json;
using SkillSnap.Shared.Models;

namespace Frontend.Services
{
    public class PortfolioUserService
    {
        private readonly AuthenticatedHttpClientService _httpClientService;
        private readonly JsonSerializerOptions _jsonOptions;

        public PortfolioUserService(AuthenticatedHttpClientService httpClientService)
        {
            _httpClientService = httpClientService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<PortfolioUser>> GetAllPortfolioUsersAsync()
        {
            var response = await _httpClientService.GetAsync("api/portfoliousers");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<PortfolioUser>>(json, _jsonOptions) ?? new List<PortfolioUser>();
        }

        public async Task<PortfolioUser?> GetPortfolioUserByIdAsync(int id)
        {
            var response = await _httpClientService.GetAsync($"api/portfoliousers/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PortfolioUser>(json, _jsonOptions);
            }

            return null;
        }

        public async Task<PortfolioUser?> GetMyPortfolioUserAsync()
        {
            var response = await _httpClientService.GetAsync("api/portfoliousers/me");
            
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

            var response = await _httpClientService.PostAsync("api/portfoliousers", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PortfolioUser>(responseJson, _jsonOptions) ?? portfolioUser;
        }

        public async Task<bool> UpdatePortfolioUserAsync(int id, PortfolioUser portfolioUser)
        {
            var json = JsonSerializer.Serialize(portfolioUser, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClientService.PutAsync($"api/portfoliousers/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeletePortfolioUserAsync(int id)
        {
            var response = await _httpClientService.DeleteAsync($"api/portfoliousers/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetUserRoleAsync(int id)
        {
            try
            {
                var response = await _httpClientService.GetAsync($"api/portfoliousers/{id}/role");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<string>(json, _jsonOptions) ?? "User";
                }
                return "User";
            }
            catch
            {
                return "User";
            }
        }

        public async Task<bool> UpdateUserRoleAsync(int id, string role)
        {
            var json = JsonSerializer.Serialize(role, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClientService.PutAsync($"api/portfoliousers/{id}/role", content);
            return response.IsSuccessStatusCode;
        }
    }
}