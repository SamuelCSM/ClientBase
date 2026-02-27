using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages.GC2GS
{
    /// <summary>
    /// 更新玩家昵称请求
    /// </summary>
    [ProtoContract]
    public class GC2GS_002_002_UpdateNicknameRequest : IMessage
    {
        /// <summary>
        /// 新昵称
        /// </summary>
        [ProtoMember(1)]
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
