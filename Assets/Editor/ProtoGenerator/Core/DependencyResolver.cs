using System.Collections.Generic;
using System.Linq;
using ProtoGenerator.Models;

namespace ProtoGenerator.Core
{
    /// <summary>
    /// 依赖解析器
    /// </summary>
    public class DependencyResolver
    {
        private static readonly HashSet<string> CSharpBuiltInTypes = new HashSet<string>
        {
            "bool", "byte", "sbyte", "char", "decimal", "double", "float",
            "int", "uint", "long", "ulong", "short", "ushort", "string", "object"
        };

        private static readonly Dictionary<string, string> ProtoToCSharpTypeMap = new Dictionary<string, string>
        {
            { "int32", "int" },
            { "int64", "long" },
            { "uint32", "uint" },
            { "uint64", "ulong" },
            { "bool", "bool" },
            { "string", "string" },
            { "double", "double" },
            { "float", "float" }
        };

        public List<string> ResolveDependencies(MessageDefinition definition)
        {
            var dependencies = new List<string>();
            if (definition == null || definition.Fields == null)
                return dependencies;

            foreach (var field in definition.Fields)
            {
                var fieldType = field.Type;
                if (ProtoToCSharpTypeMap.ContainsKey(fieldType))
                    fieldType = ProtoToCSharpTypeMap[fieldType];

                if (CSharpBuiltInTypes.Contains(fieldType))
                    continue;

                if (fieldType.Contains("<"))
                {
                    var genericType = ExtractGenericType(fieldType);
                    if (!string.IsNullOrEmpty(genericType) && !CSharpBuiltInTypes.Contains(genericType))
                    {
                        if (!dependencies.Contains(genericType))
                            dependencies.Add(genericType);
                    }
                    continue;
                }

                if (!dependencies.Contains(fieldType))
                    dependencies.Add(fieldType);
            }

            definition.Dependencies = dependencies;
            return dependencies;
        }

        public string[] GetRequiredUsings(MessageDefinition definition)
        {
            var usings = new HashSet<string>
            {
                "ProtoBuf",
                "Framework.Network"
            };

            if (definition.Dependencies != null && definition.Dependencies.Count > 0)
            {
                if (definition.Type == MessageType.Send || definition.Type == MessageType.Receive)
                {
                    usings.Add("Framework.Network.Messages.Common");
                    usings.Add("Framework.Network.Messages.Enum");
                }
            }

            if (definition.Fields.Any(f => f.Type.StartsWith("List<")))
            {
                usings.Add("System.Collections.Generic");
            }

            definition.RequiredUsings = usings.ToList();
            return usings.ToArray();
        }

        public void ValidateDependencies(MessageDefinition[] allDefinitions)
        {
            if (allDefinitions == null || allDefinitions.Length == 0)
                return;

            var typeMap = new Dictionary<string, MessageDefinition>();
            foreach (var def in allDefinitions)
            {
                if (!typeMap.ContainsKey(def.Name))
                    typeMap[def.Name] = def;
            }

            foreach (var definition in allDefinitions)
            {
                var dependencies = ResolveDependencies(definition);
                foreach (var dependency in dependencies)
                {
                    if (!typeMap.ContainsKey(dependency))
                    {
                        ProtoGeneratorLogger.LogWarning(
                            $"消息 {definition.Name} 依赖的类型 {dependency} 未找到定义");
                    }
                }
            }
        }

        private string MapTypeToNamespace(string typeName, MessageDefinition[] availableTypes)
        {
            if (availableTypes == null)
                return null;

            var matchingType = availableTypes.FirstOrDefault(t => t.Name == typeName);
            return matchingType?.Namespace;
        }

        private string ExtractGenericType(string genericType)
        {
            var startIndex = genericType.IndexOf('<');
            var endIndex = genericType.IndexOf('>');

            if (startIndex >= 0 && endIndex > startIndex)
                return genericType.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();

            return null;
        }

        public string ConvertProtoTypeToCSharp(string protoType)
        {
            if (ProtoToCSharpTypeMap.ContainsKey(protoType))
                return ProtoToCSharpTypeMap[protoType];

            return protoType;
        }
    }
}
