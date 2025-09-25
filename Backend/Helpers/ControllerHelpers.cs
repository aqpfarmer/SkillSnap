using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SkillSnap.Backend.Controllers
{
    /// <summary>
    /// Helper methods for common controller operations
    /// </summary>
    public static class ControllerHelpers
    {
        /// <summary>
        /// Gets the current user's ID from claims
        /// </summary>
        public static string? GetCurrentUserId(this ControllerBase controller)
        {
            return controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Gets the current user's email from claims
        /// </summary>
        public static string? GetCurrentUserEmail(this ControllerBase controller)
        {
            return controller.User.FindFirst("email")?.Value;
        }

        /// <summary>
        /// Checks if the current user has the specified role
        /// </summary>
        public static bool HasRole(this ControllerBase controller, string role)
        {
            return controller.User.IsInRole(role);
        }

        /// <summary>
        /// Returns a standardized unauthorized response
        /// </summary>
        public static IActionResult UnauthorizedResponse(this ControllerBase controller, string message = "Unauthorized access")
        {
            return controller.Unauthorized(new { error = message, timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Returns a standardized bad request response
        /// </summary>
        public static IActionResult BadRequestResponse(this ControllerBase controller, string message = "Bad request")
        {
            return controller.BadRequest(new { error = message, timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Returns a standardized forbidden response
        /// </summary>
        public static IActionResult ForbiddenResponse(this ControllerBase controller, string message = "Access denied")
        {
            return controller.StatusCode(403, new { error = message, timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Returns a standardized not found response
        /// </summary>
        public static IActionResult NotFoundResponse(this ControllerBase controller, string message = "Resource not found")
        {
            return controller.NotFound(new { error = message, timestamp = DateTime.UtcNow });
        }
    }
}