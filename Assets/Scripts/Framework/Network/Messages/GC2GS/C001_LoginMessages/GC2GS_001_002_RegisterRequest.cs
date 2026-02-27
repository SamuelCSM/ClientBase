using ProtoBuf;
using Framework.Network;
using Framework.Network.Messages.Common;
using Framework.Network.Messages.Enum;

namespace Framework.Network.Messages.GC2GS
{
    /// <summary>
    /// 注册请求消息
    /// </summary>
    [ProtoContract]
    public class GC2GS_001_002_RegisterRequest : IMessage
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
        /// 邮箱
        /// </summary>
        [ProtoMember(3)]
        public string Email { get; set; }

        /// <summary>
        /// 平台类型
        /// </summary>
        [ProtoMember(4)]
        public PlatformType Platform { get; set; }

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
