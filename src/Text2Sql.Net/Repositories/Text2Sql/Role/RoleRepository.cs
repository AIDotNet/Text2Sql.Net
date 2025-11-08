using Text2Sql.Net.Base;

namespace Text2Sql.Net.Repositories.Text2Sql.Role
{
    /// <summary>
    /// 角色仓储实现
    /// </summary>
    public class RoleRepository : Repository<Role>, IRoleRepository
    {
        public RoleRepository() : base(SqlSugarHelper.SqlScope())
        {
        }

        /// <summary>
        /// 根据角色代码获取角色
        /// </summary>
        public async Task<Role?> GetByCodeAsync(string code)
        {
            return await Context.Queryable<Role>()
                .FirstAsync(x => x.Code == code);
        }
    }
}
