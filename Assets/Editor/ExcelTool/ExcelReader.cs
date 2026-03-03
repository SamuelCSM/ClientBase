// 注意：此文件需要安装ExcelDataReader才能正常编译
// 运行 Tools > Excel > Install ExcelDataReader 查看安装指南

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using ExcelDataReader;
using UnityEngine;

namespace Editor.ExcelTool
{
    /// <summary>
    /// Excel读取器
    /// 负责读取Excel文件并解析为结构化数据
    /// </summary>
    public class ExcelReader
    {
        /// <summary>
        /// Excel格式定义
        /// </summary>
        public class ExcelFormat
        {
            /// <summary>
            /// 注释行索引（从0开始）
            /// </summary>
            public int CommentRowIndex { get; set; } = 0;

            /// <summary>
            /// 字段名行索引（从0开始）
            /// </summary>
            public int FieldNameRowIndex { get; set; } = 1;

            /// <summary>
            /// 类型定义行索引（从0开始）
            /// </summary>
            public int TypeRowIndex { get; set; } = 2;

            /// <summary>
            /// 数据起始行索引（从0开始）
            /// </summary>
            public int DataStartRowIndex { get; set; } = 3;
        }

        /// <summary>
        /// Excel表数据
        /// </summary>
        public class ExcelSheetData
        {
            /// <summary>
            /// 表名
            /// </summary>
            public string SheetName { get; set; }

            /// <summary>
            /// 字段名列表
            /// </summary>
            public List<string> FieldNames { get; set; }

            /// <summary>
            /// 类型定义列表（与字段名对应）
            /// </summary>
            public List<string> TypeDefinitions { get; set; }

            /// <summary>
            /// 注释列表（与字段名对应）
            /// </summary>
            public List<string> Comments { get; set; }

            /// <summary>
            /// 数据行列表（每行是一个字典：字段名 -> 值）
            /// </summary>
            public List<Dictionary<string, object>> DataRows { get; set; }

            public ExcelSheetData()
            {
                FieldNames = new List<string>();
                TypeDefinitions = new List<string>();
                Comments = new List<string>();
                DataRows = new List<Dictionary<string, object>>();
            }
        }

        private ExcelFormat _format;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="format">Excel格式定义，如果为null则使用默认格式</param>
        public ExcelReader(ExcelFormat format = null)
        {
            _format = format ?? new ExcelFormat();
        }

        /// <summary>
        /// 读取Excel文件
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>所有表的数据列表</returns>
        public List<ExcelSheetData> ReadExcel(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Excel文件不存在: {filePath}");
            }

            var result = new List<ExcelSheetData>();

            try
            {
                // 注册编码提供程序（ExcelDataReader需要）
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                        {
                            ConfigureDataTable = _ => new ExcelDataTableConfiguration
                            {
                                UseHeaderRow = false // 我们手动处理表头
                            }
                        });

                        // 遍历所有工作表
                        foreach (DataTable table in dataSet.Tables)
                        {
                            var sheetData = ReadSheet(table);
                            if (sheetData != null)
                            {
                                result.Add(sheetData);
                            }
                        }
                    }
                }

                Debug.Log($"[ExcelReader] 成功读取Excel文件: {filePath}, 共 {result.Count} 个工作表");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExcelReader] 读取Excel文件失败: {filePath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 读取单个工作表
        /// </summary>
        /// <param name="table">DataTable对象</param>
        /// <returns>工作表数据</returns>
        private ExcelSheetData ReadSheet(DataTable table)
        {
            if (table == null || table.Rows.Count == 0)
            {
                Debug.LogWarning($"[ExcelReader] 工作表为空或没有数据");
                return null;
            }

            // 检查行数是否足够
            //if (table.Rows.Count <= _format.DataStartRowIndex)
            //{
            //    Debug.LogWarning($"[ExcelReader] 工作表 {table.TableName} 数据行不足，跳过");
            //    return null;
            //}

            var sheetData = new ExcelSheetData
            {
                SheetName = table.TableName
            };

            try
            {
                // 读取注释行
                if (_format.CommentRowIndex < table.Rows.Count)
                {
                    var commentRow = table.Rows[_format.CommentRowIndex];
                    for (int col = 0; col < table.Columns.Count; col++)
                    {
                        var comment = commentRow[col]?.ToString() ?? string.Empty;
                        sheetData.Comments.Add(comment);
                    }
                }

                // 读取字段名行
                if (_format.FieldNameRowIndex < table.Rows.Count)
                {
                    var fieldNameRow = table.Rows[_format.FieldNameRowIndex];
                    for (int col = 0; col < table.Columns.Count; col++)
                    {
                        var fieldName = fieldNameRow[col]?.ToString() ?? string.Empty;
                        
                        // 跳过空字段名
                        if (string.IsNullOrWhiteSpace(fieldName))
                        {
                            continue;
                        }

                        sheetData.FieldNames.Add(fieldName.Trim());
                    }
                }

                // 读取类型定义行
                if (_format.TypeRowIndex < table.Rows.Count)
                {
                    var typeRow = table.Rows[_format.TypeRowIndex];
                    for (int col = 0; col < sheetData.FieldNames.Count && col < table.Columns.Count; col++)
                    {
                        var typeDef = typeRow[col]?.ToString() ?? "string";
                        sheetData.TypeDefinitions.Add(typeDef.Trim());
                    }
                }

                if (sheetData.FieldNames.Count == 0)
                {
                    Debug.LogWarning($"[ExcelReader] 工作表 {table.TableName} 没有有效的字段名，跳过");
                    return null;
                }

                // 读取数据行
                for (int row = _format.DataStartRowIndex; row < table.Rows.Count; row++)
                {
                    var dataRow = table.Rows[row];
                    var rowData = new Dictionary<string, object>();
                    bool hasData = false;

                    for (int col = 0; col < sheetData.FieldNames.Count && col < table.Columns.Count; col++)
                    {
                        var fieldName = sheetData.FieldNames[col];
                        var cellValue = dataRow[col];

                        // 检查是否有数据
                        if (cellValue != null && cellValue != DBNull.Value)
                        {
                            hasData = true;
                        }

                        rowData[fieldName] = cellValue;
                    }

                    // 只添加有数据的行
                    if (hasData)
                    {
                        sheetData.DataRows.Add(rowData);
                    }
                }

                Debug.Log($"[ExcelReader] 读取工作表 {table.TableName}, 字段数: {sheetData.FieldNames.Count}, 数据行数: {sheetData.DataRows.Count}");
                return sheetData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExcelReader] 读取工作表 {table.TableName} 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 解析单元格值为指定类型
        /// </summary>
        /// <param name="cellValue">单元格值</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>解析后的值</returns>
        public static object ParseCellValue(object cellValue, Type targetType)
        {
            if (cellValue == null || cellValue == DBNull.Value)
            {
                return GetDefaultValue(targetType);
            }

            try
            {
                // 获取实际类型（处理Nullable类型）
                var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                // 字符串类型
                if (actualType == typeof(string))
                {
                    return cellValue.ToString();
                }

                // 布尔类型
                if (actualType == typeof(bool))
                {
                    return ParseBool(cellValue);
                }

                // 整数类型
                if (actualType == typeof(int))
                {
                    return ParseInt(cellValue);
                }

                if (actualType == typeof(long))
                {
                    return ParseLong(cellValue);
                }

                if (actualType == typeof(short))
                {
                    return ParseShort(cellValue);
                }

                if (actualType == typeof(byte))
                {
                    return ParseByte(cellValue);
                }

                // 浮点类型
                if (actualType == typeof(float))
                {
                    return ParseFloat(cellValue);
                }

                if (actualType == typeof(double))
                {
                    return ParseDouble(cellValue);
                }

                if (actualType == typeof(decimal))
                {
                    return ParseDecimal(cellValue);
                }

                // 枚举类型
                if (actualType.IsEnum)
                {
                    return ParseEnum(cellValue, actualType);
                }

                // 数组类型（格式：[1,2,3] 或 1,2,3）
                if (actualType.IsArray)
                {
                    return ParseArray(cellValue, actualType);
                }

                // List 类型（格式：[1,2,3] 或 1,2,3）
                if (actualType.IsGenericType && actualType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return ParseList(cellValue, actualType);
                }

                // 自定义类型（JSON格式）
                if (actualType.IsClass && actualType != typeof(string))
                {
                    return ParseCustomType(cellValue, actualType);
                }

                // 默认使用Convert转换
                return Convert.ChangeType(cellValue, actualType);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ExcelReader] 解析单元格值失败: {cellValue} -> {targetType.Name}, 错误: {ex.Message}");
                return GetDefaultValue(targetType);
            }
        }

        #region 类型解析方法

        private static bool ParseBool(object value)
        {
            var str = value.ToString().ToLower().Trim();
            if (str == "true" || str == "1" || str == "yes" || str == "是")
            {
                return true;
            }
            if (str == "false" || str == "0" || str == "no" || str == "否")
            {
                return false;
            }
            return bool.Parse(str);
        }

        private static int ParseInt(object value)
        {
            if (value is double d)
            {
                return (int)d;
            }
            return Convert.ToInt32(value);
        }

        private static long ParseLong(object value)
        {
            if (value is double d)
            {
                return (long)d;
            }
            return Convert.ToInt64(value);
        }

        private static short ParseShort(object value)
        {
            if (value is double d)
            {
                return (short)d;
            }
            return Convert.ToInt16(value);
        }

        private static byte ParseByte(object value)
        {
            if (value is double d)
            {
                return (byte)d;
            }
            return Convert.ToByte(value);
        }

        private static float ParseFloat(object value)
        {
            if (value is double d)
            {
                return (float)d;
            }
            return Convert.ToSingle(value);
        }

        private static double ParseDouble(object value)
        {
            return Convert.ToDouble(value);
        }

        private static decimal ParseDecimal(object value)
        {
            return Convert.ToDecimal(value);
        }

        private static object ParseEnum(object value, Type enumType)
        {
            var str = value.ToString().Trim();
            
            // 尝试按名称解析
            if (Enum.IsDefined(enumType, str))
            {
                return Enum.Parse(enumType, str);
            }

            // 尝试按数值解析
            if (int.TryParse(str, out int intValue))
            {
                return Enum.ToObject(enumType, intValue);
            }

            throw new ArgumentException($"无法将 '{str}' 解析为枚举类型 {enumType.Name}");
        }

        private static object ParseArray(object value, Type arrayType)
        {
            var str = value.ToString().Trim();
            
            // 移除方括号
            if (str.StartsWith("[") && str.EndsWith("]"))
            {
                str = str.Substring(1, str.Length - 2);
            }

            if (string.IsNullOrWhiteSpace(str))
            {
                return Array.CreateInstance(arrayType.GetElementType(), 0);
            }

            // 分割字符串
            var parts = str.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var elementType = arrayType.GetElementType();
            var array = Array.CreateInstance(elementType, parts.Length);

            for (int i = 0; i < parts.Length; i++)
            {
                var element = ParseCellValue(parts[i].Trim(), elementType);
                array.SetValue(element, i);
            }

            return array;
        }

        private static object ParseList(object value, Type listType)
        {
            var str = value.ToString().Trim();
            
            // 移除方括号
            if (str.StartsWith("[") && str.EndsWith("]"))
            {
                str = str.Substring(1, str.Length - 2);
            }

            // 获取元素类型
            var elementType = listType.GetGenericArguments()[0];
            
            // 创建 List 实例
            var listInstance = Activator.CreateInstance(listType);
            var addMethod = listType.GetMethod("Add");

            if (string.IsNullOrWhiteSpace(str))
            {
                return listInstance;
            }

            // 分割字符串
            var parts = str.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                var element = ParseCellValue(parts[i].Trim(), elementType);
                addMethod.Invoke(listInstance, new[] { element });
            }

            return listInstance;
        }

        private static object ParseCustomType(object value, Type targetType)
        {
            var str = value.ToString().Trim();
            
            // 1. 检查是否有 CustomTypeParser 特性
            var parserAttr = System.Attribute.GetCustomAttribute(targetType, typeof(Editor.ExcelTool.CustomTypeParserAttribute)) as Editor.ExcelTool.CustomTypeParserAttribute;
            if (parserAttr != null)
            {
                try
                {
                    var parser = Activator.CreateInstance(parserAttr.ParserType) as Editor.ExcelTool.ICustomTypeParser;
                    if (parser != null && parser.CanParse(targetType))
                    {
                        return parser.Parse(str, targetType);
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"使用自定义解析器 {parserAttr.ParserType.Name} 解析失败: {ex.Message}");
                }
            }

            // 2. 检查是否有静态 Parse(string) 方法
            var parseMethod = targetType.GetMethod("Parse",
                BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(string) }, null);

            if (parseMethod != null && parseMethod.ReturnType == targetType)
            {
                try
                {
                    return parseMethod.Invoke(null, new object[] { str });
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"调用 {targetType.Name}.Parse(string) 方法失败: {ex.Message}");
                }
            }

            // 3. 尝试使用 JSON 反序列化（保留原有功能）
            try
            {
                return JsonUtility.FromJson(str, targetType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"无法将 '{str}' 解析为自定义类型 {targetType.Name}: {ex.Message}");
            }
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        #endregion
    }
}
