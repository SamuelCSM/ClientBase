using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ProtoGenerator.Models;

namespace ProtoGenerator.Core
{
    /// <summary>
    /// 定义解析器
    /// 负责解析.txt定义文件并提取消息结构信息
    /// </summary>
    public class DefinitionParser
    {
        // 正则表达式模式
        private static readonly Regex MessagePattern = new Regex(@"^message\s+(\w+)\s*\{", RegexOptions.Compiled);
        private static readonly Regex EnumPattern = new Regex(@"^enum\s+(\w+)\s*\{", RegexOptions.Compiled);
        private static readonly Regex FieldPattern = new Regex(@"^\s*(\w+)\s+(\w+)\s*=\s*(\d+)\s*;", RegexOptions.Compiled);
        private static readonly Regex EnumValuePattern = new Regex(@"^\s*(\w+)\s*=\s*(\d+)\s*;", RegexOptions.Compiled);
        private static readonly Regex CommentPattern = new Regex(@"//(.*)$", RegexOptions.Compiled);
        // 匹配消息名称格式: GC2GS_001_001_LoginRequest 或 GS2GC_001_002_LoginResponse
        private static readonly Regex MessageNamePattern = new Regex(@"^(GC2GS|GS2GC)_(\d{3})_(\d{3})_(\w+)$", RegexOptions.Compiled);

        /// <summary>
        /// 解析单个定义文件（可能包含多个消息定义）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>消息定义列表</returns>
        public List<MessageDefinition> ParseFileToList(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    ProtoGeneratorLogger.LogError($"文件不存在: {filePath}");
                    return new List<MessageDefinition>();
                }

                var content = File.ReadAllText(filePath);
                ValidateSyntax(content, filePath);

                var definitions = ParseContentMultiple(content, filePath);
                ProtoGeneratorLogger.Log($"成功解析文件: {filePath}, 找到 {definitions.Count} 个消息定义");

                return definitions;
            }
            catch (Exception ex)
            {
                ProtoGeneratorLogger.LogError($"解析文件失败 {filePath}: {ex.Message}");
                return new List<MessageDefinition>();
            }
        }

        /// <summary>
        /// 解析多个定义文件
        /// </summary>
        /// <param name="filePaths">文件路径数组</param>
        /// <returns>消息定义数组</returns>
        public MessageDefinition[] ParseMultipleFiles(string[] filePaths)
        {
            var definitions = new List<MessageDefinition>();

            foreach (var filePath in filePaths)
            {
                var fileDefinitions = ParseFileToList(filePath);
                definitions.AddRange(fileDefinitions);
            }

            ProtoGeneratorLogger.Log($"成功解析 {definitions.Count} 个消息定义，来自 {filePaths.Length} 个文件");
            return definitions.ToArray();
        }

        /// <summary>
        /// 解析文件内容（支持多个消息定义和枚举定义）
        /// </summary>
        /// <param name="content">文件内容</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>消息定义列表</returns>
        private List<MessageDefinition> ParseContentMultiple(string content, string filePath)
        {
            var definitions = new List<MessageDefinition>();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            MessageDefinition currentMessage = null;
            EnumDefinition currentEnum = null;
            bool inMessageBlock = false;
            bool inEnumBlock = false;
            string pendingComment = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // 提取注释
                string comment = null;
                var commentMatch = CommentPattern.Match(line);
                if (commentMatch.Success)
                {
                    comment = commentMatch.Groups[1].Value.Trim();
                    line = line.Substring(0, commentMatch.Index).Trim();
                }

                // 如果这一行只有注释，保存起来用于下一个字段
                if (string.IsNullOrWhiteSpace(line) && !string.IsNullOrEmpty(comment))
                {
                    pendingComment = comment;
                    continue;
                }

                // 解析enum定义
                var enumMatch = EnumPattern.Match(line);
                if (enumMatch.Success)
                {
                    var enumName = enumMatch.Groups[1].Value;
                    currentEnum = new EnumDefinition
                    {
                        Name = enumName,
                        Namespace = DetermineNamespace(filePath),
                        SourceFilePath = filePath,
                        Comment = pendingComment  // 保存类级别注释
                    };
                    inEnumBlock = true;
                    pendingComment = null;
                    ProtoGeneratorLogger.Log($"  解析枚举: {enumName}");
                    continue;
                }

                // 解析message定义
                var messageMatch = MessagePattern.Match(line);
                if (messageMatch.Success)
                {
                    var fullMessageName = messageMatch.Groups[1].Value;
                    var nameMatch = MessageNamePattern.Match(fullMessageName);

                    if (nameMatch.Success)
                    {
                        // 格式: GC2GS_001_001_LoginRequest
                        var prefix = nameMatch.Groups[1].Value;  // GC2GS 或 GS2GC
                        var mainId = byte.Parse(nameMatch.Groups[2].Value);  // 001
                        var subId = byte.Parse(nameMatch.Groups[3].Value);   // 001
                        var messageName = nameMatch.Groups[4].Value;  // LoginRequest

                        currentMessage = new MessageDefinition
                        {
                            Name = messageName,
                            FullName = fullMessageName,  // 保存完整名称用于文件名
                            MainId = mainId,
                            SubId = subId,
                            Type = prefix == "GC2GS" ? MessageType.Send : MessageType.Receive,
                            Namespace = DetermineNamespace(filePath),
                            SourceFilePath = filePath,
                            Comment = pendingComment  // 保存类级别注释
                        };

                        ProtoGeneratorLogger.Log($"  解析消息: {messageName} (MainId={mainId}, SubId={subId})");
                    }
                    else
                    {
                        // 不符合命名规范，使用默认处理
                        currentMessage = new MessageDefinition
                        {
                            Name = fullMessageName,
                            FullName = fullMessageName,  // 完整名称和类名相同
                            Type = DetermineMessageType(filePath),
                            Namespace = DetermineNamespace(filePath),
                            SourceFilePath = filePath,
                            Comment = pendingComment  // 保存类级别注释
                        };
                        ProtoGeneratorLogger.LogWarning($"  消息名称不符合规范: {fullMessageName}，应使用格式: GC2GS_001_001_MessageName");
                    }

                    inMessageBlock = true;
                    pendingComment = null;
                    continue;
                }

                // 解析枚举值
                if (inEnumBlock && currentEnum != null)
                {
                    if (line == "}")
                    {
                        inEnumBlock = false;
                        // 将枚举转换为MessageDefinition（用于统一处理）
                        var enumDef = ConvertEnumToMessageDefinition(currentEnum);
                        definitions.Add(enumDef);
                        currentEnum = null;
                        pendingComment = null;
                        continue;
                    }

                    var enumValueMatch = EnumValuePattern.Match(line);
                    if (enumValueMatch.Success)
                    {
                        var enumValue = new EnumValueDefinition
                        {
                            Name = enumValueMatch.Groups[1].Value,
                            Value = int.Parse(enumValueMatch.Groups[2].Value),
                            Comment = comment ?? pendingComment
                        };
                        currentEnum.Values.Add(enumValue);
                        pendingComment = null;
                    }
                }

                // 解析字段
                if (inMessageBlock && currentMessage != null)
                {
                    if (line == "}")
                    {
                        inMessageBlock = false;
                        definitions.Add(currentMessage);
                        currentMessage = null;
                        pendingComment = null;
                        continue;
                    }

                    var fieldMatch = FieldPattern.Match(line);
                    if (fieldMatch.Success)
                    {
                        var field = new FieldDefinition
                        {
                            Type = fieldMatch.Groups[1].Value,
                            Name = fieldMatch.Groups[2].Value,
                            ProtoMemberIndex = int.Parse(fieldMatch.Groups[3].Value),
                            Comment = comment ?? pendingComment
                        };
                        currentMessage.Fields.Add(field);
                        pendingComment = null;
                    }
                }
            }

            return definitions;
        }

        /// <summary>
        /// 将枚举定义转换为消息定义（用于统一处理）
        /// </summary>
        private MessageDefinition ConvertEnumToMessageDefinition(EnumDefinition enumDef)
        {
            return new MessageDefinition
            {
                Name = enumDef.Name,
                FullName = enumDef.Name,
                Type = MessageType.Enum,
                Namespace = enumDef.Namespace,
                SourceFilePath = enumDef.SourceFilePath,
                // 存储枚举值信息到字段中（临时方案）
                Fields = enumDef.Values.Select(v => new FieldDefinition
                {
                    Name = v.Name,
                    Type = "enum",
                    ProtoMemberIndex = v.Value,
                    Comment = v.Comment
                }).ToList()
            };
        }

        /// <summary>
        /// 验证语法
        /// </summary>
        /// <param name="content">文件内容</param>
        /// <param name="filePath">文件路径（用于错误报告）</param>
        private void ValidateSyntax(string content, string filePath)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException($"文件内容为空: {filePath}");
            }

            // 检查是否包含message或enum定义
            if (!content.Contains("message") && !content.Contains("enum"))
            {
                throw new InvalidOperationException($"文件中未找到message或enum定义: {filePath}");
            }

            // 检查大括号是否匹配
            int openBraces = 0;
            int closeBraces = 0;
            foreach (char c in content)
            {
                if (c == '{') openBraces++;
                if (c == '}') closeBraces++;
            }

            if (openBraces != closeBraces)
            {
                throw new InvalidOperationException($"大括号不匹配: {filePath} ({{ = {openBraces}, }} = {closeBraces})");
            }
        }



        /// <summary>
        /// 根据文件路径确定消息类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>消息类型</returns>
        private MessageType DetermineMessageType(string filePath)
        {
            filePath = filePath.Replace("\\", "/");

            if (filePath.Contains("/Enum/") || filePath.Contains("/enum/"))
                return MessageType.Enum;
            if (filePath.Contains("/Common/") || filePath.Contains("/common/"))
                return MessageType.Common;
            if (filePath.Contains("/GC2GS/") || filePath.Contains("/Send/"))
                return MessageType.Send;
            if (filePath.Contains("/GS2GC/") || filePath.Contains("/Recv/") || filePath.Contains("/Receive/"))
                return MessageType.Receive;

            // 默认为通用类型
            return MessageType.Common;
        }

        /// <summary>
        /// 根据文件路径确定命名空间
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>命名空间</returns>
        private string DetermineNamespace(string filePath)
        {
            // 基础命名空间
            string baseNamespace = "Framework.Network.Messages";

            filePath = filePath.Replace("\\", "/");

            // 根据路径确定子命名空间
            if (filePath.Contains("/Enum/"))
                return baseNamespace + ".Enum";
            if (filePath.Contains("/Common/"))
                return baseNamespace + ".Common";
            if (filePath.Contains("/GC2GS/") || filePath.Contains("/Send/"))
                return baseNamespace + ".GC2GS";
            if (filePath.Contains("/GS2GC/") || filePath.Contains("/Recv/"))
                return baseNamespace + ".GS2GC";

            return baseNamespace;
        }
    }
}
