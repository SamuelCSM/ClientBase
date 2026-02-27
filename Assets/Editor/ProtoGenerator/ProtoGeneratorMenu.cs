using UnityEditor;
using ProtoGenerator.Core;

namespace ProtoGenerator
{
    /// <summary>
    /// Proto生成器Unity编辑器菜单
    /// </summary>
    public static class ProtoGeneratorMenu
    {
        [MenuItem("Tools/ProtoGenerator/Generate All Classes")]
        public static void GenerateAllClasses()
        {
            ProtoGeneratorLogger.Log("开始生成所有Proto类...");
            ProtoGenerator.GenerateAllClasses();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/ProtoGenerator/Open Definitions Folder")]
        public static void OpenDefinitionsFolder()
        {
            var path = "Assets/ProtoDefinitions";
            if (System.IO.Directory.Exists(path))
            {
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                ProtoGeneratorLogger.LogWarning($"定义文件夹不存在: {path}");
            }
        }

        [MenuItem("Tools/ProtoGenerator/Open Output Folder")]
        public static void OpenOutputFolder()
        {
            var path = "Assets/Scripts/Framework/Network/Messages";
            if (System.IO.Directory.Exists(path))
            {
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                ProtoGeneratorLogger.LogWarning($"输出文件夹不存在: {path}");
            }
        }

        [MenuItem("Tools/ProtoGenerator/Clear Generated Files")]
        public static void ClearGeneratedFiles()
        {
            if (EditorUtility.DisplayDialog("确认清除", 
                "确定要清除所有生成的文件吗？此操作不可撤销！", 
                "确定", "取消"))
            {
                var outputPath = "Assets/Scripts/Framework/Network/Messages";
                if (System.IO.Directory.Exists(outputPath))
                {
                    var directories = new[] { "GC2GS", "GS2GC", "Common", "Enum" };
                    foreach (var dir in directories)
                    {
                        var fullPath = System.IO.Path.Combine(outputPath, dir);
                        if (System.IO.Directory.Exists(fullPath))
                        {
                            System.IO.Directory.Delete(fullPath, true);
                            ProtoGeneratorLogger.Log($"已删除目录: {fullPath}");
                        }
                    }
                    AssetDatabase.Refresh();
                    ProtoGeneratorLogger.LogSuccess("清除完成！");
                }
            }
        }
    }
}
