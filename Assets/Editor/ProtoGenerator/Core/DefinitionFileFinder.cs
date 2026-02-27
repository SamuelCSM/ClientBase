using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ProtoGenerator.Core
{
    /// <summary>
    /// 定义文件发现器
    /// 负责在项目中搜索和过滤.txt定义文件
    /// </summary>
    public class DefinitionFileFinder
    {
        private const string DefinitionFileExtension = ".txt";
        private const string DefaultDefinitionsPath = "Assets/ProtoDefinitions";

        /// <summary>
        /// 查找所有定义文件
        /// </summary>
        /// <returns>定义文件路径数组</returns>
        public string[] FindAllDefinitionFiles()
        {
            return FindDefinitionFiles(DefaultDefinitionsPath);
        }

        /// <summary>
        /// 在指定根路径下查找定义文件
        /// </summary>
        /// <param name="rootPath">根路径</param>
        /// <returns>定义文件路径数组</returns>
        public string[] FindDefinitionFiles(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                ProtoGeneratorLogger.LogWarning("根路径为空，使用默认路径");
                rootPath = DefaultDefinitionsPath;
            }

            // 检查路径是否存在
            if (!Directory.Exists(rootPath))
            {
                ProtoGeneratorLogger.LogWarning($"定义文件目录不存在: {rootPath}");
                return new string[0];
            }

            ProtoGeneratorLogger.Log($"开始搜索定义文件: {rootPath}");

            var definitionFiles = new List<string>();
            SearchDirectory(rootPath, definitionFiles);

            ProtoGeneratorLogger.Log($"找到 {definitionFiles.Count} 个定义文件");
            return definitionFiles.ToArray();
        }

        /// <summary>
        /// 递归搜索目录
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <param name="results">结果列表</param>
        private void SearchDirectory(string directoryPath, List<string> results)
        {
            try
            {
                // 搜索当前目录中的.txt文件
                var files = Directory.GetFiles(directoryPath, $"*{DefinitionFileExtension}");
                foreach (var file in files)
                {
                    if (IsValidDefinitionFile(file))
                    {
                        // 转换为Unity相对路径
                        var relativePath = file.Replace("\\", "/");
                        results.Add(relativePath);
                        ProtoGeneratorLogger.Log($"  发现定义文件: {relativePath}");
                    }
                }

                // 递归搜索子目录
                var directories = Directory.GetDirectories(directoryPath);
                foreach (var directory in directories)
                {
                    SearchDirectory(directory, results);
                }
            }
            catch (System.Exception ex)
            {
                ProtoGeneratorLogger.LogError($"搜索目录时出错 {directoryPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证是否为有效的定义文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否有效</returns>
        private bool IsValidDefinitionFile(string filePath)
        {
            // 检查文件扩展名
            if (!filePath.EndsWith(DefinitionFileExtension))
            {
                return false;
            }

            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                return false;
            }

            // 检查文件是否为空
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                ProtoGeneratorLogger.LogWarning($"跳过空文件: {filePath}");
                return false;
            }

            // 排除Unity的.meta文件
            if (filePath.EndsWith(".meta"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 根据消息类型过滤定义文件
        /// </summary>
        /// <param name="files">文件列表</param>
        /// <param name="pathPattern">路径模式（例如：GC2GS, GS2GC, Enum, Common）</param>
        /// <returns>过滤后的文件列表</returns>
        public string[] FilterByPathPattern(string[] files, string pathPattern)
        {
            if (string.IsNullOrEmpty(pathPattern))
            {
                return files;
            }

            return files.Where(f => f.Contains(pathPattern)).ToArray();
        }

        /// <summary>
        /// 获取定义文件的相对路径（相对于定义根目录）
        /// </summary>
        /// <param name="fullPath">完整路径</param>
        /// <param name="rootPath">根路径</param>
        /// <returns>相对路径</returns>
        public string GetRelativePath(string fullPath, string rootPath)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(rootPath))
            {
                return fullPath;
            }

            // 标准化路径分隔符
            fullPath = fullPath.Replace("\\", "/");
            rootPath = rootPath.Replace("\\", "/");

            // 确保根路径以/结尾
            if (!rootPath.EndsWith("/"))
            {
                rootPath += "/";
            }

            // 移除根路径部分
            if (fullPath.StartsWith(rootPath))
            {
                return fullPath.Substring(rootPath.Length);
            }

            return fullPath;
        }
    }
}
