using System.Collections.Generic;
using SQLite;
using UnityEngine;

namespace Framework.Data
{
    /// <summary>
    /// ConfigManager 使用示例
    /// 演示如何使用 ConfigManager 管理配置表
    /// </summary>
    public class ConfigManagerExample : MonoBehaviour
    {
        private ConfigManager _configManager;

        void Start()
        {
            // 初始化配置管理器
            InitializeConfigManager();

            // 演示按需加载
            DemoOnDemandLoading();

            // 演示预加载
            DemoPreloading();

            // 演示配置查询
            DemoConfigQuery();

            // 演示热更新
            DemoHotReload();
        }

        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        void InitializeConfigManager()
        {
            Debug.Log("=== 初始化配置管理器 ===");

            _configManager = new ConfigManager();
            _configManager.Initialize();

            Debug.Log($"配置数据库路径: {_configManager.GetDatabasePath()}");
        }

        /// <summary>
        /// 演示按需加载
        /// </summary>
        void DemoOnDemandLoading()
        {
            Debug.Log("\n=== 演示按需加载 ===");

            // 第一次访问：从数据库加载（简化的API，只需要一个泛型参数）
            Debug.Log("第一次访问配置表...");
            var itemConfig1 = _configManager.GetConfig<ItemConfigTable>();
            Debug.Log($"配置表已加载，数据量: {itemConfig1.Count}");

            // 第二次访问：从缓存获取
            Debug.Log("第二次访问配置表...");
            var itemConfig2 = _configManager.GetConfig<ItemConfigTable>();
            Debug.Log($"从缓存获取，是否同一实例: {ReferenceEquals(itemConfig1, itemConfig2)}");

            Debug.Log($"当前已加载配置表数量: {_configManager.GetLoadedConfigCount()}");
        }

        /// <summary>
        /// 演示预加载
        /// </summary>
        void DemoPreloading()
        {
            Debug.Log("\n=== 演示预加载 ===");

            // 批量预加载配置表
            _configManager.PreloadConfigs(
                typeof(ItemConfigTable),
                typeof(SkillConfigTable)
            );

            Debug.Log($"预加载完成，已加载配置表数量: {_configManager.GetLoadedConfigCount()}");

            // 检查配置表是否已加载
            bool itemLoaded = _configManager.IsConfigLoaded<ItemConfigTable>();
            bool skillLoaded = _configManager.IsConfigLoaded<SkillConfigTable>();

            Debug.Log($"ItemConfigTable 已加载: {itemLoaded}");
            Debug.Log($"SkillConfigTable 已加载: {skillLoaded}");
        }

        /// <summary>
        /// 演示配置查询
        /// </summary>
        void DemoConfigQuery()
        {
            Debug.Log("\n=== 演示配置查询 ===");

            var itemConfig = _configManager.GetConfig<ItemConfigTable>();

            // 根据主键查询
            var item = itemConfig.GetByKey(1001);
            if (item != null)
            {
                Debug.Log($"查询物品 1001: {item.Name}, 品质: {item.Quality}");
            }

            // 获取所有配置
            var allItems = itemConfig.GetAll();
            Debug.Log($"所有物品数量: {allItems.Count}");

            // 条件查询
            var highQualityItems = itemConfig.GetList(item => item.Quality >= 3);
            Debug.Log($"高品质物品数量: {highQualityItems.Count}");

            // 获取第一条数据
            var firstItem = itemConfig.GetFirst();
            if (firstItem != null)
            {
                Debug.Log($"第一个物品: {firstItem.Name} (ID: {firstItem.Id})");
            }

            // 获取第一个符合条件的配置
            var firstWeapon = itemConfig.GetFirst(item => item.Type == 1);
            if (firstWeapon != null)
            {
                Debug.Log($"第一个武器: {firstWeapon.Name} (ID: {firstWeapon.Id})");
            }

            // 获取最后一条数据
            var lastItem = itemConfig.GetLast();
            if (lastItem != null)
            {
                Debug.Log($"最后一个物品: {lastItem.Name} (ID: {lastItem.Id})");
            }

            // 获取最后一个符合条件的配置
            var lastWeapon = itemConfig.GetLast(item => item.Type == 1);
            if (lastWeapon != null)
            {
                Debug.Log($"最后一个武器: {lastWeapon.Name} (ID: {lastWeapon.Id})");
            }

            // 自定义查询方法
            var weapons = itemConfig.GetByType(1);
            Debug.Log($"武器数量: {weapons.Count}");
        }

        /// <summary>
        /// 演示热更新
        /// </summary>
        void DemoHotReload()
        {
            Debug.Log("\n=== 演示热更新 ===");

            // 重新加载单个配置表
            Debug.Log("重新加载 ItemConfigTable...");
            _configManager.ReloadConfig<ItemConfigTable>();

            var itemConfig = _configManager.GetConfig<ItemConfigTable>();
            Debug.Log($"重新加载后，数据量: {itemConfig.Count}");
        }

        void OnDestroy()
        {
            // 清理资源
            _configManager?.Dispose();
            Debug.Log("配置管理器已释放");
        }
    }

    #region 示例配置表定义

    /// <summary>
    /// 物品配置数据
    /// </summary>
    [Table("item_config")]
    public class ItemConfigData
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Name { get; set; }

        public int Type { get; set; }

        public int Quality { get; set; }

        public string Icon { get; set; }

        public string Description { get; set; }
    }

    /// <summary>
    /// 物品配置表
    /// </summary>
    public class ItemConfigTable : ConfigBase<int, ItemConfigData>
    {
        protected override int GetKey(ItemConfigData item)
        {
            return item.Id;
        }

        /// <summary>
        /// 根据类型获取物品列表
        /// </summary>
        public List<ItemConfigData> GetByType(int type)
        {
            return GetList(item => item.Type == type);
        }

        /// <summary>
        /// 根据品质获取物品列表
        /// </summary>
        public List<ItemConfigData> GetByQuality(int quality)
        {
            return GetList(item => item.Quality == quality);
        }
    }

    /// <summary>
    /// 技能配置数据
    /// </summary>
    [Table("skill_config")]
    public class SkillConfigData
    {
        [PrimaryKey]
        public int Id { get; set; }

        public string Name { get; set; }

        public int Type { get; set; }

        public int Level { get; set; }

        public int ManaCost { get; set; }

        public int Damage { get; set; }

        public string Description { get; set; }
    }

    /// <summary>
    /// 技能配置表
    /// </summary>
    public class SkillConfigTable : ConfigBase<int, SkillConfigData>
    {
        protected override int GetKey(SkillConfigData item)
        {
            return item.Id;
        }

        /// <summary>
        /// 根据类型获取技能列表
        /// </summary>
        public List<SkillConfigData> GetByType(int type)
        {
            return GetList(skill => skill.Type == type);
        }

        /// <summary>
        /// 根据等级获取技能列表
        /// </summary>
        public List<SkillConfigData> GetByLevel(int level)
        {
            return GetList(skill => skill.Level == level);
        }
    }

    #endregion
}
