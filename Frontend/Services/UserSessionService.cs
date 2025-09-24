using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SkillSnap.Shared.Models;
using System.Text.Json;

namespace Frontend.Services
{
    /// <summary>
    /// Comprehensive state management service for user session and application data
    /// </summary>
    public interface IUserSessionService
    {
        // User Session Management
        UserDto? CurrentUser { get; }
        bool IsAuthenticated { get; }
        string? AuthToken { get; }
        DateTime? TokenExpiry { get; }

        // Cached Data
        List<PortfolioUser>? PortfolioUsers { get; }
        List<Project>? UserProjects { get; }
        List<Skill>? UserSkills { get; }
        PortfolioUser? CurrentPortfolioUser { get; }

        // Events for reactive UI updates
        event Action? OnStateChanged;
        event Action<UserDto?>? OnUserChanged;
        event Action<bool>? OnAuthenticationChanged;
        event Action<List<Project>?>? OnProjectsChanged;
        event Action<List<Skill>?>? OnSkillsChanged;

        // Session Management Methods
        Task InitializeAsync();
        Task SetUserSessionAsync(UserDto user, string token, DateTime expiry);
        Task ClearUserSessionAsync();
        Task RefreshUserDataAsync();

        // Cached Data Management
        Task LoadPortfolioUsersAsync(bool forceRefresh = false);
        Task LoadUserProjectsAsync(bool forceRefresh = false);
        Task LoadUserSkillsAsync(bool forceRefresh = false);
        Task LoadCurrentPortfolioUserAsync(bool forceRefresh = false);

        // Data Invalidation
        Task InvalidateProjectsCache();
        Task InvalidateSkillsCache();
        Task InvalidatePortfolioUsersCache();
        Task InvalidateAllCache();

        // Utility Methods
        bool IsTokenExpired();
        Task<T?> GetCachedDataAsync<T>(string key) where T : class;
        Task SetCachedDataAsync<T>(string key, T data, TimeSpan? expiry = null) where T : class;
        Task ClearCachedDataAsync(string key);
    }

    public class UserSessionService : IUserSessionService, IDisposable
    {
        private readonly IAuthService _authService;
        private readonly IPortfolioUserService _portfolioUserService;
        private readonly IProjectService _projectService;
        private readonly ISkillService _skillService;
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;

        // Static state to persist across scoped service instances
        private static UserDto? _currentUser;
        private static string? _authToken;
        private static DateTime? _tokenExpiry;
        private static List<PortfolioUser>? _portfolioUsers;
        private static List<Project>? _userProjects;
        private static List<Skill>? _userSkills;
        private static PortfolioUser? _currentPortfolioUser;
        private static readonly Dictionary<string, DateTime> _cacheTimestamps = new();
        private static readonly TimeSpan _defaultCacheExpiry = TimeSpan.FromMinutes(5);

        // Static events to persist across instances
        private static event Action? _onStateChanged;
        private static event Action<UserDto?>? _onUserChanged;
        private static event Action<bool>? _onAuthenticationChanged;
        private static event Action<List<Project>?>? _onProjectsChanged;
        private static event Action<List<Skill>?>? _onSkillsChanged;

        // Instance events that delegate to static events
        public event Action? OnStateChanged
        {
            add => _onStateChanged += value;
            remove => _onStateChanged -= value;
        }

        public event Action<UserDto?>? OnUserChanged
        {
            add => _onUserChanged += value;
            remove => _onUserChanged -= value;
        }

        public event Action<bool>? OnAuthenticationChanged
        {
            add => _onAuthenticationChanged += value;
            remove => _onAuthenticationChanged -= value;
        }

        public event Action<List<Project>?>? OnProjectsChanged
        {
            add => _onProjectsChanged += value;
            remove => _onProjectsChanged -= value;
        }

        public event Action<List<Skill>?>? OnSkillsChanged
        {
            add => _onSkillsChanged += value;
            remove => _onSkillsChanged -= value;
        }

        public UserSessionService(
            IAuthService authService,
            IPortfolioUserService portfolioUserService,
            IProjectService projectService,
            ISkillService skillService,
            NavigationManager navigationManager,
            IJSRuntime jsRuntime)
        {
            _authService = authService;
            _portfolioUserService = portfolioUserService;
            _projectService = projectService;
            _skillService = skillService;
            _navigationManager = navigationManager;
            _jsRuntime = jsRuntime;
        }

        // Properties
        public UserDto? CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null && !IsTokenExpired();
        public string? AuthToken => _authToken;
        public DateTime? TokenExpiry => _tokenExpiry;
        public List<PortfolioUser>? PortfolioUsers => _portfolioUsers;
        public List<Project>? UserProjects => _userProjects;
        public List<Skill>? UserSkills => _userSkills;
        public PortfolioUser? CurrentPortfolioUser => _currentPortfolioUser;

        /// <summary>
        /// Initialize the session service - should be called on app startup
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Check if user is already authenticated
                var isAuthenticated = await _authService.IsAuthenticatedAsync();
                if (isAuthenticated)
                {
                    var currentUser = await _authService.GetCurrentUserAsync();
                    var token = await _authService.GetTokenAsync();
                    
                    if (currentUser != null && !string.IsNullOrEmpty(token))
                    {
                        var tokenExpiry = GetTokenExpiry(token);
                        await SetUserSessionAsync(currentUser, token, tokenExpiry);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing user session: {ex.Message}");
                // Clear any potentially corrupted session data
                await ClearUserSessionAsync();
            }
        }

        /// <summary>
        /// Set user session data and initialize user-specific cache
        /// </summary>
        public async Task SetUserSessionAsync(UserDto user, string token, DateTime expiry)
        {
            var wasAuthenticated = IsAuthenticated;
            
            _currentUser = user;
            _authToken = token;
            _tokenExpiry = expiry;

            // Clear existing cache when user changes
            if (!wasAuthenticated || _currentUser?.Id != user.Id)
            {
                await InvalidateAllCache();
            }

            // Notify subscribers
            _onUserChanged?.Invoke(_currentUser);
            _onAuthenticationChanged?.Invoke(true);
            NotifyStateChanged();

            // Load initial user data in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadCurrentPortfolioUserAsync();
                    await LoadUserProjectsAsync();
                    await LoadUserSkillsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading initial user data: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Clear user session and all cached data
        /// </summary>
        public async Task ClearUserSessionAsync()
        {
            var wasAuthenticated = IsAuthenticated;

            _currentUser = null;
            _authToken = null;
            _tokenExpiry = null;

            await InvalidateAllCache();

            // Notify subscribers
            _onUserChanged?.Invoke(null);
            if (wasAuthenticated)
            {
                _onAuthenticationChanged?.Invoke(false);
            }
            NotifyStateChanged();
        }

        /// <summary>
        /// Refresh current user data from the server
        /// </summary>
        public async Task RefreshUserDataAsync()
        {
            if (!IsAuthenticated) return;

            try
            {
                var refreshedUser = await _authService.GetCurrentUserAsync();
                if (refreshedUser != null)
                {
                    _currentUser = refreshedUser;
                    _onUserChanged?.Invoke(_currentUser);
                    NotifyStateChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing user data: {ex.Message}");
            }
        }

        /// <summary>
        /// Load portfolio users with caching
        /// </summary>
        public async Task LoadPortfolioUsersAsync(bool forceRefresh = false)
        {
            const string cacheKey = "portfolio_users";
            
            if (!forceRefresh && IsCacheValid(cacheKey) && _portfolioUsers != null)
            {
                return;
            }

            try
            {
                _portfolioUsers = await _portfolioUserService.GetAllPortfolioUsersAsync();
                UpdateCacheTimestamp(cacheKey);
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading portfolio users: {ex.Message}");
            }
        }

        /// <summary>
        /// Load current user's projects with caching
        /// </summary>
        public async Task LoadUserProjectsAsync(bool forceRefresh = false)
        {
            if (!IsAuthenticated) return;

            const string cacheKey = "user_projects";
            
            if (!forceRefresh && IsCacheValid(cacheKey) && _userProjects != null)
            {
                return;
            }

            try
            {
                _userProjects = await _projectService.GetMyProjectsAsync();
                UpdateCacheTimestamp(cacheKey);
                _onProjectsChanged?.Invoke(_userProjects);
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user projects: {ex.Message}");
            }
        }

        /// <summary>
        /// Load current user's skills with caching
        /// </summary>
        public async Task LoadUserSkillsAsync(bool forceRefresh = false)
        {
            if (!IsAuthenticated) return;

            const string cacheKey = "user_skills";
            
            if (!forceRefresh && IsCacheValid(cacheKey) && _userSkills != null)
            {
                return;
            }

            try
            {
                _userSkills = await _skillService.GetAllSkillsAsync();
                UpdateCacheTimestamp(cacheKey);
                _onSkillsChanged?.Invoke(_userSkills);
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user skills: {ex.Message}");
            }
        }

        /// <summary>
        /// Load current portfolio user data
        /// </summary>
        public async Task LoadCurrentPortfolioUserAsync(bool forceRefresh = false)
        {
            if (!IsAuthenticated) return;

            const string cacheKey = "current_portfolio_user";
            
            if (!forceRefresh && IsCacheValid(cacheKey) && _currentPortfolioUser != null)
            {
                return;
            }

            try
            {
                _currentPortfolioUser = await _portfolioUserService.GetMyPortfolioUserAsync();
                UpdateCacheTimestamp(cacheKey);
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading current portfolio user: {ex.Message}");
            }
        }

        /// <summary>
        /// Invalidate projects cache and reload
        /// </summary>
        public async Task InvalidateProjectsCache()
        {
            _userProjects = null;
            ClearCacheTimestamp("user_projects");
            await LoadUserProjectsAsync(true);
        }

        /// <summary>
        /// Invalidate skills cache and reload
        /// </summary>
        public async Task InvalidateSkillsCache()
        {
            _userSkills = null;
            ClearCacheTimestamp("user_skills");
            await LoadUserSkillsAsync(true);
        }

        /// <summary>
        /// Invalidate portfolio users cache and reload
        /// </summary>
        public async Task InvalidatePortfolioUsersCache()
        {
            _portfolioUsers = null;
            ClearCacheTimestamp("portfolio_users");
            await LoadPortfolioUsersAsync(true);
        }

        /// <summary>
        /// Clear all cached data
        /// </summary>
        public async Task InvalidateAllCache()
        {
            _portfolioUsers = null;
            _userProjects = null;
            _userSkills = null;
            _currentPortfolioUser = null;
            _cacheTimestamps.Clear();
            NotifyStateChanged();
        }

        /// <summary>
        /// Check if authentication token is expired
        /// </summary>
        public bool IsTokenExpired()
        {
            if (_tokenExpiry == null) return true;
            return DateTime.UtcNow >= _tokenExpiry.Value.AddMinutes(-5); // 5 minute buffer
        }

        /// <summary>
        /// Generic method to get cached data
        /// </summary>
        public async Task<T?> GetCachedDataAsync<T>(string key) where T : class
        {
            // Implementation for generic cached data if needed
            // For now, return null as we're using strongly-typed properties
            await Task.CompletedTask;
            return null;
        }

        /// <summary>
        /// Generic method to set cached data
        /// </summary>
        public async Task SetCachedDataAsync<T>(string key, T data, TimeSpan? expiry = null) where T : class
        {
            // Implementation for generic cached data if needed
            await Task.CompletedTask;
        }

        /// <summary>
        /// Clear specific cached data
        /// </summary>
        public async Task ClearCachedDataAsync(string key)
        {
            ClearCacheTimestamp(key);
            await Task.CompletedTask;
        }

        // Private helper methods
        private static void NotifyStateChanged()
        {
            _onStateChanged?.Invoke();
        }

        private DateTime GetTokenExpiry(string token)
        {
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.ValidTo;
            }
            catch
            {
                // If we can't read the token, assume it expires in 1 hour
                return DateTime.UtcNow.AddHours(1);
            }
        }

        private static bool IsCacheValid(string key)
        {
            if (!_cacheTimestamps.ContainsKey(key)) return false;
            return DateTime.UtcNow - _cacheTimestamps[key] < _defaultCacheExpiry;
        }

        private static void UpdateCacheTimestamp(string key)
        {
            _cacheTimestamps[key] = DateTime.UtcNow;
        }

        private static void ClearCacheTimestamp(string key)
        {
            _cacheTimestamps.Remove(key);
        }

        public void Dispose()
        {
            // Clean up any resources if needed
            // Events will be cleaned up automatically when instances are disposed
        }
    }
}