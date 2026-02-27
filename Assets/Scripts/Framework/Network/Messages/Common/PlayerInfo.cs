using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.Common
{
    /// <summary>
    /// 玩家信息通用类
    /// </summary>
    [ProtoContract]
    public class PlayerInfo
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        [ProtoMember(1)]
        public long PlayerId { get; set; }

        /// <summary>
        /// 玩家名称
        /// </summary>
        [ProtoMember(2)]
        public string PlayerName { get; set; }

        /// <summary>
        /// 等级
        /// </summary>
        [ProtoMember(3)]
        public int Level { get; set; }

        /// <summary>
        /// 经验值
        /// </summary>
        [ProtoMember(4)]
        public long Exp { get; set; }

        /// <summary>
        /// VIP等级
        /// </summary>
        [ProtoMember(5)]
        public int VipLevel { get; set; }

    }
}
