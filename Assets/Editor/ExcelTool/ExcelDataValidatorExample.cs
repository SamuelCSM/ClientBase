using UnityEditor;
using UnityEngine;

namespace Editor.ExcelTool
{
    /// <summary>
    /// Excel 数据校验器使用示例
    /// </summary>
    public class ExcelDataValidatorExample
    {
        /// <summary>
        /// 示例1：校验单个 Excel 文件
        /// </summary>
        [MenuItem("Tools/Excel/Examples/校验 Excel 数据")]
        public static void Example1_ValidateExcel()
        {
            var excelPath = "Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx";

            if (!System.IO.File.Exists(excelPath))
            {
                Debug.LogWarning($"Excel 文件不存在: {excelPath}");
                return;
            }

            try
            {
                // 读取 Excel
                var reader = new ExcelReader();
                var sheets = reader.ReadExcel(excelPath);

                if (sheets.Count == 0)
                {
                    Debug.LogWarning("未找到工作表");
                    return;
                }

                // 校验数据
                var validator = new ExcelDataValidator();
                var result = validator.ValidateSheet(sheets[0]);

                // 输出结果
                if (result.IsValid)
                {
                    Debug.Log($"✓ 数据校验通过: {sheets[0].SheetName}");
                    
                    if (result.Warnings.Count > 0)
                    {
                        Debug.LogWarning($"警告 ({result.Warnings.Count}):\n{string.Join("\n", result.Warnings)}");
                    }
                }
                else
                {
                    Debug.LogError($"✗ 数据校验失败: {sheets[0].SheetName}");
                    Debug.LogError($"错误:\n{string.Join("\n", result.Errors)}");
                    
                    if (result.Warnings.Count > 0)
                    {
                        Debug.LogWarning($"警告:\n{string.Join("\n", result.Warnings)}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"校验过程中发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例2：校验并显示详细信息
        /// </summary>
        [MenuItem("Tools/Excel/Examples/校验并显示详细信息")]
        public static void Example2_ValidateWithDetails()
        {
            var excelPath = "Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx";

            if (!System.IO.File.Exists(excelPath))
            {
                Debug.LogWarning($"Excel 文件不存在: {excelPath}");
                return;
            }

            try
            {
                var reader = new ExcelReader();
                var sheets = reader.ReadExcel(excelPath);

                if (sheets.Count == 0)
                {
                    return;
                }

                var sheet = sheets[0];
                var validator = new ExcelDataValidator();
                var result = validator.ValidateSheet(sheet);

                // 显示表信息
                Debug.Log($"=== 表信息 ===");
                Debug.Log($"表名: {sheet.SheetName}");
                Debug.Log($"字段数: {sheet.FieldNames.Count}");
                Debug.Log($"数据行数: {sheet.DataRows.Count}");
                Debug.Log($"字段: {string.Join(", ", sheet.FieldNames)}");

                // 显示校验结果
                Debug.Log($"\n=== 校验结果 ===");
                Debug.Log($"是否通过: {(result.IsValid ? "是" : "否")}");
                Debug.Log($"错误数: {result.Errors.Count}");
                Debug.Log($"警告数: {result.Warnings.Count}");

                if (result.Errors.Count > 0)
                {
                    Debug.LogError($"\n错误列表:");
                    foreach (var error in result.Errors)
                    {
                        Debug.LogError($"  - {error}");
                    }
                }

                if (result.Warnings.Count > 0)
                {
                    Debug.LogWarning($"\n警告列表:");
                    foreach (var warning in result.Warnings)
                    {
                        Debug.LogWarning($"  - {warning}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"校验过程中发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例3：校验范围
        /// </summary>
        [MenuItem("Tools/Excel/Examples/校验数据范围")]
        public static void Example3_ValidateRanges()
        {
            var excelPath = "Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx";

            if (!System.IO.File.Exists(excelPath))
            {
                Debug.LogWarning($"Excel 文件不存在: {excelPath}");
                return;
            }

            try
            {
                var reader = new ExcelReader();
                var sheets = reader.ReadExcel(excelPath);

                if (sheets.Count == 0)
                {
                    return;
                }

                var validator = new ExcelDataValidator();
                var result = validator.ValidateSheet(sheets[0]);

                // 定义范围规则
                var ranges = new System.Collections.Generic.Dictionary<string, (double min, double max)>
                {
                    { "Quality", (1, 5) },      // 品质范围 1-5
                    { "Level", (1, 100) },      // 等级范围 1-100
                    { "Price", (0, 999999) }    // 价格范围 0-999999
                };

                // 校验范围
                validator.ValidateRanges(sheets[0], ranges, result);

                // 输出结果
                if (result.IsValid && result.Warnings.Count == 0)
                {
                    Debug.Log("✓ 数据范围校验通过");
                }
                else
                {
                    if (!result.IsValid)
                    {
                        Debug.LogError($"✗ 数据校验失败:\n{string.Join("\n", result.Errors)}");
                    }
                    
                    if (result.Warnings.Count > 0)
                    {
                        Debug.LogWarning($"范围警告:\n{string.Join("\n", result.Warnings)}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"校验过程中发生错误: {ex.Message}");
            }
        }
    }
}
