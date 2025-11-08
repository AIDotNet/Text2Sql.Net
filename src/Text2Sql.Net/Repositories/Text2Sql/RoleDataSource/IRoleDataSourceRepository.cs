using Text2Sql.Net.Base;

namespace Text2Sql.Net.Repositories.Text2Sql.RoleDataSource
{
    /// <summary>
    /// 角色数据源关联仓储接口
    /// </summary>
    public interface IRoleDataSourceRepository : IRepository<RoleDataSource>
    {
        /// <summary>
        /// 根据角色ID获取数据源关联
        /// </summary>
        Task<List<RoleDataSource>> GetByRoleIdAsync(string roleId);

        /// <summary>
        /// 根据数据源ID获取角色关联
        /// </summary>
        Task<List<RoleDataSource>> GetByDataSourceIdAsync(string dataSourceId);

        /// <summary>
        /// 删除角色的所有数据源关联
        /// </summary>
        Task DeleteByRoleIdAsync(string roleId);
    }
}
