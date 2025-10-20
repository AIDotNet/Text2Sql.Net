using AntDesign;
using AntDesign.ProLayout;
using Text2Sql.Net.Web.Models;
using Text2Sql.Net.Web.Services.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Text2Sql.Net.Web.Components
{
    public partial class RightContent
    {
        private CurrentUser _currentUser = new CurrentUser();
        private NoticeIconData[] _notifications = { };
        private NoticeIconData[] _messages = { };
        private NoticeIconData[] _events = { };
        private int _count = 0;
        private string? _displayName;

        private List<AutoCompleteDataItem<string>> DefaultOptions { get; set; } = new List<AutoCompleteDataItem<string>>
        {
            new AutoCompleteDataItem<string>
            {
                Label = "umi ui",
                Value = "umi ui"
            },
            new AutoCompleteDataItem<string>
            {
                Label = "Pro Table",
                Value = "Pro Table"
            },
            new AutoCompleteDataItem<string>
            {
                Label = "Pro Layout",
                Value = "Pro Layout"
            }
        };

        public AvatarMenuItem[] AvatarMenuItems { get; set; } = new AvatarMenuItem[]
        {
            new() { Key = "center", IconType = "user", Option = "个人中心"},
            new() { Key = "setting", IconType = "setting", Option = "个人设置"},
            new() { IsDivider = true },
            new() { Key = "logout", IconType = "logout", Option = "退出登录"}
        };

    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    [Inject] protected MessageService MessageService { get; set; } = default!;

    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject] protected ILoginService LoginService { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            SetClassMap();
            AuthenticationStateProvider.AuthenticationStateChanged += HandleAuthenticationStateChanged;
            await UpdateUserAsync();

        }

        /// <summary>
        /// 设置组件的CSS类映射
        /// </summary>
        protected void SetClassMap()
        {
            ClassMapper
                .Clear()
                .Add("right");
        }

        /// <summary>
        /// 处理用户菜单项选择事件
        /// </summary>
        /// <param name="item">选中的菜单项</param>
        public void HandleSelectUser(MenuItem item)
        {
            switch (item.Key)
            {
                case "center":
                    NavigationManager.NavigateTo("/account/center");
                    break;
                case "setting":
                    NavigationManager.NavigateTo("/account/settings");
                    break;
                case "logout":
                    _ = LogoutAsync();
                    break;
            }
        }

        /// <summary>
        /// 处理语言选择菜单项事件
        /// </summary>
        /// <param name="item">选中的语言菜单项</param>
        public void HandleSelectLang(MenuItem item)
        {
        }

        /// <summary>
        /// 处理清空通知、消息或事件列表的事件
        /// </summary>
        /// <param name="key">要清空的项目类型（notification/message/event）</param>
        /// <returns>异步任务</returns>
        public async Task HandleClear(string key)
        {
            switch (key)
            {
                case "notification":
                    _notifications = new NoticeIconData[] { };
                    break;
                case "message":
                    _messages = new NoticeIconData[] { };
                    break;
                case "event":
                    _events = new NoticeIconData[] { };
                    break;
            }
            await MessageService.Success($"清空了{key}");
        }

        /// <summary>
        /// 处理查看更多的事件
        /// </summary>
        /// <param name="key">要查看更多的项目类型</param>
        /// <returns>异步任务</returns>
        public async Task HandleViewMore(string key)
        {
            await MessageService.Info("Click on view more");
        }

        private async Task UpdateUserAsync()
        {
            var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            UpdateFromState(state);
        }

        private void HandleAuthenticationStateChanged(Task<AuthenticationState> task)
        {
            _ = UpdateFromTaskAsync(task);
        }

        private async Task UpdateFromTaskAsync(Task<AuthenticationState> task)
        {
            var state = await task;
            UpdateFromState(state);
            await InvokeAsync(StateHasChanged);
        }

        private void UpdateFromState(AuthenticationState state)
        {
            if (state.User.Identity?.IsAuthenticated == true)
            {
                _displayName = state.User.Identity.Name;
                if (string.IsNullOrWhiteSpace(_displayName))
                {
                    _displayName = state.User.FindFirstValue(ClaimTypes.Name) ?? state.User.Identity.Name;
                }
            }
            else
            {
                _displayName = null;
            }
        }

        private void NavigateToLogin()
        {
            NavigationManager.NavigateTo("/login", true);
        }

        private async Task LogoutAsync()
        {
            await LoginService.SignOutAsync();
            await MessageService.Success("已退出登录");
            NavigationManager.NavigateTo("/login", true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AuthenticationStateProvider.AuthenticationStateChanged -= HandleAuthenticationStateChanged;
            }

            base.Dispose(disposing);
        }
    }
}