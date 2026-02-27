using System.Collections.Generic;

namespace ProtoGenerator.Models
{
    /// <summary>
    /// 枚举定义数据模型
    /// </summary>
    public class EnumDefinition
    {
        /// <summary>
        /// 枚举名
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 命名空间
        /// </summary>
        public string Namespace { get; set; }
        
        /// <summary>
        /// 枚举注释
        /// </summary>
        public string Comment { get; set; }
        
        /// <summary>
        /// 枚举值列表
        /// </summary>
        public List<EnumValueDefinition> Values { get; set; }
        
        /// <summary>
        /// 源文件路径
        /// </summary>
        public string SourceFilePath { get; set; }

        public EnumDefinition()
        {
            Values = new List<EnumValueDefinition>();
        }
    }
}
