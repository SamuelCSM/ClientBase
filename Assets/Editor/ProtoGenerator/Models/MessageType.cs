namespace ProtoGenerator.Models
{
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// 枚举定义
        /// </summary>
        Enum,
        
        /// <summary>
        /// 通用类定义
        /// </summary>
        Common,
        
        /// <summary>
        /// 发送协议（客户端到服务器）
        /// </summary>
        Send,
        
        /// <summary>
        /// 接收协议（服务器到客户端）
        /// </summary>
        Receive
    }
}
