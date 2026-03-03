// ==========================================
// 自动生成的配置表加载类: ItemConfigTable
// 来源表: item_config
// 生成时间: 2026-03-03 09:48:08
// 警告: 请勿手动修改此文件！
// ==========================================

using System;
using Framework.Data;
using HotUpdate.Config.Data;

namespace HotUpdate.Config.Table
{
    /// <summary>
    /// ItemConfig 配置表加载器
    /// </summary>
    public class ItemConfigTable : ConfigBase<int, ItemConfig>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ItemConfigTable()
        {
            // 可以在这里指定数据库路径和表名
            // Load(dbPath, "item_config");
        }

        /// <summary>
        /// 获取配置项的主键
        /// </summary>
        protected override int GetKey(ItemConfig item)
        {
            return item.Id;
        }
    }
}
