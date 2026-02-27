using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Framework.Network
{
    /// <summary>
    /// 消息分发器
    /// 负责将网络消息分发到对应的处理器
    /// 支持主线程消息队列和异常捕获
    /// </summary>
    public class MessageDispatcher
    {
        /// <summary>
        /// 消息处理器委托
        /// </summary>
        /// <param name="payload">消息体数据</param>
        public delegate void MessageHandler(byte[] payload);

        // 消息处理器字典：Key为完整消息ID（主ID<<8 | 子ID），Value为处理器
        private readonly Dictionary<ushort, MessageHandler> _handlers = new Dictionary<ushort, MessageHandler>();

        // 主线程消息队列（线程安全）
        private readonly Queue<PendingMessage> _messageQueue = new Queue<PendingMessage>();
        private readonly object _queueLock = new object();

        /// <summary>
        /// 待处理消息
        /// </summary>
        private struct PendingMessage
        {
            public byte MainId;
            public byte SubId;
            public byte[] Payload;
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <param name="mainId">主消息ID</param>
        /// <param name="subId">子消息ID</param>
        /// <param name="handler">处理器</param>
        public void RegisterHandler(byte mainId, byte subId, MessageHandler handler)
        {
            if (handler == null)
            {
                Logger.Error("消息处理器不能为空");
                return;
            }

            ushort msgId = MessagePacket.CombineMessageId(mainId, subId);

            if (_handlers.ContainsKey(msgId))
            {
                Logger.Warning($"消息处理器已存在，将被覆盖: 主ID={mainId}, 子ID={subId}");
            }

            _handlers[msgId] = handler;
            Logger.Debug($"注册消息处理器: 主ID={mainId}, 子ID={subId}");
        }

        /// <summary>
        /// 注册消息处理器（泛型版本，自动反序列化）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="mainId">主消息ID</param>
        /// <param name="subId">子消息ID</param>
        /// <param name="handler">处理器</param>
        public void RegisterHandler<T>(byte mainId, byte subId, Action<T> handler) where T : class
        {
            if (handler == null)
            {
                Logger.Error("消息处理器不能为空");
                return;
            }

            RegisterHandler(mainId, subId, (payload) =>
            {
                try
                {
                    T message = ProtobufUtil.Deserialize<T>(payload);
                    handler(message);
                }
                catch (Exception ex)
                {
                    Logger.Error($"反序列化消息失败: 主ID={mainId}, 子ID={subId}, 错误={ex.Message}");
                }
            });
        }

        /// <summary>
        /// 注销消息处理器
        /// </summary>
        /// <param name="mainId">主消息ID</param>
        /// <param name="subId">子消息ID</param>
        public void UnregisterHandler(byte mainId, byte subId)
        {
            ushort msgId = MessagePacket.CombineMessageId(mainId, subId);

            if (_handlers.Remove(msgId))
            {
                Logger.Debug($"注销消息处理器: 主ID={mainId}, 子ID={subId}");
            }
            else
            {
                Logger.Warning($"消息处理器不存在: 主ID={mainId}, 子ID={subId}");
            }
        }

        /// <summary>
        /// 清除所有消息处理器
        /// </summary>
        public void ClearAllHandlers()
        {
            _handlers.Clear();
            Logger.Debug("已清除所有消息处理器");
        }

        /// <summary>
        /// 分发消息（立即在当前线程处理）
        /// </summary>
        /// <param name="mainId">主消息ID</param>
        /// <param name="subId">子消息ID</param>
        /// <param name="payload">消息体数据</param>
        public void DispatchMessage(byte mainId, byte subId, byte[] payload)
        {
            ushort msgId = MessagePacket.CombineMessageId(mainId, subId);

            if (_handlers.TryGetValue(msgId, out var handler))
            {
                try
                {
                    handler(payload);
                }
                catch (Exception ex)
                {
                    Logger.Error($"处理消息时发生异常: 主ID={mainId}, 子ID={subId}, 错误={ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                Logger.Warning($"未注册的消息: 主ID={mainId}, 子ID={subId}");
            }
        }

        /// <summary>
        /// 将消息加入主线程队列（线程安全）
        /// 用于从网络接收线程将消息转发到主线程处理
        /// </summary>
        /// <param name="mainId">主消息ID</param>
        /// <param name="subId">子消息ID</param>
        /// <param name="payload">消息体数据</param>
        public void EnqueueMessage(byte mainId, byte subId, byte[] payload)
        {
            lock (_queueLock)
            {
                _messageQueue.Enqueue(new PendingMessage
                {
                    MainId = mainId,
                    SubId = subId,
                    Payload = payload
                });
            }
        }

        /// <summary>
        /// 处理主线程消息队列
        /// 应该在Unity主线程的Update中调用
        /// </summary>
        public void ProcessMessageQueue()
        {
            // 临时列表，避免长时间持有锁
            List<PendingMessage> messagesToProcess = new List<PendingMessage>();

            lock (_queueLock)
            {
                while (_messageQueue.Count > 0)
                {
                    messagesToProcess.Add(_messageQueue.Dequeue());
                }
            }

            // 处理消息
            foreach (var msg in messagesToProcess)
            {
                DispatchMessage(msg.MainId, msg.SubId, msg.Payload);
            }
        }

        /// <summary>
        /// 获取待处理消息数量
        /// </summary>
        public int GetPendingMessageCount()
        {
            lock (_queueLock)
            {
                return _messageQueue.Count;
            }
        }

        /// <summary>
        /// 检查是否已注册处理器
        /// </summary>
        /// <param name="mainId">主消息ID</param>
        /// <param name="subId">子消息ID</param>
        /// <returns>是否已注册</returns>
        public bool HasHandler(byte mainId, byte subId)
        {
            ushort msgId = MessagePacket.CombineMessageId(mainId, subId);
            return _handlers.ContainsKey(msgId);
        }

        /// <summary>
        /// 获取已注册的处理器数量
        /// </summary>
        public int GetHandlerCount()
        {
            return _handlers.Count;
        }
    }
}
