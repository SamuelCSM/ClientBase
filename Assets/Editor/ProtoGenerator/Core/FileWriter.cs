using System;
using System.IO;
using ProtoGenerator.Models;

namespace ProtoGenerator.Core
{
    public class FileWriter
    {
        private readonly PathResolver _pathResolver;

        public FileWriter()
        {
            _pathResolver = new PathResolver();
        }

        public void WriteClass(string code, string outputPath)
        {
            if (string.IsNullOrEmpty(code))
            {
                ProtoGeneratorLogger.LogError("生成的代码为空");
                return;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                ProtoGeneratorLogger.LogError("输出路径为空");
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(outputPath);
                EnsureDirectoryExists(directory);

                if (File.Exists(outputPath))
                {
                    ProtoGeneratorLogger.Log($"覆盖现有文件: {outputPath}");
                }

                File.WriteAllText(outputPath, code);
                ProtoGeneratorLogger.LogSuccess($"成功写入文件: {outputPath}");
            }
            catch (Exception ex)
            {
                ProtoGeneratorLogger.LogError($"写入文件失败 {outputPath}: {ex.Message}");
            }
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return;

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                ProtoGeneratorLogger.Log($"创建目录: {directoryPath}");
            }
        }

        private void BackupExistingFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            var backupPath = filePath + ".backup";
            File.Copy(filePath, backupPath, true);
            ProtoGeneratorLogger.Log($"备份文件: {backupPath}");
        }

        public bool WriteGeneratedClass(MessageDefinition definition, string code, bool backup = false)
        {
            try
            {
                var pathResolver = new PathResolver();
                var outputPath = pathResolver.ResolveOutputPathForMessage(definition);

                if (backup && File.Exists(outputPath))
                {
                    BackupExistingFile(outputPath);
                }

                WriteClass(code, outputPath);
                return true;
            }
            catch (Exception ex)
            {
                ProtoGeneratorLogger.LogError($"写入生成的类失败: {ex.Message}");
                return false;
            }
        }
    }
}
