using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor.ExcelTool
{
    /// <summary>
    /// 代码生成器使用示例
    /// </summary>
    public class CodeGeneratorExample
    {
        /// <summary>
        /// 示例：从 Excel 生成配置类代码
        /// </summary>
        [MenuItem("Tools/Excel/Generate Config Code Example")]
        public static void GenerateConfigCodeExample()
        {
            // 1. 读取 Excel 文件
            var excelPath = "Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx"; // 示例路径
            
            if (!File.Exists(excelPath))
            {
                Debug.LogWarning($"[CodeGeneratorExample] Excel 文件不存在: {excelPath}");
                return;
            }

            try
            {
                var excelReader = new ExcelReader();
                var sheets = excelReader.ReadExcel(excelPath);

                if (sheets.Count == 0)
                {
                    Debug.LogWarning("[CodeGeneratorExample] 没有读取到任何工作表");
                    return;
                }

                // 2. 创建代码生成器
                var config = new CodeGenerator.GeneratorConfig
                {
                    Namespace = "HotUpdate.Config",
                    GenerateComments = true,
                    UseSQLiteAttributes = true,
                    GenerateSerializable = true
                };

                var generator = new CodeGenerator(config);

                // 3. 生成配置类代码
                foreach (var sheet in sheets)
                {
                    Debug.Log($"[CodeGeneratorExample] 开始生成配置类: {sheet.SheetName}");

                    var code = generator.GenerateConfigClass(sheet);

                    // 4. 输出到控制台（实际使用时应该保存到文件）
                    Debug.Log($"[CodeGeneratorExample] 生成的代码:\n{code}");

                    // 5. 保存到文件（可选）
                    var outputPath = $"Assets/Scripts/HotUpdate/Config/{sheet.SheetName}.cs";
                    SaveCodeToFile(code, outputPath);
                }

                Debug.Log("[CodeGeneratorExample] 代码生成完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CodeGeneratorExample] 代码生成失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例：批量生成配置类代码
        /// </summary>
        public static void GenerateBatchConfigCode()
        {
            var excelFiles = new[]
            {
                "Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx",
                "Assets/Editor/ExcelTool/TestData/SkillConfig.xlsx",
                // 添加更多文件...
            };

            var config = new CodeGenerator.GeneratorConfig
            {
                Namespace = "HotUpdate.Config",
                GenerateComments = true,
                UseSQLiteAttributes = true,
                GenerateSerializable = true
            };

            var generator = new CodeGenerator(config);
            var excelReader = new ExcelReader();

            foreach (var excelPath in excelFiles)
            {
                if (!File.Exists(excelPath))
                {
                    Debug.LogWarning($"[CodeGeneratorExample] Excel 文件不存在: {excelPath}");
                    continue;
                }

                try
                {
                    var sheets = excelReader.ReadExcel(excelPath);
                    var codeDict = generator.GenerateConfigClasses(sheets);

                    foreach (var kvp in codeDict)
                    {
                        var className = kvp.Key;
                        var code = kvp.Value;

                        var outputPath = $"Assets/Scripts/HotUpdate/Config/{className}.cs";
                        SaveCodeToFile(code, outputPath);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[CodeGeneratorExample] 处理文件失败: {excelPath}, 错误: {ex.Message}");
                }
            }

            Debug.Log("[CodeGeneratorExample] 批量代码生成完成");
        }

        /// <summary>
        /// 保存代码到文件
        /// </summary>
        private static void SaveCodeToFile(string code, string filePath)
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 写入文件
                File.WriteAllText(filePath, code, System.Text.Encoding.UTF8);

                Debug.Log($"[CodeGeneratorExample] 代码已保存到: {filePath}");

                // 刷新 Unity 资源
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CodeGeneratorExample] 保存文件失败: {filePath}, 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 示例：自定义代码生成配置
        /// </summary>
        public static void GenerateWithCustomConfig()
        {
            var config = new CodeGenerator.GeneratorConfig
            {
                Namespace = "MyGame.Data",           // 自定义命名空间
                GenerateComments = true,              // 生成注释
                UseSQLiteAttributes = true,           // 使用 SQLite 特性
                GenerateSerializable = true,          // 生成 Serializable 特性
                Indent = "    "                       // 使用 4 个空格缩进
            };

            var generator = new CodeGenerator(config);

            // 使用自定义配置生成代码...
        }
    }
}
