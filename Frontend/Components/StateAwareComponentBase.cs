using Microsoft.AspNetCore.Components;
using Frontend.Services;

namespace Frontend.Components
{
    /// <summary>
    /// Base component that provides easy access to UserSessionService and automatic state updates
    /// </summary>
    public abstract class StateAwareComponentBase : ComponentBase, IDisposable
    {
        [Inject] protected IUserSessionService UserSession { get; set; } = default!;

        protected override void OnInitialized()
        {
            // Subscribe to state changes
            UserSession.OnStateChanged += OnStateChanged;
            UserSession.OnUserChanged += OnUserChanged;
            UserSession.OnAuthenticationChanged += OnAuthenticationChanged;
            UserSession.OnProjectsChanged += OnProjectsChanged;
            UserSession.OnSkillsChanged += OnSkillsChanged;

            base.OnInitialized();
        }

        /// <summary>
        /// Called when any state in UserSessionService changes
        /// </summary>
        protected virtual void OnStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Called when the current user changes
        /// </summary>
        protected virtual void OnUserChanged(SkillSnap.Shared.Models.UserDto? user)
        {
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Called when authentication state changes
        /// </summary>
        protected virtual void OnAuthenticationChanged(bool isAuthenticated)
        {
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Called when projects data changes
        /// </summary>
        protected virtual void OnProjectsChanged(List<SkillSnap.Shared.Models.Project>? projects)
        {
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Called when skills data changes
        /// </summary>
        protected virtual void OnSkillsChanged(List<SkillSnap.Shared.Models.Skill>? skills)
        {
            InvokeAsync(StateHasChanged);
        }

        public virtual void Dispose()
        {
            // Unsubscribe from events to prevent memory leaks
            if (UserSession != null)
            {
                UserSession.OnStateChanged -= OnStateChanged;
                UserSession.OnUserChanged -= OnUserChanged;
                UserSession.OnAuthenticationChanged -= OnAuthenticationChanged;
                UserSession.OnProjectsChanged -= OnProjectsChanged;
                UserSession.OnSkillsChanged -= OnSkillsChanged;
            }
        }
    }
}