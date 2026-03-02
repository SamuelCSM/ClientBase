using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Framework.Data
{
    /// <summary>
    /// 配置管理器
    /// 负责管理所有配置表的加载、缓存和访问
    /// 支持按需加载和自动缓存
    /// </summary>
    public class ConfigManager
    {
        /// <summary>
        /// 配置表缓存字典（类型 -> 配置表实例）
        /// </summary>
        private readonly Dictionary<Type, IConfigTable> _configCache = new Dictionary<Type, IConfigTable>();

        /// <summary>
        /// 配置数据库路径
        /// </summary>
        private string _dbPath;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        /// <param name="dbPath">数据库文件路径，如果为null则使用默认路径</param>
        public void Initialize(string dbPath = null)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[ConfigManager] 配置管理器已经初始化，跳过重复初始化");
                return;
            }

            // 设置数据库路径
            if (string.IsNullOrEmpty(dbPath))
            {
                _dbPath = Path.Combine(Application.persistentDataPath, "config.db");
            }
            else
            {
                _dbPath = dbPath;
            }

            // 检查数据库文件是否存在
            if (!File.Exists(_dbPath))
            {
                Debug.LogWarning($"[ConfigManager] 配置数据库不存在: {_dbPath}");
            }
            else
            {
                Debug.Log($"[ConfigManager] 配置数据库路径: {_dbPath}");
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 获取配置表（按需加载，自动缓存）
        /// </summary>
        /// <typeparam name="TConfig">配置表类型</typeparam>
        /// <returns>配置表实例</returns>
        public TConfig GetConfig<TConfig>() where TConfig : IConfigTable, new()
        {
            EnsureInitialized();

            Type configType = typeof(TConfig);

            // 从缓存中获取
            if (_configCache.TryGetValue(configType, out IConfigTable cachedConfig))
            {
                return (TConfig)cachedConfig;
            }

            // 创建新的配置表实例
            TConfig config = new TConfig();

            // 获取表名
            string tableName = config.TableName;
            if (string.IsNullOrEmpty(tableName))
            {
                // 如果配置表没有指定表名，尝试从类型推断
                tableName = GetTableNameFromType(configType);
            }

            try
            {
                // 加载配置数据
                config.Load(_dbPath, tableName);

                // 加入缓存
                _configCache[configType] = config;

                Debug.Log($"[ConfigManager] 配置表 {configType.Name} 加载成功，共 {config.Count} 条数据");

                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigManager] 加载配置表 {configType.Name} 失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 预加载配置表
        /// </summary>
        /// <typeparam name="TConfig">配置表类型</typeparam>
        public void PreloadConfig<TConfig>() where TConfig : IConfigTable, new()
        {
            GetConfig<TConfig>();
        }

        /// <summary>
        /// 预加载多个配置表
        /// </summary>
        /// <param name="configTypes">配置表类型数组</param>
        public void PreloadConfigs(params Type[] configTypes)
        {
            EnsureInitialized();

            foreach (var configType in configTypes)
            {
                try
                {
                    // 检查是否实现了 IConfigTable 接口
                    if (!typeof(IConfigTable).IsAssignableFrom(configType))
                    {
                        Debug.LogWarning($"[ConfigManager] 类型 {configType.Name} 没有实现 IConfigTable 接口");
                        continue;
                    }

                    // 创建配置表实例
                    IConfigTable config = Activator.CreateInstance(configType) as IConfigTable;
                    if (config == null)
                    {
                        Debug.LogWarning($"[ConfigManager] 无法创建配置表实例: {configType.Name}");
                        continue;
                    }

                    // 获取表名
                    string tableName = config.TableName;
                    if (string.IsNullOrEmpty(tableName))
                    {
                        tableName = GetTableNameFromType(configType);
                    }

                    // 加载配置数据
                    config.Load(_dbPath, tableName);

                    // 加入缓存
                    _configCache[configType] = config;

                    Debug.Log($"[ConfigManager] 预加载配置表 {configType.Name} 成功，共 {config.Count} 条数据");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ConfigManager] 预加载配置表 {configType.Name} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 卸载配置表
        /// </summary>
        /// <typeparam name="TConfig">配置表类型</typeparam>
        public void UnloadConfig<TConfig>() where TConfig : IConfigTable
        {
            Type configType = typeof(TConfig);

            if (_configCache.TryGetValue(configType, out IConfigTable config))
            {
                // 调用Unload方法
                config.Unload();

                // 从缓存中移除
                _configCache.Remove(configType);

                Debug.Log($"[ConfigManager] 配置表 {configType.Name} 已卸载");
            }
        }

        /// <summary>
        /// 卸载所有配置表
        /// </summary>
        public void UnloadAllConfigs()
        {
            foreach (var kvp in _configCache)
            {
                try
                {
                    kvp.Value.Unload();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ConfigManager] 卸载配置表 {kvp.Key.Name} 失败: {ex.Message}");
                }
            }

            _configCache.Clear();
            Debug.Log("[ConfigManager] 所有配置表已卸载");
        }

        /// <summary>
        /// 重新加载配置表（用于热更新）
        /// </summary>
        /// <typeparam name="TConfig">配置表类型</typeparam>
        public void ReloadConfig<TConfig>() where TConfig : IConfigTable, new()
        {
            // 先卸载
            UnloadConfig<TConfig>();

            // 重新加载
            GetConfig<TConfig>();
        }

        /// <summary>
        /// 重新加载所有配置表
        /// </summary>
        public void ReloadAllConfigs()
        {
            var configTypes = new List<Type>(_configCache.Keys);
            
            UnloadAllConfigs();

            PreloadConfigs(configTypes.ToArray());

            Debug.Log("[ConfigManager] 所有配置表已重新加载");
        }

        /// <summary>
        /// 检查配置表是否已加载
        /// </summary>
        /// <typeparam name="TConfig">配置表类型</typeparam>
        /// <returns>如果已加载返回true，否则返回false</returns>
        public bool IsConfigLoaded<TConfig>() where TConfig : IConfigTable
        {
            return _configCache.ContainsKey(typeof(TConfig));
        }

        /// <summary>
        /// 获取已加载的配置表数量
        /// </summary>
        public int GetLoadedConfigCount()
        {
            return _configCache.Count;
        }

        /// <summary>
        /// 获取配置数据库路径
        /// </summary>
        public string GetDatabasePath()
        {
            return _dbPath;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            UnloadAllConfigs();
            _isInitialized = false;
            Debug.Log("[ConfigManager] 配置管理器已释放");
        }

        #region 私有方法

        /// <summary>
        /// 确保已初始化
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("ConfigManager未初始化，请先调用Initialize方法");
            }
        }

        /// <summary>
        /// 从类型推断表名
        /// </summary>
        private string GetTableNameFromType(Type configType)
        {
            // 尝试从配置表的泛型参数获取配置项类型
            var baseType = configType.BaseType;
            if (baseType != null && baseType.IsGenericType)
            {
                var valueType = baseType.GetGenericArguments()[1];
                return GetTableName(valueType);
            }

            // 使用配置表类型名（转换为下划线格式）
            return ConvertToSnakeCase(configType.Name);
        }

        /// <summary>
        /// 获取表名（从Table特性或类型名）
        /// </summary>
        private string GetTableName(Type type)
        {
            // 尝试从Table特性获取表名
            var tableAttr = Attribute.GetCustomAttribute(type, typeof(SQLite.TableAttribute)) as SQLite.TableAttribute;
            if (tableAttr != null && !string.IsNullOrEmpty(tableAttr.Name))
            {
                return tableAttr.Name;
            }

            // 使用类型名作为表名（转换为小写加下划线格式）
            return ConvertToSnakeCase(type.Name);
        }

        /// <summary>
        /// 将驼峰命名转换为下划线命名
        /// </summary>
        private string ConvertToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToLower(input[0]));

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLower(input[i]));
                }
                else
                {
                    result.Append(input[i]);
                }
            }

            return result.ToString();
        }

        #endregion
    }
}
