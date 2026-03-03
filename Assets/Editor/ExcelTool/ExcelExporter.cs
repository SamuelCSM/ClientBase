using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using SQLite;

namespace Editor.ExcelTool
{
    /// <summary>
    /// Excel 导出器
    /// 负责将 Excel 数据导出到 SQLite 数据库
    /// </summary>
    public class ExcelExporter
    {
        /// <summary>
        /// 导出配置
        /// </summary>
        public class ExportConfig
        {
            /// <summary>
            /// 输出数据库路径
            /// </summary>
            public string OutputDbPath { get; set; }

            /// <summary>
            /// 是否覆盖已存在的表
            /// </summary>
            public bool OverwriteExistingTables { get; set; } = true;

            /// <summary>
            /// 是否启用数据校验
            /// </summary>
            public bool EnableValidation { get; set; } = true;

            /// <summary>
            /// 是否显示详细日志
            /// </summary>
            public bool VerboseLogging { get; set; } = false;
        }

        /// <summary>
        /// 导出结果
        /// </summary>
        public class ExportResult
        {
            /// <summary>
            /// 是否成功
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            /// 导出的表名
            /// </summary>
            public string TableName { get; set; }

            /// <summary>
            /// 导出的行数
            /// </summary>
            public int RowCount { get; set; }

            /// <summary>
            /// 错误消息
            /// </summary>
            public string ErrorMessage { get; set; }

            /// <summary>
            /// 警告消息列表
            /// </summary>
            public List<string> Warnings { get; set; } = new List<string>();
        }

        private readonly ExportConfig _config;
        private readonly ExcelReader _reader;
        private readonly ExcelDataValidator _validator;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ExcelExporter(ExportConfig config = null)
        {
            _config = config ?? new ExportConfig();
            _reader = new ExcelReader();
            _validator = new ExcelDataValidator();
        }

        /// <summary>
        /// 导出单个 Excel 文件
        /// </summary>
        public ExportResult ExportExcel(string excelPath, string sheetName = null)
        {
            var result = new ExportResult();

            try
            {
                // 检查文件是否存在
                if (!File.Exists(excelPath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Excel 文件不存在: {excelPath}";
                    return result;
                }

                // 读取 Excel
                var sheets = _reader.ReadExcel(excelPath);
                if (sheets == null || sheets.Count == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "Excel 文件为空或无法读取";
                    return result;
                }

                // 选择要导出的表
                ExcelReader.ExcelSheetData targetSheet = null;
                if (string.IsNullOrEmpty(sheetName))
                {
                    targetSheet = sheets.FirstOrDefault();
                }
                else
                {
                    targetSheet = sheets.FirstOrDefault(s => s.SheetName == sheetName);
                }

                if (targetSheet == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"未找到工作表: {sheetName}";
                    return result;
                }

                result.TableName = targetSheet.SheetName;

                // 数据校验
                if (_config.EnableValidation)
                {
                    var validationResult = _validator.ValidateSheet(targetSheet);
                    
                    // 添加警告
                    result.Warnings.AddRange(validationResult.Warnings);
                    
                    // 如果有错误，返回失败
                    if (!validationResult.IsValid)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"数据校验失败:\n{string.Join("\n", validationResult.Errors)}";
                        
                        // 如果有警告，也添加到错误消息中
                        if (validationResult.Warnings.Count > 0)
                        {
                            result.ErrorMessage += $"\n\n警告:\n{string.Join("\n", validationResult.Warnings)}";
                        }
                        
                        return result;
                    }
                }

                // 导出到 SQLite
                ExportToSQLite(targetSheet, result);

                if (_config.VerboseLogging)
                {
                    Debug.Log($"[ExcelExporter] 成功导出: {result.TableName}, 行数: {result.RowCount}");
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"导出失败: {ex.Message}\n{ex.StackTrace}";
                Debug.LogError($"[ExcelExporter] {result.ErrorMessage}");
                return result;
            }
        }

        /// <summary>
        /// 批量导出 Excel 文件
        /// </summary>
        public List<ExportResult> ExportBatch(List<string> excelPaths, Action<int, int> progressCallback = null)
        {
            var results = new List<ExportResult>();

            for (int i = 0; i < excelPaths.Count; i++)
            {
                var excelPath = excelPaths[i];
                
                // 更新进度
                progressCallback?.Invoke(i + 1, excelPaths.Count);

                // 导出单个文件
                var result = ExportExcel(excelPath);
                results.Add(result);

                // 显示进度
                if (EditorUtility.DisplayCancelableProgressBar(
                    "批量导出 Excel",
                    $"正在导出: {Path.GetFileName(excelPath)} ({i + 1}/{excelPaths.Count})",
                    (float)(i + 1) / excelPaths.Count))
                {
                    Debug.LogWarning("[ExcelExporter] 用户取消了批量导出");
                    break;
                }
            }

            EditorUtility.ClearProgressBar();

            // 输出统计信息
            var successCount = results.Count(r => r.Success);
            var failCount = results.Count(r => !r.Success);
            Debug.Log($"[ExcelExporter] 批量导出完成: 成功 {successCount}, 失败 {failCount}");

            return results;
        }

        /// <summary>
        /// 导出到 SQLite
        /// </summary>
        private void ExportToSQLite(ExcelReader.ExcelSheetData sheetData, ExportResult result)
        {
            // 确保输出目录存在
            var directory = Path.GetDirectoryName(_config.OutputDbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 创建或打开数据库
            using (var connection = new SQLiteConnection(_config.OutputDbPath))
            {
                // 删除已存在的表（如果配置允许）
                if (_config.OverwriteExistingTables)
                {
                    var dropTableSql = $"DROP TABLE IF EXISTS {sheetData.SheetName}";
                    connection.Execute(dropTableSql);
                }

                // 创建表
                CreateTable(connection, sheetData);

                // 插入数据
                InsertData(connection, sheetData, result);
            }

            result.Success = true;
        }

        /// <summary>
        /// 创建表
        /// </summary>
        private void CreateTable(SQLiteConnection connection, ExcelReader.ExcelSheetData sheetData)
        {
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE IF NOT EXISTS {sheetData.SheetName} (");

            for (int i = 0; i < sheetData.FieldNames.Count; i++)
            {
                var fieldName = sheetData.FieldNames[i];
                var typeName = i < sheetData.TypeDefinitions.Count ? sheetData.TypeDefinitions[i] : "string";
                var sqlType = ConvertToSQLiteType(typeName);

                sb.Append($"{fieldName} {sqlType}");

                // 第一个字段作为主键
                if (i == 0)
                {
                    sb.Append(" PRIMARY KEY");
                }

                if (i < sheetData.FieldNames.Count - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(")");

            var createTableSql = sb.ToString();
            if (_config.VerboseLogging)
            {
                Debug.Log($"[ExcelExporter] 创建表 SQL: {createTableSql}");
            }

            connection.Execute(createTableSql);
        }

        /// <summary>
        /// 插入数据
        /// </summary>
        private void InsertData(SQLiteConnection connection, ExcelReader.ExcelSheetData sheetData, ExportResult result)
        {
            // 构建插入 SQL
            var fieldList = string.Join(", ", sheetData.FieldNames);
            var paramList = string.Join(", ", sheetData.FieldNames.Select(f => "?"));
            var insertSql = $"INSERT INTO {sheetData.SheetName} ({fieldList}) VALUES ({paramList})";

            if (_config.VerboseLogging)
            {
                Debug.Log($"[ExcelExporter] 插入数据 SQL: {insertSql}");
            }

            // 开始事务
            connection.BeginTransaction();
            try
            {
                foreach (var row in sheetData.DataRows)
                {
                    // 准备参数值
                    var values = new object[sheetData.FieldNames.Count];
                    for (int i = 0; i < sheetData.FieldNames.Count; i++)
                    {
                        var fieldName = sheetData.FieldNames[i];
                        var value = row.ContainsKey(fieldName) ? row[fieldName] : null;
                        values[i] = ConvertToString(value);
                    }

                    connection.Execute(insertSql, values);
                    result.RowCount++;
                }

                connection.Commit();
            }
            catch (Exception ex)
            {
                connection.Rollback();
                throw new Exception($"插入数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 转换为 SQLite 类型
        /// </summary>
        private string ConvertToSQLiteType(string typeName)
        {
            switch (typeName.ToLower())
            {
                case "int":
                case "long":
                case "short":
                case "byte":
                case "bool":
                    return "INTEGER";

                case "float":
                case "double":
                case "decimal":
                    return "REAL";

                default:
                    return "TEXT";
            }
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        private string ConvertToString(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            // 数组类型转换为逗号分隔的字符串
            if (value is Array array)
            {
                var items = new List<string>();
                foreach (var item in array)
                {
                    items.Add(item?.ToString() ?? string.Empty);
                }
                return string.Join(",", items);
            }

            return value.ToString();
        }
    }
}
