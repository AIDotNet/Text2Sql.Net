using Text2Sql.Net.Domain.Interface;
using Text2Sql.Net.Repositories.Text2Sql.Role;
using Text2Sql.Net.Repositories.Text2Sql.RoleDataSource;
using Text2Sql.Net.Repositories.Text2Sql.DatabaseConnection;

namespace Text2Sql.Net.Domain.Service
{
    /// <summary>
    /// 角色管理服务实现
    /// </summary>
    public class RoleManagementService : IRoleManagementService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IRoleDataSourceRepository _roleDataSourceRepository;
        private readonly IDatabaseConnectionConfigRepository _databaseConnectionConfigRepository;

        public RoleManagementService(
            IRoleRepository roleRepository,
            IRoleDataSourceRepository roleDataSourceRepository,
            IDatabaseConnectionConfigRepository databaseConnectionConfigRepository)
        {
            _roleRepository = roleRepository;
            _roleDataSourceRepository = roleDataSourceRepository;
            _databaseConnectionConfigRepository = databaseConnectionConfigRepository;
        }

        /// <summary>
        /// 获取角色列表
        /// </summary>
        public async Task<List<Role>> GetRolesAsync()
        {
            return await _roleRepository.GetListAsync();
        }

        /// <summary>
        /// 根据ID获取角色
        /// </summary>
        public async Task<Role?> GetRoleByIdAsync(string id)
        {
            return await _roleRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        public async Task<Role> CreateRoleAsync(Role role)
        {
            role.Id = Guid.NewGuid().ToString();
            role.CreateTime = DateTime.Now;
            await _roleRepository.InsertAsync(role);
            return role;
        }

        /// <summary>
        /// 更新角色
        /// </summary>
        public async Task UpdateRoleAsync(Role role)
        {
            role.UpdateTime = DateTime.Now;
            await _roleRepository.UpdateAsync(role);
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        public async Task DeleteRoleAsync(string id)
        {
            // 先删除角色的数据源关联
            await _roleDataSourceRepository.DeleteByRoleIdAsync(id);
            // 再删除角色
            await _roleRepository.DeleteAsync(id);
        }

        /// <summary>
        /// 获取角色的数据源列表
        /// </summary>
        public async Task<List<DatabaseConnectionConfig>> GetRoleDataSourcesAsync(string roleId)
        {
            var roleDataSources = await _roleDataSourceRepository.GetByRoleIdAsync(roleId);
            var dataSourceIds = roleDataSources.Select(x => x.DataSourceId).ToList();

            if (!dataSourceIds.Any())
            {
                return new List<DatabaseConnectionConfig>();
            }

            var dataSources = await _databaseConnectionConfigRepository.GetListAsync();
            return dataSources.Where(x => dataSourceIds.Contains(x.Id)).ToList();
        }

        /// <summary>
        /// 为角色分配数据源
        /// </summary>
        public async Task AssignDataSourcesToRoleAsync(string roleId, List<string> dataSourceIds)
        {
            // 先删除角色的所有数据源关联
            await _roleDataSourceRepository.DeleteByRoleIdAsync(roleId);

            // 然后添加新的数据源关联
            foreach (var dataSourceId in dataSourceIds)
            {
                var roleDataSource = new Repositories.Text2Sql.RoleDataSource.RoleDataSource
                {
                    Id = Guid.NewGuid().ToString(),
                    RoleId = roleId,
                    DataSourceId = dataSourceId,
                    CreateTime = DateTime.Now
                };
                await _roleDataSourceRepository.InsertAsync(roleDataSource);
            }
        }
    }
}
