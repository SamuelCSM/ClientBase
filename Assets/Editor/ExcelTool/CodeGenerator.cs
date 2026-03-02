using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Editor.ExcelTool
{
    /// <summary>
    /// 代码生成器
    /// 根据 Excel 表结构生成 C# 配置类代码
    /// </summary>
    public class CodeGenerator
    {
        /// <summary>
        /// 代码生成配置
        /// </summary>
        public class GeneratorConfig
        {
            /// <summary>
            /// 命名空间
            /// </summary>
            public string Namespace { get; set; } = "HotUpdate.Config";

            /// <summary>
            /// 是否生成注释
            /// </summary>
            public bool GenerateComments { get; set; } = true;

            /// <summary>
            /// 是否使用 SQLite 特性
            /// </summary>
            public bool UseSQLiteAttributes { get; set; } = true;

            /// <summary>
            /// 是否生成 Serializable 特性
            /// </summary>
            public bool GenerateSerializable { get; set; } = true;

            /// <summary>
            /// 缩进字符（默认4个空格）
            /// </summary>
            public string Indent { get; set; } = "    ";
        }

        private GeneratorConfig _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        public CodeGenerator(GeneratorConfig config = null)
        {
            _config = config ?? new GeneratorConfig();
        }

        /// <summary>
        /// 生成配置类代码
        /// </summary>
        /// <param name="sheetData">Excel 表数据</param>
        /// <param name="className">类名（如果为空则使用表名）</param>
        /// <returns>生成的 C# 代码</returns>
        public string GenerateConfigClass(ExcelReader.ExcelSheetData sheetData, string className = null)
        {
            if (sheetData == null)
            {
                throw new ArgumentNullException(nameof(sheetData));
            }

            if (string.IsNullOrEmpty(className))
            {
                className = sheetData.SheetName;
            }

            // 确保类名符合 C# 命名规范
            className = SanitizeClassName(className);

            var sb = new StringBuilder();

            // 生成文件头注释
            GenerateFileHeader(sb, className, sheetData.SheetName);

            // 生成 using 语句
            GenerateUsings(sb);

            // 开始命名空间
            sb.AppendLine($"namespace {_config.Namespace}");
            sb.AppendLine("{");

            // 生成配置类
            GenerateClass(sb, sheetData, className);

            // 生成配置表加载类
            GenerateLoaderClass(sb, sheetData, className);

            // 结束命名空间
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 生成文件头注释
        /// </summary>
        private void GenerateFileHeader(StringBuilder sb, string className, string sheetName)
        {
            if (!_config.GenerateComments)
            {
                return;
            }

            sb.AppendLine("// ==========================================");
            sb.AppendLine($"// 自动生成的配置类: {className}");
            sb.AppendLine($"// 来源表: {sheetName}");
            sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// 警告: 请勿手动修改此文件！");
            sb.AppendLine("// ==========================================");
            sb.AppendLine();
        }

        /// <summary>
        /// 生成 using 语句
        /// </summary>
        private void GenerateUsings(StringBuilder sb)
        {
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            
            if (_config.UseSQLiteAttributes)
            {
                sb.AppendLine("using SQLite;");
            }

            if (_config.GenerateSerializable)
            {
                // Unity 的 Serializable 在 System 命名空间中
            }

            sb.AppendLine("using Framework.Data;");
            sb.AppendLine();
        }

        /// <summary>
        /// 生成配置类
        /// </summary>
        private void GenerateClass(StringBuilder sb, ExcelReader.ExcelSheetData sheetData, string className)
        {
            var indent = _config.Indent;

            // 类注释
            if (_config.GenerateComments)
            {
                sb.AppendLine($"{indent}/// <summary>");
                sb.AppendLine($"{indent}/// {className} 配置类");
                sb.AppendLine($"{indent}/// </summary>");
            }

            // 特性
            if (_config.UseSQLiteAttributes)
            {
                sb.AppendLine($"{indent}[Table(\"{sheetData.SheetName}\")]");
            }

            if (_config.GenerateSerializable)
            {
                sb.AppendLine($"{indent}[Serializable]");
            }

            // 类声明
            sb.AppendLine($"{indent}public class {className}");
            sb.AppendLine($"{indent}{{");

            // 生成属性
            for (int i = 0; i < sheetData.FieldNames.Count; i++)
            {
                var fieldName = sheetData.FieldNames[i];
                var comment = i < sheetData.Comments.Count ? sheetData.Comments[i] : "";

                GenerateProperty(sb, fieldName, comment, i == 0, indent + _config.Indent);
            }

            // 类结束
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }

        /// <summary>
        /// 生成属性
        /// </summary>
        private void GenerateProperty(StringBuilder sb, string fieldName, string comment, bool isPrimaryKey, string indent)
        {
            // 推断类型
            var propertyType = InferPropertyType(fieldName);
            var propertyName = SanitizePropertyName(fieldName);

            // 属性注释
            if (_config.GenerateComments && !string.IsNullOrEmpty(comment))
            {
                sb.AppendLine($"{indent}/// <summary>");
                sb.AppendLine($"{indent}/// {comment}");
                sb.AppendLine($"{indent}/// </summary>");
            }

            // 特性
            if (_config.UseSQLiteAttributes)
            {
                if (isPrimaryKey)
                {
                    sb.AppendLine($"{indent}[PrimaryKey]");
                }

                sb.AppendLine($"{indent}[Column(\"{fieldName}\")]");
            }

            // 属性声明
            sb.AppendLine($"{indent}public {propertyType} {propertyName} {{ get; set; }}");
            sb.AppendLine();
        }

        /// <summary>
        /// 生成配置表加载类
        /// </summary>
        private void GenerateLoaderClass(StringBuilder sb, ExcelReader.ExcelSheetData sheetData, string className)
        {
            var indent = _config.Indent;
            var loaderClassName = $"{className}Table";

            // 推断主键类型
            var primaryKeyType = sheetData.FieldNames.Count > 0 ? InferPropertyType(sheetData.FieldNames[0]) : "int";

            // 类注释
            if (_config.GenerateComments)
            {
                sb.AppendLine($"{indent}/// <summary>");
                sb.AppendLine($"{indent}/// {className} 配置表加载器");
                sb.AppendLine($"{indent}/// </summary>");
            }

            // 类声明
            sb.AppendLine($"{indent}public class {loaderClassName} : ConfigBase<{primaryKeyType}, {className}>");
            sb.AppendLine($"{indent}{{");

            // 构造函数
            if (_config.GenerateComments)
            {
                sb.AppendLine($"{indent}{_config.Indent}/// <summary>");
                sb.AppendLine($"{indent}{_config.Indent}/// 构造函数");
                sb.AppendLine($"{indent}{_config.Indent}/// </summary>");
            }

            sb.AppendLine($"{indent}{_config.Indent}public {loaderClassName}()");
            sb.AppendLine($"{indent}{_config.Indent}{{");
            sb.AppendLine($"{indent}{_config.Indent}{_config.Indent}// 可以在这里指定数据库路径和表名");
            sb.AppendLine($"{indent}{_config.Indent}{_config.Indent}// Load(dbPath, \"{sheetData.SheetName}\");");
            sb.AppendLine($"{indent}{_config.Indent}}}");
            sb.AppendLine();

            // GetKey 方法
            if (_config.GenerateComments)
            {
                sb.AppendLine($"{indent}{_config.Indent}/// <summary>");
                sb.AppendLine($"{indent}{_config.Indent}/// 获取配置项的主键");
                sb.AppendLine($"{indent}{_config.Indent}/// </summary>");
            }

            var firstPropertyName = sheetData.FieldNames.Count > 0 ? SanitizePropertyName(sheetData.FieldNames[0]) : "Id";
            sb.AppendLine($"{indent}{_config.Indent}protected override {primaryKeyType} GetKey({className} item)");
            sb.AppendLine($"{indent}{_config.Indent}{{");
            sb.AppendLine($"{indent}{_config.Indent}{_config.Indent}return item.{firstPropertyName};");
            sb.AppendLine($"{indent}{_config.Indent}}}");

            // 类结束
            sb.AppendLine($"{indent}}}");
        }

        /// <summary>
        /// 推断属性类型
        /// </summary>
        private string InferPropertyType(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return "string";
            }

            var lowerName = fieldName.ToLower();

            // 根据字段名推断类型
            if (lowerName.Contains("id") || lowerName.Contains("count") || lowerName.Contains("num") || 
                lowerName.Contains("level") || lowerName.Contains("type"))
            {
                return "int";
            }

            if (lowerName.Contains("rate") || lowerName.Contains("percent") || lowerName.Contains("ratio"))
            {
                return "float";
            }

            if (lowerName.Contains("is") || lowerName.Contains("enable") || lowerName.Contains("flag"))
            {
                return "bool";
            }

            if (lowerName.Contains("name") || lowerName.Contains("desc") || lowerName.Contains("text") || 
                lowerName.Contains("icon") || lowerName.Contains("path"))
            {
                return "string";
            }

            // 默认为 string
            return "string";
        }

        /// <summary>
        /// 清理类名，确保符合 C# 命名规范
        /// </summary>
        private string SanitizeClassName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Config";
            }

            // 移除非法字符
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
            }

            var result = sb.ToString();

            // 确保以字母或下划线开头
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            // 确保首字母大写
            if (result.Length > 0)
            {
                result = char.ToUpper(result[0]) + result.Substring(1);
            }

            return string.IsNullOrEmpty(result) ? "Config" : result;
        }

        /// <summary>
        /// 清理属性名，确保符合 C# 命名规范
        /// </summary>
        private string SanitizePropertyName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Value";
            }

            // 移除非法字符
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
            }

            var result = sb.ToString();

            // 确保以字母或下划线开头
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            // 确保首字母大写（属性名使用 PascalCase）
            if (result.Length > 0)
            {
                result = char.ToUpper(result[0]) + result.Substring(1);
            }

            return string.IsNullOrEmpty(result) ? "Value" : result;
        }

        /// <summary>
        /// 批量生成配置类代码
        /// </summary>
        /// <param name="sheets">多个 Excel 表数据</param>
        /// <returns>类名 -> 代码的字典</returns>
        public Dictionary<string, string> GenerateConfigClasses(List<ExcelReader.ExcelSheetData> sheets)
        {
            var result = new Dictionary<string, string>();

            foreach (var sheet in sheets)
            {
                try
                {
                    var className = SanitizeClassName(sheet.SheetName);
                    var code = GenerateConfigClass(sheet, className);
                    result[className] = code;

                    Debug.Log($"[CodeGenerator] 成功生成配置类: {className}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CodeGenerator] 生成配置类失败: {sheet.SheetName}, 错误: {ex.Message}");
                }
            }

            return result;
        }
    }
}
