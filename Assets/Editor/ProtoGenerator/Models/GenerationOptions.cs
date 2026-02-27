namespace ProtoGenerator.Models
{
    /// <summary>
    /// 生成选项
    /// </summary>
    public class GenerationOptions
    {
        /// <summary>
        /// 是否生成XML注释
        /// </summary>
        public bool GenerateXmlComments { get; set; }
        
        /// <summary>
        /// 是否覆盖现有文件
        /// </summary>
        public bool OverwriteExisting { get; set; }
        
        /// <summary>
        /// 是否备份现有文件
        /// </summary>
        public bool BackupExisting { get; set; }

        public GenerationOptions()
        {
            GenerateXmlComments = true;
            OverwriteExisting = true;
            BackupExisting = false;
        }
    }
}
