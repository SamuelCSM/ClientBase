using System.Text;
using ProtoGenerator.Models;

namespace ProtoGenerator.Core
{
    public class CodeGenerator
    {
        private readonly DependencyResolver _dependencyResolver;

        public CodeGenerator()
        {
            _dependencyResolver = new DependencyResolver();
        }

        public string GenerateClass(MessageDefinition definition, GenerationContext context)
        {
            if (definition == null)
            {
                ProtoGeneratorLogger.LogError("消息定义为空");
                return null;
            }

            // 如果是枚举类型，使用枚举生成逻辑
            if (definition.Type == MessageType.Enum)
            {
                return GenerateEnumFromMessageDefinition(definition);
            }

            var sb = new StringBuilder();
            var usings = _dependencyResolver.GetRequiredUsings(definition);
            foreach (var usingStatement in usings)
            {
                sb.AppendLine($"using {usingStatement};");
            }
            sb.AppendLine();

            sb.AppendLine($"namespace {definition.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {definition.Comment ?? definition.Name}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [ProtoContract]");

            var baseClass = (definition.Type == MessageType.Send || definition.Type == MessageType.Receive) ? " : IMessage" : "";
            sb.AppendLine($"    public class {definition.FullName}{baseClass}");
            sb.AppendLine("    {");

            foreach (var field in definition.Fields)
            {
                GenerateField(sb, field, context);
            }

            if (definition.Type == MessageType.Send || definition.Type == MessageType.Receive)
            {
                GenerateMessageIdMethods(sb, definition);
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 从MessageDefinition生成枚举代码
        /// </summary>
        private string GenerateEnumFromMessageDefinition(MessageDefinition definition)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using ProtoBuf;");
            sb.AppendLine();
            sb.AppendLine($"namespace {definition.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {definition.Comment ?? definition.Name}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public enum {definition.FullName}");
            sb.AppendLine("    {");

            for (int i = 0; i < definition.Fields.Count; i++)
            {
                var field = definition.Fields[i];
                if (!string.IsNullOrEmpty(field.Comment))
                {
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// {field.Comment}");
                    sb.AppendLine("        /// </summary>");
                }

                var comma = i < definition.Fields.Count - 1 ? "," : "";
                sb.AppendLine($"        {field.Name} = {field.ProtoMemberIndex}{comma}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public string GenerateEnum(EnumDefinition definition, GenerationContext context)
        {
            if (definition == null)
            {
                ProtoGeneratorLogger.LogError("枚举定义为空");
                return null;
            }

            var sb = new StringBuilder();
            sb.AppendLine("using ProtoBuf;");
            sb.AppendLine();
            sb.AppendLine($"namespace {definition.Namespace}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {definition.Comment ?? definition.Name}");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public enum {definition.Name}");
            sb.AppendLine("    {");

            for (int i = 0; i < definition.Values.Count; i++)
            {
                var enumValue = definition.Values[i];
                if (!string.IsNullOrEmpty(enumValue.Comment))
                {
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine($"        /// {enumValue.Comment}");
                    sb.AppendLine("        /// </summary>");
                }

                var comma = i < definition.Values.Count - 1 ? "," : "";
                sb.AppendLine($"        {enumValue.Name} = {enumValue.Value}{comma}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private void GenerateField(StringBuilder sb, FieldDefinition field, GenerationContext context)
        {
            var csharpType = _dependencyResolver.ConvertProtoTypeToCSharp(field.Type);

            if (!string.IsNullOrEmpty(field.Comment))
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// {field.Comment}");
                sb.AppendLine("        /// </summary>");
            }

            sb.AppendLine($"        [ProtoMember({field.ProtoMemberIndex})]");
            sb.AppendLine($"        public {csharpType} {field.Name} {{ get; set; }}");
            sb.AppendLine();
        }

        private void GenerateMessageIdMethods(StringBuilder sb, MessageDefinition definition)
        {
            sb.AppendLine("        public byte GetMainId()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return {definition.MainId};");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public byte GetSubId()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return {definition.SubId};");
            sb.AppendLine("        }");
        }
    }
}
