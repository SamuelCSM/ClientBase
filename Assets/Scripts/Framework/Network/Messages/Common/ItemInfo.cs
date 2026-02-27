using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.Common
{
    /// <summary>
    /// 物品信息通用类
    /// </summary>
    [ProtoContract]
    public class ItemInfo
    {
        /// <summary>
        /// 物品ID
        /// </summary>
        [ProtoMember(1)]
        public int ItemId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [ProtoMember(2)]
        public int Count { get; set; }

        /// <summary>
        /// 品质
        /// </summary>
        [ProtoMember(3)]
        public int Quality { get; set; }

    }
}
