using ProtoBuf;

namespace Framework.Network.Messages.Enum
{
    /// <summary>
    /// ErrorCode
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 0,
        /// <summary>
        /// 参数无效
        /// </summary>
        InvalidParameter = 1,
        /// <summary>
        /// 未找到
        /// </summary>
        NotFound = 2,
        /// <summary>
        /// 权限不足
        /// </summary>
        PermissionDenied = 3,
        /// <summary>
        /// 服务器错误
        /// </summary>
        ServerError = 4
    }
}
