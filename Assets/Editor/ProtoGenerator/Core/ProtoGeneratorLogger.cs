using UnityEngine;

namespace ProtoGenerator.Core
{
    /// <summary>
    /// Proto生成器日志工具
    /// </summary>
    public static class ProtoGeneratorLogger
    {
        private const string LogPrefix = "[ProtoGenerator]";

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void Log(string message)
        {
            Debug.Log($"{LogPrefix} {message}");
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{LogPrefix} {message}");
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError($"{LogPrefix} {message}");
        }

        /// <summary>
        /// 记录成功日志（绿色）
        /// </summary>
        public static void LogSuccess(string message)
        {
            Debug.Log($"<color=green>{LogPrefix} {message}</color>");
        }
    }
}
