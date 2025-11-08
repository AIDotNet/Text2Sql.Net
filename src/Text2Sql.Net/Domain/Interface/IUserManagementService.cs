using Text2Sql.Net.Repositories.Text2Sql.User;
using Text2Sql.Net.Repositories.Text2Sql.Role;

namespace Text2Sql.Net.Domain.Interface
{
    /// <summary>
    /// 用户管理服务接口
    /// </summary>
    public interface IUserManagementService
    {
        /// <summary>
        /// 获取用户列表
        /// </summary>
        Task<List<User>> GetUsersAsync();

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        Task<User?> GetUserByIdAsync(string id);

        /// <summary>
        /// 创建用户
        /// </summary>
        Task<User> CreateUserAsync(User user);

        /// <summary>
        /// 更新用户
        /// </summary>
        Task UpdateUserAsync(User user);

        /// <summary>
        /// 删除用户
        /// </summary>
        Task DeleteUserAsync(string id);

        /// <summary>
        /// 获取用户的角色列表
        /// </summary>
        Task<List<Role>> GetUserRolesAsync(string userId);

        /// <summary>
        /// 为用户分配角色
        /// </summary>
        Task AssignRolesToUserAsync(string userId, List<string> roleIds);
    }
}
