using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor.ExcelTool
{
    /// <summary>
    /// Excel 导出器使用示例
    /// </summary>
    public class ExcelExporterExample
    {
        /// <summary>
        /// 示例1：导出单个 Excel 文件
        /// </summary>
        [MenuItem("Tools/Excel/Examples/导出单个文件")]
        public static void Example1_ExportSingleFile()
        {
            // 创建导出配置
            var config = new ExcelExporter.ExportConfig
            {
                OutputDbPath = "Assets/StreamingAssets/Config.db",
                OverwriteExistingTables = true,
                EnableValidation = true,
                VerboseLogging = true
            };

            // 创建导出器
            var exporter = new ExcelExporter(config);

            // 导出单个文件
            var result = exporter.ExportExcel("Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx");

            // 检查结果
            if (result.Success)
            {
                Debug.Log($"导出成功: {result.TableName}, 行数: {result.RowCount}");
            }
            else
            {
                Debug.LogError($"导出失败: {result.ErrorMessage}");
            }

            // 显示警告
            foreach (var warning in result.Warnings)
            {
                Debug.LogWarning(warning);
            }
        }

        /// <summary>
        /// 示例2：批量导出 Excel 文件
        /// </summary>
        [MenuItem("Tools/Excel/Examples/批量导出文件")]
        public static void Example2_ExportBatch()
        {
            // 创建导出配置
            var config = new ExcelExporter.ExportConfig
            {
                OutputDbPath = "Assets/StreamingAssets/Config.db",
                OverwriteExistingTables = true,
                EnableValidation = true,
                VerboseLogging = false
            };

            // 创建导出器
            var exporter = new ExcelExporter(config);

            // 准备要导出的文件列表
            var excelFiles = new List<string>
            {
                "Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx",
                "Assets/Editor/ExcelTool/TestData/SkillConfig.xlsx",
                "Assets/Editor/ExcelTool/TestData/MonsterConfig.xlsx"
            };

            // 批量导出，带进度回调
            var results = exporter.ExportBatch(excelFiles, (current, total) =>
            {
                Debug.Log($"导出进度: {current}/{total}");
            });

            // 统计结果
            var successCount = 0;
            var failCount = 0;

            foreach (var result in results)
            {
                if (result.Success)
                {
                    successCount++;
                    Debug.Log($"✓ {result.TableName}: {result.RowCount} 行");
                }
                else
                {
                    failCount++;
                    Debug.LogError($"✗ {result.TableName}: {result.ErrorMessage}");
                }
            }

            Debug.Log($"批量导出完成: 成功 {successCount}, 失败 {failCount}");
        }

        /// <summary>
        /// 示例3：导出时禁用数据校验
        /// </summary>
        [MenuItem("Tools/Excel/Examples/导出（禁用校验）")]
        public static void Example3_ExportWithoutValidation()
        {
            // 创建导出配置（禁用数据校验）
            var config = new ExcelExporter.ExportConfig
            {
                OutputDbPath = "Assets/StreamingAssets/Config.db",
                OverwriteExistingTables = true,
                EnableValidation = false,  // 禁用数据校验
                VerboseLogging = true
            };

            // 创建导出器
            var exporter = new ExcelExporter(config);

            // 导出文件
            var result = exporter.ExportExcel("Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx");

            if (result.Success)
            {
                Debug.Log($"导出成功（未校验）: {result.TableName}");
            }
            else
            {
                Debug.LogError($"导出失败: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// 示例4：导出时不覆盖已存在的表
        /// </summary>
        [MenuItem("Tools/Excel/Examples/导出（不覆盖）")]
        public static void Example4_ExportWithoutOverwrite()
        {
            // 创建导出配置（不覆盖已存在的表）
            var config = new ExcelExporter.ExportConfig
            {
                OutputDbPath = "Assets/StreamingAssets/Config.db",
                OverwriteExistingTables = false,  // 不覆盖已存在的表
                EnableValidation = true,
                VerboseLogging = true
            };

            // 创建导出器
            var exporter = new ExcelExporter(config);

            // 导出文件
            var result = exporter.ExportExcel("Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx");

            if (result.Success)
            {
                Debug.Log($"导出成功（未覆盖）: {result.TableName}");
            }
            else
            {
                Debug.LogError($"导出失败: {result.ErrorMessage}");
            }
        }
    }
}
