namespace Frontend.Constants
{
    /// <summary>
    /// Frontend application constants
    /// </summary>
    public static class FrontendConstants
    {
        /// <summary>
        /// Local storage keys
        /// </summary>
        public static class LocalStorageKeys
        {
            public const string AuthToken = "authToken";
            public const string CurrentUser = "currentUser";
            public const string TokenExpiry = "tokenExpiry";
            public const string UserPreferences = "userPreferences";
        }

        /// <summary>
        /// API endpoints
        /// </summary>
        public static class ApiEndpoints
        {
            public const string Login = "api/auth/login";
            public const string Register = "api/auth/register";
            public const string CreateUser = "api/auth/create-user";
            public const string Skills = "api/Skills";
            public const string Projects = "api/Projects";
            public const string PortfolioUsers = "api/PortfolioUsers";
            public const string Metrics = "api/Metrics";
        }

        /// <summary>
        /// Navigation routes
        /// </summary>
        public static class Routes
        {
            public const string Home = "/";
            public const string Login = "/login";
            public const string Profile = "/profile";
            public const string Projects = "/projects";
            public const string Metrics = "/metrics";
            public const string Unauthorized = "/unauthorized";
        }

        /// <summary>
        /// Cache durations for frontend data
        /// </summary>
        public static class CacheDurations
        {
            public static readonly TimeSpan UserData = TimeSpan.FromMinutes(15);
            public static readonly TimeSpan ProjectData = TimeSpan.FromMinutes(10);
            public static readonly TimeSpan SkillData = TimeSpan.FromMinutes(10);
            public static readonly TimeSpan MetricsData = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// UI messages
        /// </summary>
        public static class Messages
        {
            public const string LoginRequired = "Please log in to access this page.";
            public const string AccessDenied = "You don't have permission to access this page.";
            public const string LoadingData = "Loading...";
            public const string NoDataFound = "No data found.";
            public const string OperationSuccessful = "Operation completed successfully.";
            public const string OperationFailed = "Operation failed. Please try again.";
        }

        /// <summary>
        /// Component refresh intervals
        /// </summary>
        public static class RefreshIntervals
        {
            public static readonly TimeSpan Metrics = TimeSpan.FromSeconds(30);
            public static readonly TimeSpan UserData = TimeSpan.FromMinutes(5);
            public static readonly TimeSpan ProjectData = TimeSpan.FromMinutes(2);
        }
    }
}