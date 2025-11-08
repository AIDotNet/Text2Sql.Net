using Text2Sql.Net.Domain.Interface;
using Text2Sql.Net.Repositories.Text2Sql.User;
using Text2Sql.Net.Repositories.Text2Sql.Role;
using Text2Sql.Net.Repositories.Text2Sql.UserRole;

namespace Text2Sql.Net.Domain.Service
{
    /// <summary>
    /// 用户管理服务实现
    /// </summary>
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;

        public UserManagementService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
        }

        /// <summary>
        /// 获取用户列表
        /// </summary>
        public async Task<List<User>> GetUsersAsync()
        {
            return await _userRepository.GetListAsync();
        }

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        public async Task<User?> GetUserByIdAsync(string id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        public async Task<User> CreateUserAsync(User user)
        {
            user.Id = Guid.NewGuid().ToString();
            user.CreateTime = DateTime.Now;
            await _userRepository.InsertAsync(user);
            return user;
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        public async Task UpdateUserAsync(User user)
        {
            user.UpdateTime = DateTime.Now;
            await _userRepository.UpdateAsync(user);
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        public async Task DeleteUserAsync(string id)
        {
            // 先删除用户的角色关联
            await _userRoleRepository.DeleteByUserIdAsync(id);
            // 再删除用户
            await _userRepository.DeleteAsync(id);
        }

        /// <summary>
        /// 获取用户的角色列表
        /// </summary>
        public async Task<List<Role>> GetUserRolesAsync(string userId)
        {
            var userRoles = await _userRoleRepository.GetByUserIdAsync(userId);
            var roleIds = userRoles.Select(x => x.RoleId).ToList();

            if (!roleIds.Any())
            {
                return new List<Role>();
            }

            var roles = await _roleRepository.GetListAsync();
            return roles.Where(x => roleIds.Contains(x.Id)).ToList();
        }

        /// <summary>
        /// 为用户分配角色
        /// </summary>
        public async Task AssignRolesToUserAsync(string userId, List<string> roleIds)
        {
            // 先删除用户的所有角色关联
            await _userRoleRepository.DeleteByUserIdAsync(userId);

            // 然后添加新的角色关联
            foreach (var roleId in roleIds)
            {
                var userRole = new Repositories.Text2Sql.UserRole.UserRole
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    RoleId = roleId,
                    CreateTime = DateTime.Now
                };
                await _userRoleRepository.InsertAsync(userRole);
            }
        }
    }
}
