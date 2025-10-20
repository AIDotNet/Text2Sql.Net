using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Extensions.Logging;
using Text2Sql.Net.Web.Models;

namespace Text2Sql.Net.Web.Services.Authentication
{
    public sealed class SessionAuthenticationStateProvider : AuthenticationStateProvider
    {
        private const string StorageKey = "text2sql.auth.state";
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly ILogger<SessionAuthenticationStateProvider> _logger;

        public SessionAuthenticationStateProvider(
            ProtectedSessionStorage sessionStorage,
            ILogger<SessionAuthenticationStateProvider> logger)
        {
            _sessionStorage = sessionStorage;
            _logger = logger;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var storedResult = await _sessionStorage.GetAsync<LoginSession>(StorageKey);
                if (storedResult.Success && storedResult.Value?.IsAuthenticated == true)
                {
                    return BuildAuthenticationState(storedResult.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取登录状态失败");
            }

            return BuildAnonymousState();
        }

        public async Task SetAuthenticatedUserAsync(LoginSession session)
        {
            await _sessionStorage.SetAsync(StorageKey, session);
            NotifyAuthenticationStateChanged(Task.FromResult(BuildAuthenticationState(session)));
        }

        public async Task ClearStateAsync()
        {
            await _sessionStorage.DeleteAsync(StorageKey);
            NotifyAuthenticationStateChanged(Task.FromResult(BuildAnonymousState()));
        }

        private static AuthenticationState BuildAuthenticationState(LoginSession session)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, session.Username),
                new Claim(ClaimTypes.Name, session.DisplayName ?? session.Username)
            }, "Text2SqlAuth");

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        private static AuthenticationState BuildAnonymousState()
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }
}
