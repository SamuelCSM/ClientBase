using System.IO;
using System.Text.RegularExpressions;
using ProtoGenerator.Models;

namespace ProtoGenerator.Core
{
    /// <summary>
    /// 路径解析器
    /// 负责将定义文件路径映射到输出路径
    /// </summary>
    public class PathResolver
    {
        private const string DefaultOutputBasePath = "Assets/Scripts/Framework/Network/Messages";
        private const string DefaultDefinitionsPath = "Assets/ProtoDefinitions";

        public string ResolveOutputPath(string definitionPath, MessageType messageType)
        {
            definitionPath = definitionPath.Replace("\\", "/");
            
            var relativePath = GetRelativePath(DefaultDefinitionsPath, definitionPath);
            var directoryPath = Path.GetDirectoryName(relativePath).Replace("\\", "/");
            var fileName = Path.GetFileNameWithoutExtension(definitionPath);
            
            var outputDirectory = Path.Combine(DefaultOutputBasePath, directoryPath).Replace("\\", "/");
            outputDirectory = SanitizeDirectoryName(outputDirectory);
            
            var outputPath = Path.Combine(outputDirectory, fileName + ".cs").Replace("\\", "/");
            
            return outputPath;
        }

        /// <summary>
        /// 根据消息定义解析输出路径（使用完整消息名称作为文件名）
        /// </summary>
        /// <param name="definition">消息定义</param>
        /// <returns>输出文件路径</returns>
        public string ResolveOutputPathForMessage(MessageDefinition definition)
        {
            var definitionPath = definition.SourceFilePath.Replace("\\", "/");
            
            var relativePath = GetRelativePath(DefaultDefinitionsPath, definitionPath);
            var directoryPath = Path.GetDirectoryName(relativePath).Replace("\\", "/");
            
            var outputDirectory = Path.Combine(DefaultOutputBasePath, directoryPath).Replace("\\", "/");
            outputDirectory = SanitizeDirectoryName(outputDirectory);
            
            // 使用完整消息名称作为文件名（包含前缀和协议号）
            var fileName = string.IsNullOrEmpty(definition.FullName) ? definition.Name : definition.FullName;
            var outputPath = Path.Combine(outputDirectory, fileName + ".cs").Replace("\\", "/");
            
            return outputPath;
        }

        public string GetRelativePath(string basePath, string fullPath)
        {
            basePath = basePath.Replace("\\", "/");
            fullPath = fullPath.Replace("\\", "/");
            
            if (!basePath.EndsWith("/"))
                basePath += "/";
            
            if (fullPath.StartsWith(basePath))
                return fullPath.Substring(basePath.Length);
            
            return fullPath;
        }

        private string SanitizeDirectoryName(string directoryName)
        {
            var invalidChars = Path.GetInvalidPathChars();
            foreach (var c in invalidChars)
            {
                directoryName = directoryName.Replace(c, '_');
            }
            
            directoryName = Regex.Replace(directoryName, @"[<>:""|?*]", "_");
            
            return directoryName;
        }

        private string MapMessageTypeToDirectory(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Enum:
                    return "Enum";
                case MessageType.Common:
                    return "Common";
                case MessageType.Send:
                    return "GC2GS";
                case MessageType.Receive:
                    return "GS2GC";
                default:
                    return "Common";
            }
        }

        public string GetOutputDirectory(string definitionPath)
        {
            var outputPath = ResolveOutputPath(definitionPath, MessageType.Common);
            return Path.GetDirectoryName(outputPath).Replace("\\", "/");
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                ProtoGeneratorLogger.Log($"创建目录: {directoryPath}");
            }
        }
    }
}
