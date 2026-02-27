namespace ProtoGenerator.Models
{
    /// <summary>
    /// 枚举值定义数据模型
    /// </summary>
    public class EnumValueDefinition
    {
        /// <summary>
        /// 枚举值名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 枚举值
        /// </summary>
        public int Value { get; set; }
        
        /// <summary>
        /// 注释
        /// </summary>
        public string Comment { get; set; }
    }
}
