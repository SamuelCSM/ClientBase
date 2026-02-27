namespace ProtoGenerator.Models
{
    /// <summary>
    /// 生成错误信息
    /// </summary>
    public class GenerationError
    {
        /// <summary>
        /// 错误类型
        /// </summary>
        public ErrorType Type { get; set; }
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; }

        public GenerationError(ErrorType type, string message, string filePath = null, int lineNumber = 0)
        {
            Type = type;
            Message = message;
            FilePath = filePath;
            LineNumber = lineNumber;
        }
    }

    /// <summary>
    /// 错误类型枚举
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// 语法错误
        /// </summary>
        SyntaxError,
        
        /// <summary>
        /// 文件系统错误
        /// </summary>
        FileSystemError,
        
        /// <summary>
        /// 生成错误
        /// </summary>
        GenerationError,
        
        /// <summary>
        /// 验证错误
        /// </summary>
        ValidationError
    }
}
