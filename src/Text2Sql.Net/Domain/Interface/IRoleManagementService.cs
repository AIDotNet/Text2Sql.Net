using Text2Sql.Net.Repositories.Text2Sql.Role;
using Text2Sql.Net.Repositories.Text2Sql.DatabaseConnection;

namespace Text2Sql.Net.Domain.Interface
{
    /// <summary>
    /// 角色管理服务接口
    /// </summary>
    public interface IRoleManagementService
    {
        /// <summary>
        /// 获取角色列表
        /// </summary>
        Task<List<Role>> GetRolesAsync();

        /// <summary>
        /// 根据ID获取角色
        /// </summary>
        Task<Role?> GetRoleByIdAsync(string id);

        /// <summary>
        /// 创建角色
        /// </summary>
        Task<Role> CreateRoleAsync(Role role);

        /// <summary>
        /// 更新角色
        /// </summary>
        Task UpdateRoleAsync(Role role);

        /// <summary>
        /// 删除角色
        /// </summary>
        Task DeleteRoleAsync(string id);

        /// <summary>
        /// 获取角色的数据源列表
        /// </summary>
        Task<List<DatabaseConnectionConfig>> GetRoleDataSourcesAsync(string roleId);

        /// <summary>
        /// 为角色分配数据源
        /// </summary>
        Task AssignDataSourcesToRoleAsync(string roleId, List<string> dataSourceIds);
    }
}
