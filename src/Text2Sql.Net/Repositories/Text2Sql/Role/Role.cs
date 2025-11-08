using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;

namespace Text2Sql.Net.Repositories.Text2Sql.Role
{
    /// <summary>
    /// 角色
    /// </summary>
    [SugarTable("Roles")]
    public class Role
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 角色名称
        /// </summary>
        [SugarColumn(IsNullable = false)]
        [Required(ErrorMessage = "请输入角色名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 角色代码
        /// </summary>
        [SugarColumn(IsNullable = false)]
        [Required(ErrorMessage = "请输入角色代码")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public string? Description { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public bool IsEnabled { get; set; } = true;

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
