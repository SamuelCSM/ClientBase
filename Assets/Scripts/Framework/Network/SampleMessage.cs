using ProtoBuf;

namespace Framework.Network
{
    /// <summary>
    /// 消息模块ID定义
    /// 主ID用于区分不同的功能模块
    /// </summary>
    public static class MessageModule
    {
        public const byte System = 1;      // 系统模块（心跳、ping等）
        public const byte Login = 2;       // 登录认证模块
        public const byte Player = 3;      // 玩家数据模块
        public const byte Battle = 4;      // 战斗模块
        public const byte Social = 5;      // 社交模块
        public const byte Shop = 6;        // 商店模块
        public const byte Chat = 7;        // 聊天模块
    }

    /// <summary>
    /// 示例消息类 - 演示如何定义Protobuf消息
    /// 实际项目中，消息定义应该放在HotUpdate/Proto/目录下
    /// </summary>
    [ProtoContract]
    public class SampleMessage : IMessage
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string Content { get; set; }

        [ProtoMember(3)]
        public long Timestamp { get; set; }

        /// <summary>
        /// 获取主消息ID（模块ID）
        /// </summary>
        public byte GetMainId()
        {
            return MessageModule.System; // 系统模块
        }

        /// <summary>
        /// 获取子消息ID（消息类型ID）
        /// </summary>
        public byte GetSubId()
        {
            return 99; // 示例消息
        }
    }

    /// <summary>
    /// 心跳请求消息
    /// 主ID: 1 (System), 子ID: 1
    /// </summary>
    [ProtoContract]
    public class HeartbeatRequest : IMessage
    {
        [ProtoMember(1)]
        public long ClientTime { get; set; }

        public byte GetMainId() => MessageModule.System;
        public byte GetSubId() => 1;
    }

    /// <summary>
    /// 心跳响应消息
    /// 主ID: 1 (System), 子ID: 2
    /// </summary>
    [ProtoContract]
    public class HeartbeatResponse : IMessage
    {
        [ProtoMember(1)]
        public long ServerTime { get; set; }

        public byte GetMainId() => MessageModule.System;
        public byte GetSubId() => 2;
    }

    /// <summary>
    /// 登录请求消息
    /// 主ID: 2 (Login), 子ID: 1
    /// </summary>
    [ProtoContract]
    public class LoginRequest : IMessage
    {
        [ProtoMember(1)]
        public string Username { get; set; }

        [ProtoMember(2)]
        public string Password { get; set; }

        public byte GetMainId() => MessageModule.Login;
        public byte GetSubId() => 1;
    }

    /// <summary>
    /// 登录响应消息
    /// 主ID: 2 (Login), 子ID: 2
    /// </summary>
    [ProtoContract]
    public class LoginResponse : IMessage
    {
        [ProtoMember(1)]
        public int ResultCode { get; set; }

        [ProtoMember(2)]
        public string Token { get; set; }

        [ProtoMember(3)]
        public string Message { get; set; }

        public byte GetMainId() => MessageModule.Login;
        public byte GetSubId() => 2;
    }
}
