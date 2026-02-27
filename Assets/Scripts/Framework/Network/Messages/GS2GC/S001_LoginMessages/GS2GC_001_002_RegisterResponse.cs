using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.GS2GC
{
    /// <summary>
    /// 注册响应消息
    /// </summary>
    [ProtoContract]
    public class GS2GC_001_002_RegisterResponse : IMessage
    {
        /// <summary>
        /// 结果码
        /// </summary>
        [ProtoMember(1)]
        public int ResultCode { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [ProtoMember(3)]
        public long UserId { get; set; }

        public byte GetMainId()
        {
            return 1;
        }

        public byte GetSubId()
        {
            return 2;
        }
    }
}
