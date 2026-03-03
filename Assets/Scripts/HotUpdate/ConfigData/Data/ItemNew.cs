// ==========================================
// 自动生成的配置类: ItemNew
// 来源表: item_new
// 生成时间: 2026-03-02 18:02:27
// 警告: 请勿手动修改此文件！
// ==========================================

using System;
using System.Collections.Generic;
using SQLite;
using Framework.Data;

namespace HotUpdate.Config.Data
{
    /// <summary>
    /// ItemNew 配置类
    /// </summary>
    [Table("item_new")]
    [Serializable]
    public class ItemNew
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [PrimaryKey]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 物品
        /// </summary>
        [Column("ItemConfigType")]
        public ItemConfig[] ItemConfigType { get; set; }

    }

}
