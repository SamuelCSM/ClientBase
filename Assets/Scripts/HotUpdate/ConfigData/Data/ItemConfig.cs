// ==========================================
// 自动生成的配置类: ItemConfig
// 来源表: item_config
// 生成时间: 2026-03-03 09:48:08
// 警告: 请勿手动修改此文件！
// ==========================================

using System;
using System.Collections.Generic;
using SQLite;
using Framework.Data;

namespace HotUpdate.Config.Data
{
    /// <summary>
    /// ItemConfig 配置类
    /// </summary>
    [Table("item_config")]
    [Serializable]
    public class ItemConfig
    {
        /// <summary>
        /// 主键id
        /// </summary>
        [PrimaryKey]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 名字
        /// </summary>
        [Column("Name")]
        public string Name { get; set; }

    }

}
