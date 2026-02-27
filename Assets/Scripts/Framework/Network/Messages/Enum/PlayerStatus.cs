using ProtoBuf;

namespace Framework.Network.Messages.Enum
{
    /// <summary>
    /// PlayerStatus
    /// </summary>
    public enum PlayerStatus
    {
        /// <summary>
        /// 离线
        /// </summary>
        Offline = 0,
        /// <summary>
        /// 在线
        /// </summary>
        Online = 1,
        /// <summary>
        /// 忙碌
        /// </summary>
        Busy = 2,
        /// <summary>
        /// 离开
        /// </summary>
        Away = 3
    }
}
