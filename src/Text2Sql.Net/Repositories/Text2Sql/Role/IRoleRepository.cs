using Text2Sql.Net.Base;

namespace Text2Sql.Net.Repositories.Text2Sql.Role
{
    /// <summary>
    /// 角色仓储接口
    /// </summary>
    public interface IRoleRepository : IRepository<Role>
    {
        /// <summary>
        /// 根据角色代码获取角色
        /// </summary>
        Task<Role?> GetByCodeAsync(string code);
    }
}
