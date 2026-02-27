using System;
using Cysharp.Threading.Tasks;
using Framework.Core;
using Framework.Network;

namespace Framework
{
    /// <summary>
    /// 网络管理器
    /// 整合TcpClient和MessageDispatcher，提供统一的网络接口
    /// 支持心跳机制和自动重连
    /// </summary>
    public class NetworkManager : FrameworkComponent
    {
        private TcpClient _client;
        private MessageDispatcher _dispatcher;

        // 心跳相关
        private float _heartbeatInterval = 30f; // 心跳间隔（秒）
        private float _heartbeatTimer = 0f;
        private bool _enableHeartbeat = true;
        private byte _heartbeatMainId = MessageModule.System;
        private byte _heartbeatSubId = 1;

        // 重连相关
        private bool _enableAutoReconnect = true;
        private int _maxReconnectAttempts = 5;
        private int _currentReconnectAttempt = 0;
        private float[] _reconnectIntervals = { 1f, 2f, 5f, 10f, 30f }; // 重连间隔（秒）
        private bool _isReconnecting = false;
        private string _lastHost;
        private int _lastPort;

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => _client != null && _client.IsConnected;

        /// <summary>
        /// 是否正在重连
        /// </summary>
        public bool IsReconnecting => _isReconnecting;

        /// <summary>
        /// 连接成功事件
        /// </summary>
        public event Action OnConnected;

        /// <summary>
        /// 断开连接事件
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// 初始化
        /// </summary>
        public override void OnInit()
        {
            _dispatcher = new MessageDispatcher();
            Logger.Log("NetworkManager初始化完成");
        }

        /// <summary>
        /// 更新
        /// </summary>
        public override void OnUpdate(float deltaTime)
        {
            // 处理消息队列
            _dispatcher?.ProcessMessageQueue();

            // 心跳机制
            if (IsConnected && _enableHeartbeat)
            {
                _heartbeatTimer += deltaTime;
                if (_heartbeatTimer >= _heartbeatInterval)
                {
                    _heartbeatTimer = 0f;
                    SendHeartbeat();
                }
            }
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="host">服务器地址</param>
        /// <param name="port">服务器端口</param>
        public async UniTask ConnectAsync(string host, int port)
        {
            if (IsConnected)
            {
                Logger.Warning("已经连接到服务器");
                return;
            }

            _lastHost = host;
            _lastPort = port;
            _currentReconnectAttempt = 0;

            await ConnectInternalAsync(host, port);
        }

        /// <summary>
        /// 内部连接方法
        /// </summary>
        private async UniTask ConnectInternalAsync(string host, int port)
        {
            try
            {
                // 创建TCP客户端
                if (_client == null)
                {
                    _client = new TcpClient();
                    _client.OnConnected += OnClientConnected;
                    _client.OnDisconnected += OnClientDisconnected;
                    _client.OnReceive += OnClientReceive;
                    _client.OnError += OnClientError;
                }

                // 连接服务器
                await _client.ConnectAsync(host, port);
            }
            catch (Exception ex)
            {
                Logger.Error($"连接服务器失败: {ex.Message}");
                OnError?.Invoke($"连接失败: {ex.Message}");

                // 尝试重连
                if (_enableAutoReconnect && !_isReconnecting)
                {
                    await TryReconnectAsync();
                }
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            _enableAutoReconnect = false; // 主动断开时禁用自动重连
            _isReconnecting = false;
            _currentReconnectAttempt = 0;

            if (_client != null)
            {
                _client.Disconnect();
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息对象</param>
        public void SendMessage<T>(T message) where T : class, IMessage
        {
            if (!IsConnected)
            {
                Logger.Error("未连接到服务器，无法发送消息");
                return;
            }

            try
            {
                // 序列化消息
                byte[] payload = ProtobufUtil.Serialize(message);

                // 打包消息
                byte[] packet = MessagePacket.Pack(message, payload);

                // 发送
                _client.Send(packet);

                Logger.Debug($"发送消息: 主ID={message.GetMainId()}, 子ID={message.GetSubId()}");
            }
            catch (Exception ex)
            {
                Logger.Error($"发送消息失败: {ex.Message}");
                OnError?.Invoke($"发送失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="mainId">主消息ID</param>
        /// <param name="subId">子消息ID</param>
        /// <param name="handler">处理器</param>
        public void RegisterHandler<T>(byte mainId, byte subId, Action<T> handler) where T : class
        {
            _dispatcher.RegisterHandler(mainId, subId, handler);
        }

        /// <summary>
        /// 注销消息处理器
        /// </summary>
        /// <param name="mainId">主消息ID</param>
        /// <param name="subId">子消息ID</param>
        public void UnregisterHandler(byte mainId, byte subId)
        {
            _dispatcher.UnregisterHandler(mainId, subId);
        }

        /// <summary>
        /// 设置心跳间隔
        /// </summary>
        /// <param name="interval">间隔时间（秒）</param>
        public void SetHeartbeatInterval(float interval)
        {
            if (interval <= 0)
            {
                Logger.Warning("心跳间隔必须大于0");
                return;
            }

            _heartbeatInterval = interval;
            Logger.Log($"心跳间隔设置为: {interval}秒");
        }

        /// <summary>
        /// 设置心跳消息ID
        /// </summary>
        /// <param name="mainId">主消息ID</param>
        /// <param name="subId">子消息ID</param>
        public void SetHeartbeatMessageId(byte mainId, byte subId)
        {
            _heartbeatMainId = mainId;
            _heartbeatSubId = subId;
        }

        /// <summary>
        /// 启用/禁用心跳
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void EnableHeartbeat(bool enable)
        {
            _enableHeartbeat = enable;
            Logger.Log($"心跳机制: {(enable ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 启用/禁用自动重连
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void EnableAutoReconnect(bool enable)
        {
            _enableAutoReconnect = enable;
            Logger.Log($"自动重连: {(enable ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 设置最大重连次数
        /// </summary>
        /// <param name="maxAttempts">最大重连次数</param>
        public void SetMaxReconnectAttempts(int maxAttempts)
        {
            if (maxAttempts < 0)
            {
                Logger.Warning("最大重连次数不能小于0");
                return;
            }

            _maxReconnectAttempts = maxAttempts;
            Logger.Log($"最大重连次数设置为: {maxAttempts}");
        }

        /// <summary>
        /// 设置重连间隔序列
        /// </summary>
        /// <param name="intervals">重连间隔数组（秒）</param>
        public void SetReconnectIntervals(float[] intervals)
        {
            if (intervals == null || intervals.Length == 0)
            {
                Logger.Warning("重连间隔数组不能为空");
                return;
            }

            _reconnectIntervals = intervals;
            Logger.Log($"重连间隔序列已更新，共{intervals.Length}个间隔");
        }

        /// <summary>
        /// 客户端连接成功回调
        /// </summary>
        private void OnClientConnected()
        {
            _currentReconnectAttempt = 0;
            _isReconnecting = false;
            _heartbeatTimer = 0f;

            Logger.Log("网络连接成功");
            OnConnected?.Invoke();
        }

        /// <summary>
        /// 客户端断开连接回调
        /// </summary>
        private void OnClientDisconnected()
        {
            Logger.Log("网络连接断开");
            OnDisconnected?.Invoke();

            // 尝试重连
            if (_enableAutoReconnect && !_isReconnecting)
            {
                TryReconnectAsync().Forget();
            }
        }

        /// <summary>
        /// 客户端接收消息回调
        /// </summary>
        private void OnClientReceive(byte[] packet)
        {
            // 解析消息包
            if (MessagePacket.Unpack(packet, out byte mainId, out byte subId, out byte[] payload))
            {
                // 加入主线程消息队列
                _dispatcher.EnqueueMessage(mainId, subId, payload);
            }
            else
            {
                Logger.Error("消息包解析失败");
            }
        }

        /// <summary>
        /// 客户端错误回调
        /// </summary>
        private void OnClientError(string error)
        {
            Logger.Error($"网络错误: {error}");
            OnError?.Invoke(error);
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        private void SendHeartbeat()
        {
            try
            {
                // 创建心跳请求（空消息）
                byte[] packet = MessagePacket.Pack(_heartbeatMainId, _heartbeatSubId, null);
                _client.Send(packet);

                Logger.Debug("发送心跳");
            }
            catch (Exception ex)
            {
                Logger.Error($"发送心跳失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试重连
        /// </summary>
        private async UniTask TryReconnectAsync()
        {
            if (_isReconnecting)
            {
                return;
            }

            _isReconnecting = true;

            while (_currentReconnectAttempt < _maxReconnectAttempts)
            {
                _currentReconnectAttempt++;

                // 获取重连间隔
                int intervalIndex = Math.Min(_currentReconnectAttempt - 1, _reconnectIntervals.Length - 1);
                float interval = _reconnectIntervals[intervalIndex];

                Logger.Log($"尝试重连 ({_currentReconnectAttempt}/{_maxReconnectAttempts})，等待{interval}秒...");

                // 等待重连间隔
                await UniTask.Delay(TimeSpan.FromSeconds(interval));

                // 尝试连接
                try
                {
                    await _client.ConnectAsync(_lastHost, _lastPort);

                    // 连接成功
                    Logger.Log("重连成功");
                    _isReconnecting = false;
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"重连失败: {ex.Message}");
                }
            }

            // 重连失败
            _isReconnecting = false;
            Logger.Error($"重连失败，已达到最大重连次数({_maxReconnectAttempts})");
            OnError?.Invoke("重连失败");
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public override void OnShutdown()
        {
            Disconnect();
            _dispatcher?.ClearAllHandlers();
            _client = null;
            _dispatcher = null;

            Logger.Log("NetworkManager已关闭");
        }
    }
}
