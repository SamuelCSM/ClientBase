using UnityEditor;
using ProtoGenerator.Core;
using ProtoGenerator.Models;

namespace ProtoGenerator.Tests
{
    /// <summary>
    /// 解析器测试类
    /// 用于验证文件发现和解析功能
    /// </summary>
    public static class ParserTest
    {
        [MenuItem("Tools/ProtoGenerator/Test/Run Parser Test")]
        public static void RunTest()
        {
            ProtoGeneratorLogger.Log("========== 开始解析器测试 ==========");

            // 测试1: 文件发现
            TestFileFinder();

            // 测试2: 文件解析
            TestFileParser();

            ProtoGeneratorLogger.LogSuccess("========== 解析器测试完成 ==========");
        }

        private static void TestFileFinder()
        {
            ProtoGeneratorLogger.Log("\n--- 测试1: 文件发现 ---");

            var finder = new DefinitionFileFinder();
            var files = finder.FindAllDefinitionFiles();

            ProtoGeneratorLogger.Log($"找到 {files.Length} 个定义文件:");
            foreach (var file in files)
            {
                ProtoGeneratorLogger.Log($"  - {file}");
            }

            // 测试路径过滤
            var sendFiles = finder.FilterByPathPattern(files, "GC2GS");
            ProtoGeneratorLogger.Log($"\n发送协议文件 (GC2GS): {sendFiles.Length} 个");

            var recvFiles = finder.FilterByPathPattern(files, "GS2GC");
            ProtoGeneratorLogger.Log($"接收协议文件 (GS2GC): {recvFiles.Length} 个");

            var commonFiles = finder.FilterByPathPattern(files, "Common");
            ProtoGeneratorLogger.Log($"通用类文件 (Common): {commonFiles.Length} 个");

            var enumFiles = finder.FilterByPathPattern(files, "Enum");
            ProtoGeneratorLogger.Log($"枚举文件 (Enum): {enumFiles.Length} 个");
        }

        private static void TestFileParser()
        {
            ProtoGeneratorLogger.Log("\n--- 测试2: 文件解析 ---");

            var finder = new DefinitionFileFinder();
            var parser = new DefinitionParser();

            var files = finder.FindAllDefinitionFiles();
            var definitions = parser.ParseMultipleFiles(files);

            ProtoGeneratorLogger.Log($"\n成功解析 {definitions.Length} 个定义:");
            foreach (var def in definitions)
            {
                ProtoGeneratorLogger.Log($"\n消息: {def.Name}");
                ProtoGeneratorLogger.Log($"  类型: {def.Type}");
                ProtoGeneratorLogger.Log($"  命名空间: {def.Namespace}");
                ProtoGeneratorLogger.Log($"  MainId: {def.MainId}, SubId: {def.SubId}");
                ProtoGeneratorLogger.Log($"  字段数量: {def.Fields.Count}");

                foreach (var field in def.Fields)
                {
                    var comment = string.IsNullOrEmpty(field.Comment) ? "" : $" // {field.Comment}";
                    ProtoGeneratorLogger.Log($"    [{field.ProtoMemberIndex}] {field.Type} {field.Name}{comment}");
                }
            }
        }
    }
}
