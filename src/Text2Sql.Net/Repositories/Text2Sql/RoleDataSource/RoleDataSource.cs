using SqlSugar;
using System;

namespace Text2Sql.Net.Repositories.Text2Sql.RoleDataSource
{
    /// <summary>
    /// 角色数据源关联
    /// </summary>
    [SugarTable("RoleDataSources")]
    public class RoleDataSource
    {
        /// <summary>
        /// 关联ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 角色ID
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public string RoleId { get; set; } = string.Empty;

        /// <summary>
        /// 数据源ID (DatabaseConnectionConfig Id)
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public string DataSourceId { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
