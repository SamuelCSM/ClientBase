using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.GC2GS
{
    /// <summary>
    /// 获取玩家信息请求
    /// </summary>
    [ProtoContract]
    public class GC2GS_002_001_GetPlayerInfoRequest : IMessage
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        [ProtoMember(1)]
        public long PlayerId { get; set; }

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
