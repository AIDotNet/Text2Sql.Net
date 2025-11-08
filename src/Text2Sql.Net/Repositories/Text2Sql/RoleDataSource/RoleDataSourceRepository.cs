using Text2Sql.Net.Base;

namespace Text2Sql.Net.Repositories.Text2Sql.RoleDataSource
{
    /// <summary>
    /// 角色数据源关联仓储实现
    /// </summary>
    public class RoleDataSourceRepository : Repository<RoleDataSource>, IRoleDataSourceRepository
    {
        public RoleDataSourceRepository() : base(SqlSugarHelper.SqlScope())
        {
        }

        /// <summary>
        /// 根据角色ID获取数据源关联
        /// </summary>
        public async Task<List<RoleDataSource>> GetByRoleIdAsync(string roleId)
        {
            return await Context.Queryable<RoleDataSource>()
                .Where(x => x.RoleId == roleId)
                .ToListAsync();
        }

        /// <summary>
        /// 根据数据源ID获取角色关联
        /// </summary>
        public async Task<List<RoleDataSource>> GetByDataSourceIdAsync(string dataSourceId)
        {
            return await Context.Queryable<RoleDataSource>()
                .Where(x => x.DataSourceId == dataSourceId)
                .ToListAsync();
        }

        /// <summary>
        /// 删除角色的所有数据源关联
        /// </summary>
        public async Task DeleteByRoleIdAsync(string roleId)
        {
            await Context.Deleteable<RoleDataSource>()
                .Where(x => x.RoleId == roleId)
                .ExecuteCommandAsync();
        }
    }
}
