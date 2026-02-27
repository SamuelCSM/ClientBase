using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.GC2GS
{
    /// <summary>
    /// 登录请求消息
    /// </summary>
    [ProtoContract]
    public class GC2GS_001_001_LoginRequest : IMessage
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [ProtoMember(1)]
        public string Username { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [ProtoMember(2)]
        public string Password { get; set; }

        /// <summary>
        /// 客户端版本
        /// </summary>
        [ProtoMember(3)]
        public string ClientVersion { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        [ProtoMember(4)]
        public string DeviceId { get; set; }

        /// <summary>
        /// 平台类型
        /// </summary>
        [ProtoMember(5)]
        public int Platform { get; set; }

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
