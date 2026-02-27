using System;
using System.Collections.Generic;

namespace ProtoGenerator.Models
{
    /// <summary>
    /// 生成结果
    /// </summary>
    public class GenerationResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// 生成的文件列表
        /// </summary>
        public List<string> GeneratedFiles { get; set; }
        
        /// <summary>
        /// 错误列表
        /// </summary>
        public List<GenerationError> Errors { get; set; }
        
        /// <summary>
        /// 生成耗时
        /// </summary>
        public TimeSpan Duration { get; set; }

        public GenerationResult()
        {
            GeneratedFiles = new List<string>();
            Errors = new List<GenerationError>();
        }
    }
}
