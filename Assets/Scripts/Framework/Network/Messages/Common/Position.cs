using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.Common
{
    /// <summary>
    /// 位置信息通用类
    /// </summary>
    [ProtoContract]
    public class Position
    {
        /// <summary>
        /// X坐标
        /// </summary>
        [ProtoMember(1)]
        public float X { get; set; }

        /// <summary>
        /// Y坐标
        /// </summary>
        [ProtoMember(2)]
        public float Y { get; set; }

        /// <summary>
        /// Z坐标
        /// </summary>
        [ProtoMember(3)]
        public float Z { get; set; }

    }
}
