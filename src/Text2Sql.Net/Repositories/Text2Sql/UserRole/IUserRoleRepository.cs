using Text2Sql.Net.Base;

namespace Text2Sql.Net.Repositories.Text2Sql.UserRole
{
    /// <summary>
    /// 用户角色关联仓储接口
    /// </summary>
    public interface IUserRoleRepository : IRepository<UserRole>
    {
        /// <summary>
        /// 根据用户ID获取角色关联
        /// </summary>
        Task<List<UserRole>> GetByUserIdAsync(string userId);

        /// <summary>
        /// 根据角色ID获取用户关联
        /// </summary>
        Task<List<UserRole>> GetByRoleIdAsync(string roleId);

        /// <summary>
        /// 删除用户的所有角色关联
        /// </summary>
        Task DeleteByUserIdAsync(string userId);
    }
}
