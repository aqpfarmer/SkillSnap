using System.Text.Json;
using SkillSnap.Shared.Models;

namespace Frontend.Services
{
    public interface ISkillService
    {
        Task<List<Skill>> GetAllSkillsAsync();
        Task<Skill?> GetSkillByIdAsync(int id);
        Task<Skill> CreateSkillAsync(Skill skill);
        Task<bool> UpdateSkillAsync(int id, Skill skill);
        Task<bool> DeleteSkillAsync(int id);
        Task<List<string>> GetDistinctSkillNamesAsync();
    }

    public class SkillService : ISkillService
    {
        private readonly AuthenticatedHttpClientService _httpClientService;
        private readonly JsonSerializerOptions _jsonOptions;

        public SkillService(AuthenticatedHttpClientService httpClientService)
        {
            _httpClientService = httpClientService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<Skill>> GetAllSkillsAsync()
        {
            var response = await _httpClientService.GetAsync("api/skills");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Skill>>(json, _jsonOptions) ?? new List<Skill>();
        }

        public async Task<Skill?> GetSkillByIdAsync(int id)
        {
            var response = await _httpClientService.GetAsync($"api/skills/{id}");
            
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

            var response = await _httpClientService.PostAsync("api/skills", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Skill>(responseJson, _jsonOptions) ?? skill;
        }

        public async Task<bool> UpdateSkillAsync(int id, Skill skill)
        {
            var json = JsonSerializer.Serialize(skill, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClientService.PutAsync($"api/skills/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteSkillAsync(int id)
        {
            var response = await _httpClientService.DeleteAsync($"api/skills/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<string>> GetDistinctSkillNamesAsync()
        {
            try
            {
                var response = await _httpClientService.GetAsync("api/skills/names/distinct");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}