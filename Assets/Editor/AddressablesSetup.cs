using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace Framework.Editor
{
    /// <summary>
    /// Addressables 自动配置工具
    /// 用于初始化 Addressables 设置和创建资源分组
    /// </summary>
    public static class AddressablesSetup
    {
        [MenuItem("Framework/Setup Addressables")]
        public static void SetupAddressables()
        {
            // 获取或创建 Addressables 设置
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                settings = AddressableAssetSettings.Create(
                    AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                    AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                    true,
                    true
                );
                Debug.Log("创建 Addressables 设置成功");
            }

            // 创建资源分组
            CreateGroupIfNotExists(settings, "Framework", "框架核心资源");
            CreateGroupIfNotExists(settings, "Common", "通用资源");
            CreateGroupIfNotExists(settings, "UI", "UI资源");
            CreateGroupIfNotExists(settings, "Scene", "场景资源");

            // 保存设置
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            
            Debug.Log("Addressables 配置完成！");
            Debug.Log("已创建资源分组：Framework, Common, UI, Scene");
        }

        private static AddressableAssetGroup CreateGroupIfNotExists(
            AddressableAssetSettings settings, 
            string groupName, 
            string description)
        {
            // 检查分组是否已存在
            var existingGroup = settings.FindGroup(groupName);
            if (existingGroup != null)
            {
                Debug.Log($"资源分组 '{groupName}' 已存在，跳过创建");
                return existingGroup;
            }

            // 创建新分组
            var group = settings.CreateGroup(groupName, false, false, true, null);
            
            // 添加 BundledAssetGroupSchema
            var bundledSchema = group.AddSchema<BundledAssetGroupSchema>();
            bundledSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            bundledSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            bundledSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            
            // 添加 ContentUpdateGroupSchema
            group.AddSchema<ContentUpdateGroupSchema>();

            Debug.Log($"创建资源分组 '{groupName}' 成功 - {description}");
            return group;
        }

        [MenuItem("Framework/Validate Addressables Installation")]
        public static void ValidateAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("Addressables 尚未初始化，请先运行 'Framework/Setup Addressables'");
                return;
            }

            Debug.Log("=== Addressables 配置信息 ===");
            Debug.Log($"配置文件路径: {AssetDatabase.GetAssetPath(settings)}");
            Debug.Log($"资源分组数量: {settings.groups.Count}");
            
            foreach (var group in settings.groups)
            {
                if (group != null)
                {
                    Debug.Log($"  - {group.Name} (条目数: {group.entries.Count})");
                }
            }
            
            Debug.Log("Addressables 配置验证完成！");
        }
    }
}
