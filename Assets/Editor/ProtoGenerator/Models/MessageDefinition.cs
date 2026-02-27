using System.Collections.Generic;

namespace ProtoGenerator.Models
{
    /// <summary>
    /// 消息定义数据模型
    /// </summary>
    public class MessageDefinition
    {
        /// <summary>
        /// 消息类名
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 完整消息名称（包含前缀和协议号，用于文件名）
        /// 例如：GC2GS_001_001_LoginRequest
        /// </summary>
        public string FullName { get; set; }
        
        /// <summary>
        /// 类注释
        /// </summary>
        public string Comment { get; set; }
        
        /// <summary>
        /// 命名空间
        /// </summary>
        public string Namespace { get; set; }
        
        /// <summary>
        /// 消息类型
        /// </summary>
        public MessageType Type { get; set; }
        
        /// <summary>
        /// 字段列表
        /// </summary>
        public List<FieldDefinition> Fields { get; set; }
        
        /// <summary>
        /// 源文件路径
        /// </summary>
        public string SourceFilePath { get; set; }
        
        /// <summary>
        /// 主消息ID（模块ID）
        /// </summary>
        public byte MainId { get; set; }
        
        /// <summary>
        /// 子消息ID（消息类型ID）
        /// </summary>
        public byte SubId { get; set; }
        
        /// <summary>
        /// 依赖的类型列表
        /// </summary>
        public List<string> Dependencies { get; set; }
        
        /// <summary>
        /// 需要的using语句列表
        /// </summary>
        public List<string> RequiredUsings { get; set; }

        public MessageDefinition()
        {
            Fields = new List<FieldDefinition>();
            Dependencies = new List<string>();
            RequiredUsings = new List<string>();
        }
    }
}
