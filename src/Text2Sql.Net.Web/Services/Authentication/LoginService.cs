using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Text2Sql.Net.Web.Models;
using Text2Sql.Net.Web.Options;

namespace Text2Sql.Net.Web.Services.Authentication
{
    public sealed class LoginService : ILoginService
    {
        private readonly IOptionsMonitor<AuthenticationOptions> _options;
        private readonly SessionAuthenticationStateProvider _stateProvider;
        private readonly ILogger<LoginService> _logger;

        public LoginService(
            IOptionsMonitor<AuthenticationOptions> options,
            SessionAuthenticationStateProvider stateProvider,
            ILogger<LoginService> logger)
        {
            _options = options;
            _stateProvider = stateProvider;
            _logger = logger;
        }

        public async Task<LoginResult> SignInAsync(string username, string password)
        {
            var credentials = _options.CurrentValue.Users ?? Array.Empty<UserCredential>();
            var user = credentials.FirstOrDefault(
                x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                _logger.LogWarning("登录失败，未知的用户名 {Username}", username);
                return LoginResult.Fail("用户名或密码错误");
            }

            if (!string.Equals(user.Password, password, StringComparison.Ordinal))
            {
                _logger.LogWarning("登录失败，用户 {Username} 密码不正确", username);
                return LoginResult.Fail("用户名或密码错误");
            }

            var session = new LoginSession
            {
                Username = user.Username,
                DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName
            };

            await _stateProvider.SetAuthenticatedUserAsync(session);
            _logger.LogInformation("用户 {Username} 登录成功", username);

            return LoginResult.Success(session.DisplayName);
        }

        public async Task SignOutAsync()
        {
            await _stateProvider.ClearStateAsync();
            _logger.LogInformation("用户注销成功");
        }
    }
}
