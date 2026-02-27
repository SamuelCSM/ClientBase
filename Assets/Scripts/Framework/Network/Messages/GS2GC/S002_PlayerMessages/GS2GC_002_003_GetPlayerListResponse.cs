using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.GS2GC
{
    /// <summary>
    /// 获取玩家列表响应
    /// </summary>
    [ProtoContract]
    public class GS2GC_002_003_GetPlayerListResponse : IMessage
    {
        /// <summary>
        /// 结果码
        /// </summary>
        [ProtoMember(1)]
        public int ResultCode { get; set; }

        /// <summary>
        /// 总数量
        /// </summary>
        [ProtoMember(2)]
        public int TotalCount { get; set; }

        public byte GetMainId()
        {
            return 2;
        }

        public byte GetSubId()
        {
            return 3;
        }
    }
}
