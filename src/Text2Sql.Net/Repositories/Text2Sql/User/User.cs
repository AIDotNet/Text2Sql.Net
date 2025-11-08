using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;

namespace Text2Sql.Net.Repositories.Text2Sql.User
{
    /// <summary>
    /// 用户
    /// </summary>
    [SugarTable("Users")]
    public class User
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 用户名
        /// </summary>
        [SugarColumn(IsNullable = false)]
        [Required(ErrorMessage = "请输入用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [SugarColumn(IsNullable = false)]
        [Required(ErrorMessage = "请输入密码")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public string? Email { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 描述
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public string? Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }
    }
}
