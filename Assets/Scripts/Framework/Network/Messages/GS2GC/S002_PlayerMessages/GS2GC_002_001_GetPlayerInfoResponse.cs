using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.GS2GC
{
    /// <summary>
    /// 获取玩家信息响应
    /// </summary>
    [ProtoContract]
    public class GS2GC_002_001_GetPlayerInfoResponse : IMessage
    {
        /// <summary>
        /// 结果码
        /// </summary>
        [ProtoMember(1)]
        public int ResultCode { get; set; }

        /// <summary>
        /// 玩家ID
        /// </summary>
        [ProtoMember(2)]
        public long PlayerId { get; set; }

        /// <summary>
        /// 玩家名称
        /// </summary>
        [ProtoMember(3)]
        public string PlayerName { get; set; }

        /// <summary>
        /// 等级
        /// </summary>
        [ProtoMember(4)]
        public int Level { get; set; }

        /// <summary>
        /// 经验值
        /// </summary>
        [ProtoMember(5)]
        public long Exp { get; set; }

        public byte GetMainId()
        {
            return 2;
        }

        public byte GetSubId()
        {
            return 1;
        }
    }
}
