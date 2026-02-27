using System;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Framework.Network
{
    /// <summary>
    /// TCP客户端
    /// 负责底层TCP连接和数据收发
    /// </summary>
    public class TcpClient
    {
        private Socket _socket;
        private Thread _receiveThread;
        private bool _isConnected;
        private readonly object _sendLock = new object();
        private byte[] _receiveBuffer = new byte[8192];
        private byte[] _messageBuffer = new byte[65536]; // 消息缓冲区
        private int _messageBufferOffset = 0;

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => _isConnected && _socket != null && _socket.Connected;

        /// <summary>
        /// 连接成功事件
        /// </summary>
        public event Action OnConnected;

        /// <summary>
        /// 断开连接事件
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 接收到数据事件
        /// </summary>
        public event Action<byte[]> OnReceive;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// 异步连接到服务器
        /// </summary>
        /// <param name="host">服务器地址</param>
        /// <param name="port">服务器端口</param>
        public async UniTask ConnectAsync(string host, int port)
        {
            if (IsConnected)
            {
                Logger.Warning("已经连接到服务器，无需重复连接");
                return;
            }

            try
            {
                // 创建Socket
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.NoDelay = true; // 禁用Nagle算法，减少延迟

                // 异步连接
                await _socket.ConnectAsync(host, port);

                _isConnected = true;
                Logger.Log($"成功连接到服务器 {host}:{port}");

                // 启动接收线程
                StartReceiveThread();

                // 触发连接成功事件
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                _isConnected = false;
                Logger.Error($"连接服务器失败: {ex.Message}");
                OnError?.Invoke($"连接失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 同步连接到服务器（阻塞）
        /// </summary>
        /// <param name="host">服务器地址</param>
        /// <param name="port">服务器端口</param>
        public void Connect(string host, int port)
        {
            if (IsConnected)
            {
                Logger.Warning("已经连接到服务器，无需重复连接");
                return;
            }

            try
            {
                // 创建Socket
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.NoDelay = true;

                // 同步连接
                _socket.Connect(host, port);

                _isConnected = true;
                Logger.Log($"成功连接到服务器 {host}:{port}");

                // 启动接收线程
                StartReceiveThread();

                // 触发连接成功事件
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                _isConnected = false;
                Logger.Error($"连接服务器失败: {ex.Message}");
                OnError?.Invoke($"连接失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            if (!IsConnected)
            {
                return;
            }

            try
            {
                _isConnected = false;

                // 关闭Socket
                if (_socket != null)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                    _socket = null;
                }

                // 等待接收线程结束
                if (_receiveThread != null && _receiveThread.IsAlive)
                {
                    _receiveThread.Join(1000); // 最多等待1秒
                    _receiveThread = null;
                }

                // 清空缓冲区
                _messageBufferOffset = 0;

                Logger.Log("已断开与服务器的连接");

                // 触发断开连接事件
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error($"断开连接时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送数据（线程安全）
        /// </summary>
        /// <param name="data">要发送的数据</param>
        public void Send(byte[] data)
        {
            if (!IsConnected)
            {
                Logger.Error("未连接到服务器，无法发送数据");
                OnError?.Invoke("未连接到服务器");
                return;
            }

            if (data == null || data.Length == 0)
            {
                Logger.Warning("发送数据为空");
                return;
            }

            try
            {
                lock (_sendLock)
                {
                    _socket.Send(data);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"发送数据失败: {ex.Message}");
                OnError?.Invoke($"发送失败: {ex.Message}");

                // 发送失败，断开连接
                Disconnect();
            }
        }

        /// <summary>
        /// 启动接收线程
        /// </summary>
        private void StartReceiveThread()
        {
            _receiveThread = new Thread(ReceiveThreadFunc)
            {
                IsBackground = true,
                Name = "TcpClient_Receive"
            };
            _receiveThread.Start();
        }

        /// <summary>
        /// 接收线程函数
        /// </summary>
        private void ReceiveThreadFunc()
        {
            try
            {
                while (_isConnected && _socket != null)
                {
                    // 接收数据
                    int bytesRead = _socket.Receive(_receiveBuffer);

                    if (bytesRead <= 0)
                    {
                        // 连接已断开
                        Logger.Warning("服务器断开连接");
                        break;
                    }

                    // 将接收到的数据添加到消息缓冲区
                    Array.Copy(_receiveBuffer, 0, _messageBuffer, _messageBufferOffset, bytesRead);
                    _messageBufferOffset += bytesRead;

                    // 处理消息缓冲区中的完整消息
                    ProcessMessageBuffer();
                }
            }
            catch (SocketException ex)
            {
                if (_isConnected)
                {
                    Logger.Error($"接收数据时发生Socket异常: {ex.Message}");
                    OnError?.Invoke($"接收失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"接收数据时发生异常: {ex.Message}");
                OnError?.Invoke($"接收失败: {ex.Message}");
            }
            finally
            {
                // 接收线程结束，断开连接
                if (_isConnected)
                {
                    Disconnect();
                }
            }
        }

        /// <summary>
        /// 处理消息缓冲区
        /// 消息格式：Length(4字节) + MsgId(2字节) + Reserved(2字节) + Payload(N字节)
        /// </summary>
        private void ProcessMessageBuffer()
        {
            while (_messageBufferOffset >= 8) // 至少需要8字节的消息头
            {
                // 读取消息长度（前4字节）
                int messageLength = BitConverter.ToInt32(_messageBuffer, 0);

                // 验证消息长度
                if (messageLength < 8 || messageLength > 65536)
                {
                    Logger.Error($"无效的消息长度: {messageLength}");
                    // 清空缓冲区
                    _messageBufferOffset = 0;
                    break;
                }

                // 检查是否接收到完整消息
                if (_messageBufferOffset < messageLength)
                {
                    // 消息不完整，等待更多数据
                    break;
                }

                // 提取完整消息
                byte[] message = new byte[messageLength];
                Array.Copy(_messageBuffer, 0, message, 0, messageLength);

                // 移除已处理的消息
                int remainingBytes = _messageBufferOffset - messageLength;
                if (remainingBytes > 0)
                {
                    Array.Copy(_messageBuffer, messageLength, _messageBuffer, 0, remainingBytes);
                }
                _messageBufferOffset = remainingBytes;

                // 触发接收事件
                try
                {
                    OnReceive?.Invoke(message);
                }
                catch (Exception ex)
                {
                    Logger.Error($"处理接收消息时发生异常: {ex.Message}");
                }
            }
        }
    }
}
