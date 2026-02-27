using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages
{
    /// <summary>
    /// 心跳请求消息
    /// 客户端定期发送心跳包以保持连接活跃
    /// </summary>
    [ProtoContract]
    public class HeartbeatRequest : IMessage
    {
        /// <summary>
        /// 客户端时间戳（毫秒）
        /// </summary>
        [ProtoMember(1)]
        public long ClientTime { get; set; }

        /// <summary>
        /// 序列号（用于匹配请求和响应）
        /// </summary>
        [ProtoMember(2)]
        public int SequenceId { get; set; }

        public byte GetMainId()
        {
            return MessageModule.System;
        }

        public byte GetSubId()
        {
            return 1; // 心跳请求
        }
    }

    /// <summary>
    /// 心跳响应消息
    /// 服务器响应客户端的心跳包
    /// </summary>
    [ProtoContract]
    public class HeartbeatResponse : IMessage
    {
        /// <summary>
        /// 服务器时间戳（毫秒）
        /// </summary>
        [ProtoMember(1)]
        public long ServerTime { get; set; }

        /// <summary>
        /// 序列号（与请求匹配）
        /// </summary>
        [ProtoMember(2)]
        public int SequenceId { get; set; }

        /// <summary>
        /// 服务器状态（0=正常，1=维护中，2=即将关闭）
        /// </summary>
        [ProtoMember(3)]
        public int ServerStatus { get; set; }

        public byte GetMainId()
        {
            return MessageModule.System;
        }

        public byte GetSubId()
        {
            return 2; // 心跳响应
        }
    }
}
