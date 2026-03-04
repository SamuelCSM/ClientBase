using System;
using UnityEngine;

namespace Framework.HotUpdate
{
    /// <summary>
    /// 版本管理器
    /// 负责版本对比、更新类型判断和兼容性检查
    /// </summary>
    public class VersionManager
    {
        /// <summary>
        /// 比较两个版本号
        /// </summary>
        /// <param name="version1">版本1（如"1.0.0"）</param>
        /// <param name="version2">版本2（如"1.0.1"）</param>
        /// <returns>
        /// 返回值 > 0: version1 > version2
        /// 返回值 = 0: version1 = version2
        /// 返回值 < 0: version1 < version2
        /// </returns>
        public static int CompareVersion(string version1, string version2)
        {
            if (string.IsNullOrEmpty(version1) && string.IsNullOrEmpty(version2))
                return 0;
            
            if (string.IsNullOrEmpty(version1))
                return -1;
            
            if (string.IsNullOrEmpty(version2))
                return 1;
            
            try
            {
                string[] parts1 = version1.Split('.');
                string[] parts2 = version2.Split('.');
                
                int maxLength = Math.Max(parts1.Length, parts2.Length);
                
                for (int i = 0; i < maxLength; i++)
                {
                    int num1 = i < parts1.Length ? int.Parse(parts1[i]) : 0;
                    int num2 = i < parts2.Length ? int.Parse(parts2[i]) : 0;
                    
                    if (num1 != num2)
                    {
                        return num1.CompareTo(num2);
                    }
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"[VersionManager] 版本比较失败: {version1} vs {version2}, 错误: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// 判断更新类型
        /// </summary>
        /// <param name="currentVersion">当前版本信息</param>
        /// <param name="targetVersion">目标版本信息</param>
        /// <returns>更新类型</returns>
        public static UpdateType DetermineUpdateType(UpdateInfo currentVersion, UpdateInfo targetVersion)
        {
            if (currentVersion == null || targetVersion == null)
            {
                Logger.Warning("[VersionManager] 版本信息为空，无法判断更新类型");
                return UpdateType.None;
            }
            
            // 比较应用版本号
            int appVersionCompare = CompareVersion(currentVersion.AppVersion, targetVersion.AppVersion);
            
            // 如果应用版本不同，需要整包更新
            if (appVersionCompare != 0)
            {
                Logger.Log($"[VersionManager] 应用版本不同，需要整包更新: {currentVersion.AppVersion} -> {targetVersion.AppVersion}");
                return UpdateType.FullUpdate;
            }
            
            // 应用版本相同，检查资源版本和代码版本
            bool resourceChanged = currentVersion.ResourceVersion != targetVersion.ResourceVersion;
            bool codeChanged = currentVersion.CodeVersion != targetVersion.CodeVersion;
            
            if (resourceChanged || codeChanged)
            {
                Logger.Log($"[VersionManager] 资源或代码版本不同，需要热更新: " +
                          $"资源版本 {currentVersion.ResourceVersion} -> {targetVersion.ResourceVersion}, " +
                          $"代码版本 {currentVersion.CodeVersion} -> {targetVersion.CodeVersion}");
                return UpdateType.HotUpdate;
            }
            
            Logger.Log("[VersionManager] 版本相同，无需更新");
            return UpdateType.None;
        }
        
        /// <summary>
        /// 检查版本兼容性
        /// </summary>
        /// <param name="currentVersion">当前版本</param>
        /// <param name="minCompatibleVersion">最低兼容版本</param>
        /// <returns>是否兼容</returns>
        public static bool CheckCompatibility(string currentVersion, string minCompatibleVersion)
        {
            if (string.IsNullOrEmpty(minCompatibleVersion))
            {
                // 没有最低兼容版本限制，认为兼容
                return true;
            }
            
            int compareResult = CompareVersion(currentVersion, minCompatibleVersion);
            bool isCompatible = compareResult >= 0;
            
            if (!isCompatible)
            {
                Logger.Warning($"[VersionManager] 版本不兼容: 当前版本 {currentVersion} < 最低兼容版本 {minCompatibleVersion}");
            }
            else
            {
                Logger.Log($"[VersionManager] 版本兼容: 当前版本 {currentVersion} >= 最低兼容版本 {minCompatibleVersion}");
            }
            
            return isCompatible;
        }
        
        /// <summary>
        /// 获取当前本地版本信息
        /// </summary>
        /// <returns>本地版本信息</returns>
        public static UpdateInfo GetLocalVersion()
        {
            // 从本地文件读取版本信息
            string versionFilePath = System.IO.Path.Combine(Application.persistentDataPath, "version.json");
            
            if (System.IO.File.Exists(versionFilePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(versionFilePath);
                    UpdateInfo versionInfo = JsonUtility.FromJson<UpdateInfo>(json);
                    Logger.Log($"[VersionManager] 读取本地版本: {versionInfo.AppVersion}");
                    return versionInfo;
                }
                catch (Exception ex)
                {
                    Logger.Error($"[VersionManager] 读取本地版本文件失败: {ex.Message}");
                }
            }
            
            // 如果本地没有版本文件，使用应用内置版本
            UpdateInfo defaultVersion = new UpdateInfo
            {
                AppVersion = Application.version,
                ResourceVersion = 1,
                CodeVersion = 1,
                ForceUpdate = false,
                MinCompatibleVersion = Application.version,
                PatchFiles = new System.Collections.Generic.List<PatchFile>(),
                Description = "初始版本",
                Type = UpdateType.None
            };
            
            Logger.Log($"[VersionManager] 使用默认版本: {defaultVersion.AppVersion}");
            return defaultVersion;
        }
        
        /// <summary>
        /// 保存版本信息到本地
        /// </summary>
        /// <param name="versionInfo">版本信息</param>
        public static void SaveLocalVersion(UpdateInfo versionInfo)
        {
            try
            {
                string versionFilePath = System.IO.Path.Combine(Application.persistentDataPath, "version.json");
                string json = JsonUtility.ToJson(versionInfo, true);
                System.IO.File.WriteAllText(versionFilePath, json);
                Logger.Log($"[VersionManager] 保存本地版本: {versionInfo.AppVersion}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[VersionManager] 保存本地版本文件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 计算需要下载的补丁总大小
        /// </summary>
        /// <param name="patchFiles">补丁文件列表</param>
        /// <returns>总大小（字节）</returns>
        public static long CalculateTotalSize(System.Collections.Generic.List<PatchFile> patchFiles)
        {
            if (patchFiles == null || patchFiles.Count == 0)
                return 0;
            
            long totalSize = 0;
            foreach (var file in patchFiles)
            {
                totalSize += file.Size;
            }
            
            return totalSize;
        }
        
        /// <summary>
        /// 格式化文件大小
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化后的字符串（如"1.5 MB"）</returns>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
