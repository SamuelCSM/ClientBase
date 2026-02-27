using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.GS2GC
{
    /// <summary>
    /// 登录响应消息
    /// </summary>
    [ProtoContract]
    public class GS2GC_001_001_LoginResponse : IMessage
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

        /// <summary>
        /// 会话令牌
        /// </summary>
        [ProtoMember(4)]
        public string SessionToken { get; set; }

        /// <summary>
        /// 用户昵称
        /// </summary>
        [ProtoMember(5)]
        public string Nickname { get; set; }

        /// <summary>
        /// 用户等级
        /// </summary>
        [ProtoMember(6)]
        public int Level { get; set; }

        /// <summary>
        /// 服务器时间戳
        /// </summary>
        [ProtoMember(7)]
        public long ServerTime { get; set; }

        public byte GetMainId()
        {
            return 1;
        }

        public byte GetSubId()
        {
            return 1;
        }
    }
}
