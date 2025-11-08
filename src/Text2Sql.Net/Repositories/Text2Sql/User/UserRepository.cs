using Text2Sql.Net.Base;

namespace Text2Sql.Net.Repositories.Text2Sql.User
{
    /// <summary>
    /// 用户仓储实现
    /// </summary>
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository() : base(SqlSugarHelper.SqlScope())
        {
        }

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await Context.Queryable<User>()
                .FirstAsync(x => x.Username == username);
        }
    }
}
