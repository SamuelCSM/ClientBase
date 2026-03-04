using System;
using System.IO;
using Cysharp.Threading.Tasks;

namespace Framework.HotUpdate
{
    /// <summary>
    /// 文件校验器
    /// 负责校验文件完整性（MD5校验）
    /// </summary>
    public class FileVerifier
    {
        /// <summary>
        /// 校验文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="expectedMD5">期望的MD5值</param>
        /// <returns>是否校验通过</returns>
        public bool VerifyFile(string filePath, string expectedMD5)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Logger.Error($"[FileVerifier] 文件不存在: {filePath}");
                    return false;
                }
                
                string actualMD5 = CalculateMD5(filePath);
                bool isValid = string.Equals(actualMD5, expectedMD5, StringComparison.OrdinalIgnoreCase);
                
                if (isValid)
                {
                    Logger.Log($"[FileVerifier] 文件校验通过: {filePath}");
                }
                else
                {
                    Logger.Error($"[FileVerifier] 文件校验失败: {filePath}, 期望MD5={expectedMD5}, 实际MD5={actualMD5}");
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                Logger.Error($"[FileVerifier] 校验文件异常: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 异步校验文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="expectedMD5">期望的MD5值</param>
        /// <returns>是否校验通过</returns>
        public async UniTask<bool> VerifyFileAsync(string filePath, string expectedMD5)
        {
            // 在后台线程执行MD5计算
            return await UniTask.RunOnThreadPool(() => VerifyFile(filePath, expectedMD5));
        }
        
        /// <summary>
        /// 计算文件MD5
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>MD5字符串</returns>
        public string CalculateMD5(string filePath)
        {
            return MD5Util.GetFileMD5(filePath);
        }
    }
}
