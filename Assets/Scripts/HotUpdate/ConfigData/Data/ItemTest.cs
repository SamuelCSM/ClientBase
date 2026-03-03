// ==========================================
// 自动生成的配置类: ItemTest
// 来源表: item_test
// 生成时间: 2026-03-03 11:04:03
// 警告: 请勿手动修改此文件！
// ==========================================

using System;
using System.Collections.Generic;
using SQLite;
using Framework.Data;

namespace HotUpdate.Config.Data
{
    /// <summary>
    /// ItemTest 配置类
    /// </summary>
    [Table("item_test")]
    [Serializable]
    public class ItemTest
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [PrimaryKey]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 测试
        /// </summary>
        [Column("NameList")]
        public List<ItemConfig> NameList { get; set; }

    }

}
