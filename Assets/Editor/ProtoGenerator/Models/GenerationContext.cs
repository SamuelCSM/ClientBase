using System.Collections.Generic;

namespace ProtoGenerator.Models
{
    /// <summary>
    /// 生成上下文
    /// </summary>
    public class GenerationContext
    {
        /// <summary>
        /// 输出基础路径
        /// </summary>
        public string OutputBasePath { get; set; }
        
        /// <summary>
        /// 项目命名空间
        /// </summary>
        public string ProjectNamespace { get; set; }
        
        /// <summary>
        /// 类型映射字典
        /// </summary>
        public Dictionary<string, string> TypeMappings { get; set; }
        
        /// <summary>
        /// 生成选项
        /// </summary>
        public GenerationOptions Options { get; set; }

        public GenerationContext()
        {
            TypeMappings = new Dictionary<string, string>();
            Options = new GenerationOptions();
        }
    }
}
