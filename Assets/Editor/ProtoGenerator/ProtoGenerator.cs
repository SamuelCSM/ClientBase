using System;
using System.Diagnostics;
using ProtoGenerator.Core;
using ProtoGenerator.Models;

namespace ProtoGenerator
{
    public class ProtoGenerator
    {
        private readonly DefinitionFileFinder _fileFinder;
        private readonly DefinitionParser _parser;
        private readonly CodeGenerator _codeGenerator;
        private readonly FileWriter _fileWriter;
        private readonly PathResolver _pathResolver;
        private readonly DependencyResolver _dependencyResolver;

        public ProtoGenerator()
        {
            _fileFinder = new DefinitionFileFinder();
            _parser = new DefinitionParser();
            _codeGenerator = new CodeGenerator();
            _fileWriter = new FileWriter();
            _pathResolver = new PathResolver();
            _dependencyResolver = new DependencyResolver();
        }

        public static void GenerateAllClasses()
        {
            var generator = new ProtoGenerator();
            var result = generator.GenerateAll();
            ShowGenerationResult(result);
        }

        public GenerationResult GenerateAll()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new GenerationResult();

            try
            {
                ProtoGeneratorLogger.Log("========== 开始生成Proto类 ==========");

                var files = _fileFinder.FindAllDefinitionFiles();
                if (files.Length == 0)
                {
                    ProtoGeneratorLogger.LogWarning("未找到定义文件");
                    result.Success = false;
                    return result;
                }

                result = GenerateFromFiles(files);
            }
            catch (Exception ex)
            {
                ProtoGeneratorLogger.LogError($"生成过程出错: {ex.Message}");
                result.Success = false;
                result.Errors.Add(new GenerationError(ErrorType.GenerationError, ex.Message));
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            return result;
        }

        public static GenerationResult GenerateFromFiles(string[] definitionFiles)
        {
            var generator = new ProtoGenerator();
            return generator.Generate(definitionFiles);
        }

        public GenerationResult Generate(string[] definitionFiles)
        {
            var result = new GenerationResult();
            var context = new GenerationContext
            {
                OutputBasePath = "Assets/Scripts/Framework/Network/Messages",
                ProjectNamespace = "Framework.Network.Messages"
            };

            try
            {
                var definitions = _parser.ParseMultipleFiles(definitionFiles);
                ProtoGeneratorLogger.Log($"成功解析 {definitions.Length} 个定义文件");

                _dependencyResolver.ValidateDependencies(definitions);

                foreach (var definition in definitions)
                {
                    try
                    {
                        _dependencyResolver.ResolveDependencies(definition);
                        var code = _codeGenerator.GenerateClass(definition, context);

                        if (!string.IsNullOrEmpty(code))
                        {
                            // 使用消息名称作为文件名
                            var outputPath = _pathResolver.ResolveOutputPathForMessage(definition);
                            _fileWriter.WriteClass(code, outputPath);
                            result.GeneratedFiles.Add(outputPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        var error = new GenerationError(ErrorType.GenerationError, ex.Message, definition.SourceFilePath);
                        result.Errors.Add(error);
                        ProtoGeneratorLogger.LogError($"生成类失败 {definition.Name}: {ex.Message}");
                    }
                }

                result.Success = result.Errors.Count == 0;
                ProtoGeneratorLogger.LogSuccess($"生成完成！成功: {result.GeneratedFiles.Count}, 失败: {result.Errors.Count}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add(new GenerationError(ErrorType.GenerationError, ex.Message));
                ProtoGeneratorLogger.LogError($"生成过程出错: {ex.Message}");
            }

            return result;
        }

        private static void ShowGenerationResult(GenerationResult result)
        {
            ProtoGeneratorLogger.Log("\n========== 生成结果 ==========");
            ProtoGeneratorLogger.Log($"耗时: {result.Duration.TotalSeconds:F2} 秒");
            ProtoGeneratorLogger.Log($"成功生成: {result.GeneratedFiles.Count} 个文件");

            if (result.GeneratedFiles.Count > 0)
            {
                ProtoGeneratorLogger.Log("\n生成的文件:");
                foreach (var file in result.GeneratedFiles)
                {
                    ProtoGeneratorLogger.Log($"  - {file}");
                }
            }

            if (result.Errors.Count > 0)
            {
                ProtoGeneratorLogger.LogError($"\n错误: {result.Errors.Count} 个");
                foreach (var error in result.Errors)
                {
                    ProtoGeneratorLogger.LogError($"  [{error.Type}] {error.Message}");
                    if (!string.IsNullOrEmpty(error.FilePath))
                    {
                        ProtoGeneratorLogger.LogError($"    文件: {error.FilePath}");
                    }
                }
            }

            if (result.Success)
            {
                ProtoGeneratorLogger.LogSuccess("\n========== 生成成功！ ==========");
            }
            else
            {
                ProtoGeneratorLogger.LogError("\n========== 生成失败！ ==========");
            }
        }
    }
}
