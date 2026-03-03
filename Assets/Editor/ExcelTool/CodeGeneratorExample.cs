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
        /// 示例1：生成单个配置类
        /// </summary>
        [MenuItem("Tools/Excel/Examples/生成单个配置类")]
        public static void Example1_GenerateSingleClass()
        {
            // 创建读取器
            var reader = new ExcelReader();

            // 读取 Excel 文件
            var sheets = reader.ReadExcel("Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx");

            if (sheets.Count == 0)
            {
                Debug.LogError("未找到工作表");
                return;
            }

            // 创建代码生成器
            var generator = new CodeGenerator();

            // 生成代码
            var result = generator.GenerateConfigClass(sheets[0], "ItemConfig");

            // 保存数据类文件
            var dataDirectory = Path.GetDirectoryName(result.DataClassPath);
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }
            File.WriteAllText(result.DataClassPath, result.DataClassCode, System.Text.Encoding.UTF8);

            // 保存 Table 类文件
            var tableDirectory = Path.GetDirectoryName(result.TableClassPath);
            if (!Directory.Exists(tableDirectory))
            {
                Directory.CreateDirectory(tableDirectory);
            }
            File.WriteAllText(result.TableClassPath, result.TableClassCode, System.Text.Encoding.UTF8);

            Debug.Log($"代码已生成:\n数据类: {result.DataClassPath}\nTable类: {result.TableClassPath}");
            
            // 刷新资源
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 示例2：批量生成配置类
        /// </summary>
        [MenuItem("Tools/Excel/Examples/批量生成配置类")]
        public static void Example2_GenerateBatchClasses()
        {
            var excelFiles = new[]
            {
                "Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx",
                "Assets/Editor/ExcelTool/TestData/SkillConfig.xlsx",
            };

            var generator = new CodeGenerator();
            var reader = new ExcelReader();

            foreach (var excelPath in excelFiles)
            {
                if (!File.Exists(excelPath))
                {
                    Debug.LogWarning($"Excel 文件不存在: {excelPath}");
                    continue;
                }

                try
                {
                    var sheets = reader.ReadExcel(excelPath);
                    var results = generator.GenerateConfigClasses(sheets);

                    foreach (var kvp in results)
                    {
                        var className = kvp.Key;
                        var result = kvp.Value;

                        // 保存数据类
                        var dataDirectory = Path.GetDirectoryName(result.DataClassPath);
                        if (!Directory.Exists(dataDirectory))
                        {
                            Directory.CreateDirectory(dataDirectory);
                        }
                        File.WriteAllText(result.DataClassPath, result.DataClassCode, System.Text.Encoding.UTF8);

                        // 保存 Table 类
                        var tableDirectory = Path.GetDirectoryName(result.TableClassPath);
                        if (!Directory.Exists(tableDirectory))
                        {
                            Directory.CreateDirectory(tableDirectory);
                        }
                        File.WriteAllText(result.TableClassPath, result.TableClassCode, System.Text.Encoding.UTF8);

                        Debug.Log($"已生成: {className}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"处理文件失败: {excelPath}, 错误: {ex.Message}");
                }
            }

            Debug.Log("批量代码生成完成");
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 示例3：自定义输出路径
        /// </summary>
        [MenuItem("Tools/Excel/Examples/自定义输出路径")]
        public static void Example3_CustomOutputPath()
        {
            var config = new CodeGenerator.GeneratorConfig
            {
                Namespace = "MyGame.Config",
                DataOutputPath = "Assets/Scripts/MyGame/ConfigData/Data",
                TableOutputPath = "Assets/Scripts/MyGame/ConfigData/Table",
                GenerateComments = true,
                UseSQLiteAttributes = true,
                GenerateSerializable = true
            };

            var generator = new CodeGenerator(config);
            var reader = new ExcelReader();

            var sheets = reader.ReadExcel("Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx");
            if (sheets.Count > 0)
            {
                var result = generator.GenerateConfigClass(sheets[0], "ItemConfig");

                // 保存文件
                var dataDirectory = Path.GetDirectoryName(result.DataClassPath);
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }
                File.WriteAllText(result.DataClassPath, result.DataClassCode, System.Text.Encoding.UTF8);

                var tableDirectory = Path.GetDirectoryName(result.TableClassPath);
                if (!Directory.Exists(tableDirectory))
                {
                    Directory.CreateDirectory(tableDirectory);
                }
                File.WriteAllText(result.TableClassPath, result.TableClassCode, System.Text.Encoding.UTF8);

                Debug.Log($"代码已生成到自定义路径:\n{result.DataClassPath}\n{result.TableClassPath}");
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 示例4：指定类名
        /// </summary>
        [MenuItem("Tools/Excel/Examples/指定类名")]
        public static void Example4_CustomClassName()
        {
            var reader = new ExcelReader();
            var sheets = reader.ReadExcel("Assets/Editor/ExcelTool/TestData/ItemConfig.xlsx");

            if (sheets.Count > 0)
            {
                var generator = new CodeGenerator();
                
                // 使用自定义类名而不是表名
                var result = generator.GenerateConfigClass(sheets[0], "MyCustomItemConfig");

                // 保存文件
                var dataDirectory = Path.GetDirectoryName(result.DataClassPath);
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }
                File.WriteAllText(result.DataClassPath, result.DataClassCode, System.Text.Encoding.UTF8);

                var tableDirectory = Path.GetDirectoryName(result.TableClassPath);
                if (!Directory.Exists(tableDirectory))
                {
                    Directory.CreateDirectory(tableDirectory);
                }
                File.WriteAllText(result.TableClassPath, result.TableClassCode, System.Text.Encoding.UTF8);

                Debug.Log($"已生成自定义类名: MyCustomItemConfig");
                AssetDatabase.Refresh();
            }
        }
    }
}
