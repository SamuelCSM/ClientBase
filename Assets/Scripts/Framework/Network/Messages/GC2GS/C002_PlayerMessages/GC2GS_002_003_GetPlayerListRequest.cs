using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.GC2GS
{
    /// <summary>
    /// 获取玩家列表请求
    /// </summary>
    [ProtoContract]
    public class GC2GS_002_003_GetPlayerListRequest : IMessage
    {
        /// <summary>
        /// 页码
        /// </summary>
        [ProtoMember(1)]
        public int PageIndex { get; set; }

        /// <summary>
        /// 每页数量
        /// </summary>
        [ProtoMember(2)]
        public int PageSize { get; set; }

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
