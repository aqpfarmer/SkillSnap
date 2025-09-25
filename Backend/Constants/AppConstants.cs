namespace SkillSnap.Backend.Constants
{
    /// <summary>
    /// Application-wide constants
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// Role names used throughout the application
        /// </summary>
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string Manager = "Manager";
            public const string User = "User";
        }

        /// <summary>
        /// Cache durations for different data types
        /// </summary>
        public static class CacheDurations
        {
            public static readonly TimeSpan Standard = TimeSpan.FromMinutes(15);
            public static readonly TimeSpan Short = TimeSpan.FromMinutes(10);
            public static readonly TimeSpan Long = TimeSpan.FromHours(1);
            public static readonly TimeSpan UserSpecific = TimeSpan.FromMinutes(10);
        }

        /// <summary>
        /// Default values for new portfolio users
        /// </summary>
        public static class PortfolioDefaults
        {
            public const string DefaultBio = "Welcome to SkillSnap! Start building your portfolio by adding your skills and projects.";
            public const string DefaultProfileImageUrl = "https://via.placeholder.com/150?text=User";
        }

        /// <summary>
        /// API response messages
        /// </summary>
        public static class Messages
        {
            public const string UserIdNotFound = "User ID not found in token.";
            public const string UserNotFound = "User not found.";
            public const string UnauthorizedAccess = "Unauthorized access.";
            public const string ResourceNotFound = "Resource not found.";
            public const string InvalidRequest = "Invalid request.";
            public const string AccessDenied = "Access denied.";
            public const string OperationSuccessful = "Operation completed successfully.";
        }

        /// <summary>
        /// Validation constants
        /// </summary>
        public static class Validation
        {
            public const int MaxSkillNameLength = 100;
            public const int MaxProjectTitleLength = 200;
            public const int MaxDescriptionLength = 1000;
            public const int MaxBioLength = 500;
        }
    }
}