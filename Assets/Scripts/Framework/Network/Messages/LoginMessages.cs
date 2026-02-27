using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages
{
    /// <summary>
    /// 登录请求消息
    /// </summary>
    [ProtoContract]
    public class LoginRequest : IMessage
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [ProtoMember(1)]
        public string Username { get; set; }

        /// <summary>
        /// 密码（建议客户端加密后传输）
        /// </summary>
        [ProtoMember(2)]
        public string Password { get; set; }

        /// <summary>
        /// 客户端版本号
        /// </summary>
        [ProtoMember(3)]
        public string ClientVersion { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        [ProtoMember(4)]
        public string DeviceId { get; set; }

        /// <summary>
        /// 平台类型（0=Windows, 1=Android, 2=iOS）
        /// </summary>
        [ProtoMember(5)]
        public int Platform { get; set; }

        public byte GetMainId()
        {
            return MessageModule.Login;
        }

        public byte GetSubId()
        {
            return 1; // 登录请求
        }
    }

    /// <summary>
    /// 登录响应消息
    /// </summary>
    [ProtoContract]
    public class LoginResponse : IMessage
    {
        /// <summary>
        /// 结果码（0=成功，其他=失败）
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
            return MessageModule.Login;
        }

        public byte GetSubId()
        {
            return 2; // 登录响应
        }
    }
}
