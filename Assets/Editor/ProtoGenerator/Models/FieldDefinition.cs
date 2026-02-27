namespace ProtoGenerator.Models
{
    /// <summary>
    /// 字段定义数据模型
    /// </summary>
    public class FieldDefinition
    {
        /// <summary>
        /// 字段名
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 字段类型
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// ProtoMember索引
        /// </summary>
        public int ProtoMemberIndex { get; set; }
        
        /// <summary>
        /// 是否可选
        /// </summary>
        public bool IsOptional { get; set; }
        
        /// <summary>
        /// 注释
        /// </summary>
        public string Comment { get; set; }
    }
}
