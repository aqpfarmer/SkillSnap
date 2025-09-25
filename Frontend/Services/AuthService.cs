using Microsoft.AspNetCore.Components.Authorization;
using SkillSnap.Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Frontend.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> CreateUserAsync(RegisterDto registerDto);
        Task LogoutAsync();
        Task<bool> IsAuthenticatedAsync();
        Task<UserDto?> GetCurrentUserAsync();
        Task<string?> GetTokenAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly JsonSerializerOptions _jsonOptions;

        public AuthService(HttpClient httpClient, AuthenticationStateProvider authenticationStateProvider)
        {
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            return await ExecuteAuthRequestAsync(loginDto, "api/auth/login", "Login failed");
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            return await ExecuteAuthRequestAsync(registerDto, "api/auth/register", "Registration failed");
        }

        /// <summary>
        /// Common method for executing authentication requests to reduce code duplication
        /// </summary>
        private async Task<AuthResponseDto> ExecuteAuthRequestAsync<T>(T requestDto, string endpoint, string defaultErrorMessage)
        {
            try
            {
                var json = JsonSerializer.Serialize(requestDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(responseContent, _jsonOptions);
                    if (authResponse != null && authResponse.IsSuccess && !string.IsNullOrEmpty(authResponse.Token))
                    {
                        await SetTokenAsync(authResponse.Token);
                        NotifyAuthenticationStateChanged();
                    }
                    return authResponse ?? new AuthResponseDto { IsSuccess = false, Message = "Invalid response" };
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<AuthResponseDto>(responseContent, _jsonOptions);
                    return errorResponse ?? new AuthResponseDto { IsSuccess = false, Message = defaultErrorMessage };
                }
            }
            catch (Exception ex)
            {
                return new AuthResponseDto { IsSuccess = false, Message = $"An error occurred: {ex.Message}" };
            }
        }

        /// <summary>
        /// Notifies authentication state change
        /// </summary>
        private void NotifyAuthenticationStateChanged()
        {
            if (_authenticationStateProvider is CustomAuthenticationStateProvider customProvider)
            {
                customProvider.NotifyAuthenticationStateChanged();
            }
        }

        public async Task<AuthResponseDto> CreateUserAsync(RegisterDto registerDto)
        {
            try
            {
                var token = await GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var json = JsonSerializer.Serialize(registerDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/auth/create-user", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(responseContent, _jsonOptions);
                    return authResponse ?? new AuthResponseDto { IsSuccess = false, Message = "Invalid response" };
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<AuthResponseDto>(responseContent, _jsonOptions);
                    return errorResponse ?? new AuthResponseDto { IsSuccess = false, Message = "User creation failed" };
                }
            }
            catch (Exception ex)
            {
                return new AuthResponseDto { IsSuccess = false, Message = $"An error occurred: {ex.Message}" };
            }
        }

        public async Task LogoutAsync()
        {
            await RemoveTokenAsync();
            ((CustomAuthenticationStateProvider)_authenticationStateProvider).NotifyAuthenticationStateChanged();
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetTokenAsync();
            return !string.IsNullOrEmpty(token) && !IsTokenExpired(token);
        }

        public async Task<UserDto?> GetCurrentUserAsync()
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token) || IsTokenExpired(token))
                return null;

            var claims = GetClaimsFromToken(token);
            if (claims == null)
                return null;

            return new UserDto
            {
                Id = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "",
                Email = claims.FindFirst(ClaimTypes.Email)?.Value ?? "",
                FirstName = claims.FindFirst(ClaimTypes.GivenName)?.Value ?? "",
                LastName = claims.FindFirst(ClaimTypes.Surname)?.Value ?? "",
                PortfolioUserId = int.TryParse(claims.FindFirst("PortfolioUserId")?.Value, out var portfolioId) ? portfolioId : null
            };
        }

        public async Task<string?> GetTokenAsync()
        {
            return await ((CustomAuthenticationStateProvider)_authenticationStateProvider).GetTokenFromLocalStorage();
        }

        private async Task SetTokenAsync(string token)
        {
            await ((CustomAuthenticationStateProvider)_authenticationStateProvider).SetTokenInLocalStorage(token);
        }

        private async Task RemoveTokenAsync()
        {
            await ((CustomAuthenticationStateProvider)_authenticationStateProvider).RemoveTokenFromLocalStorage();
        }

        private bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                return jsonToken.ValidTo < DateTime.UtcNow;
            }
            catch
            {
                return true;
            }
        }

        private ClaimsPrincipal? GetClaimsFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                
                var claims = jsonToken.Claims.ToList();
                var identity = new ClaimsIdentity(claims, "jwt");
                return new ClaimsPrincipal(identity);
            }
            catch
            {
                return null;
            }
        }
    }
}