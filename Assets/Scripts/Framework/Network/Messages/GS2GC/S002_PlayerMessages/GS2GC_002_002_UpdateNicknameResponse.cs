using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.GS2GC
{
    /// <summary>
    /// 更新玩家昵称响应
    /// </summary>
    [ProtoContract]
    public class GS2GC_002_002_UpdateNicknameResponse : IMessage
    {
        /// <summary>
        /// 结果码
        /// </summary>
        [ProtoMember(1)]
        public int ResultCode { get; set; }

        /// <summary>
        /// 新昵称
        /// </summary>
        [ProtoMember(2)]
        public string NewNickname { get; set; }

        public byte GetMainId()
        {
            return 2;
        }

        public byte GetSubId()
        {
            return 2;
        }
    }
}
