namespace Framework.Network
{
    /// <summary>
    /// 网络消息接口
    /// 所有Protobuf消息都应该实现此接口
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// 获取主消息ID（模块ID）
        /// </summary>
        /// <returns>主消息ID</returns>
        byte GetMainId();

        /// <summary>
        /// 获取子消息ID（消息类型ID）
        /// </summary>
        /// <returns>子消息ID</returns>
        byte GetSubId();
    }
}
