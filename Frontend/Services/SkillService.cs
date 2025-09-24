using System.Text.Json;
using SkillSnap.Shared.Models;

namespace Frontend.Services
{
    public class SkillService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public SkillService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<Skill>> GetAllSkillsAsync()
        {
            var response = await _httpClient.GetAsync("api/skills");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Skill>>(json, _jsonOptions) ?? new List<Skill>();
        }

        public async Task<Skill?> GetSkillByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/skills/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Skill>(json, _jsonOptions);
            }

            return null;
        }

        public async Task<Skill> CreateSkillAsync(Skill skill)
        {
            var json = JsonSerializer.Serialize(skill, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/skills", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Skill>(responseJson, _jsonOptions) ?? skill;
        }

        public async Task<bool> UpdateSkillAsync(int id, Skill skill)
        {
            var json = JsonSerializer.Serialize(skill, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"api/skills/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteSkillAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/skills/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}