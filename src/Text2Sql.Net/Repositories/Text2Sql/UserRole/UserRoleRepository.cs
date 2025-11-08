using Text2Sql.Net.Base;

namespace Text2Sql.Net.Repositories.Text2Sql.UserRole
{
    /// <summary>
    /// 用户角色关联仓储实现
    /// </summary>
    public class UserRoleRepository : Repository<UserRole>, IUserRoleRepository
    {
        public UserRoleRepository() : base(SqlSugarHelper.SqlScope())
        {
        }

        /// <summary>
        /// 根据用户ID获取角色关联
        /// </summary>
        public async Task<List<UserRole>> GetByUserIdAsync(string userId)
        {
            return await Context.Queryable<UserRole>()
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        /// <summary>
        /// 根据角色ID获取用户关联
        /// </summary>
        public async Task<List<UserRole>> GetByRoleIdAsync(string roleId)
        {
            return await Context.Queryable<UserRole>()
                .Where(x => x.RoleId == roleId)
                .ToListAsync();
        }

        /// <summary>
        /// 删除用户的所有角色关联
        /// </summary>
        public async Task DeleteByUserIdAsync(string userId)
        {
            await Context.Deleteable<UserRole>()
                .Where(x => x.UserId == userId)
                .ExecuteCommandAsync();
        }
    }
}
