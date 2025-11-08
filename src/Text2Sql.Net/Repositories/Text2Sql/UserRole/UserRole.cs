using SqlSugar;
using System;

namespace Text2Sql.Net.Repositories.Text2Sql.UserRole
{
    /// <summary>
    /// 用户角色关联
    /// </summary>
    [SugarTable("UserRoles")]
    public class UserRole
    {
        /// <summary>
        /// 关联ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 用户ID
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 角色ID
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public string RoleId { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
