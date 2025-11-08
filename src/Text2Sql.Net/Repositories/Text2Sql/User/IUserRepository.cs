using Text2Sql.Net.Base;

namespace Text2Sql.Net.Repositories.Text2Sql.User
{
    /// <summary>
    /// 用户仓储接口
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<User?> GetByUsernameAsync(string username);
    }
}
