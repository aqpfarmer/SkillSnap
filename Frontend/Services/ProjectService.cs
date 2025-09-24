using System.Text.Json;
using SkillSnap.Shared.Models;

namespace Frontend.Services
{
    public interface IProjectService
    {
        Task<List<Project>> GetAllProjectsAsync();
        Task<List<Project>> GetMyProjectsAsync();
        Task<Project?> GetProjectByIdAsync(int id);
        Task<Project> CreateProjectAsync(Project project);
        Task<bool> UpdateProjectAsync(int id, Project project);
        Task<bool> DeleteProjectAsync(int id);
    }

    public class ProjectService : IProjectService
    {
        private readonly AuthenticatedHttpClientService _httpClientService;
        private readonly JsonSerializerOptions _jsonOptions;

        public ProjectService(AuthenticatedHttpClientService httpClientService)
        {
            _httpClientService = httpClientService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            var response = await _httpClientService.GetAsync("api/projects");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Project>>(json, _jsonOptions) ?? new List<Project>();
        }

        public async Task<List<Project>> GetMyProjectsAsync()
        {
            var response = await _httpClientService.GetAsync("api/projects/my");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Project>>(json, _jsonOptions) ?? new List<Project>();
            }

            return new List<Project>();
        }

        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            var response = await _httpClientService.GetAsync($"api/projects/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Project>(json, _jsonOptions);
            }

            return null;
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            var json = JsonSerializer.Serialize(project, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClientService.PostAsync("api/projects", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Project>(responseJson, _jsonOptions) ?? project;
        }

        public async Task<bool> UpdateProjectAsync(int id, Project project)
        {
            var json = JsonSerializer.Serialize(project, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClientService.PutAsync($"api/projects/{id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            var response = await _httpClientService.DeleteAsync($"api/projects/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}