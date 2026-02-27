# MessageDispatcher 使用指南

## 概述

MessageDispatcher（消息分发器）负责将网络消息路由到对应的处理器。它支持线程安全的消息队列，可以将网络线程接收的消息转发到Unity主线程处理。

## 核心功能

### 1. 消息处理器注册
- `RegisterHandler(mainId, subId, handler)` - 注册原始字节处理器
- `RegisterHandler<T>(mainId, subId, handler)` - 注册泛型处理器（自动反序列化）
- `UnregisterHandler(mainId, subId)` - 注销处理器
- `ClearAllHandlers()` - 清除所有处理器

### 2. 消息分发
- `DispatchMessage(mainId, subId, payload)` - 立即分发消息
- `EnqueueMessage(mainId, subId, payload)` - 加入主线程队列
- `ProcessMessageQueue()` - 处理队列中的消息

### 3. 查询功能
- `HasHandler(mainId, subId)` - 检查是否已注册
- `GetHandlerCount()` - 获取处理器数量
- `GetPendingMessageCount()` - 获取待处理消息数量

## 使用示例

### 基础使用

```csharp
using Framework.Network;

public class NetworkExample
{
    private MessageDispatcher _dispatcher;

    public void Initialize()
    {
        _dispatcher = new MessageDispatcher();

        // 注册消息处理器
        RegisterMessageHandlers();
    }

    private void RegisterMessageHandlers()
    {
        // 方式1：注册泛型处理器（推荐，自动反序列化）
        _dispatcher.RegisterHandler<LoginResponse>(
            MessageModule.Login,
            LoginMessageId.LoginResponse,
            OnLoginResponse
        );

        // 方式2：注册原始字节处理器
        _dispatcher.RegisterHandler(
            MessageModule.System,
            SystemMessageId.Heartbeat,
            OnHeartbeatRaw
        );
    }

    // 泛型处理器
    private void OnLoginResponse(LoginResponse response)
    {
        Debug.Log($"登录结果: {response.ResultCode}");
        if (response.ResultCode == 0)
        {
            Debug.Log($"登录成功，Token: {response.Token}");
        }
        else
        {
            Debug.LogError($"登录失败: {response.Message}");
        }
    }

    // 原始字节处理器
    private void OnHeartbeatRaw(byte[] payload)
    {
        var response = ProtobufUtil.Deserialize<HeartbeatResponse>(payload);
        Debug.Log($"心跳响应，服务器时间: {response.ServerTime}");
    }

    // 在Unity Update中处理消息队列
    private void Update()
    {
        _dispatcher.ProcessMessageQueue();
    }
}
```

### 与TcpClient集成

```csharp
public class NetworkManager
{
    private TcpClient _client;
    private MessageDispatcher _dispatcher;

    public void Connect(string host, int port)
    {
        _client = new TcpClient();
        _dispatcher = new MessageDispatcher();

        // 注册消息处理器
        RegisterHandlers();

        // 监听接收事件
        _client.OnReceive += OnReceiveMessage;

        // 连接服务器
        _client.ConnectAsync(host, port).Forget();
    }

    private void RegisterHandlers()
    {
        // 登录模块
        _dispatcher.RegisterHandler<LoginResponse>(
            MessageModule.Login,
            LoginMessageId.LoginResponse,
            OnLoginResponse
        );

        // 玩家模块
        _dispatcher.RegisterHandler<PlayerInfoResponse>(
            MessageModule.Player,
            PlayerMessageId.GetInfoResponse,
            OnPlayerInfoResponse
        );

        // 聊天模块
        _dispatcher.RegisterHandler<ChatMessage>(
            MessageModule.Chat,
            ChatMessageId.ReceiveMessage,
            OnChatMessage
        );
    }

    private void OnReceiveMessage(byte[] packet)
    {
        // 解析消息包
        if (!MessagePacket.Unpack(packet, out byte mainId, out byte subId, out byte[] payload))
        {
            Logger.Error("消息包解析失败");
            return;
        }

        // 将消息加入主线程队列（线程安全）
        _dispatcher.EnqueueMessage(mainId, subId, payload);
    }

    private void Update()
    {
        // 在主线程处理消息队列
        _dispatcher?.ProcessMessageQueue();
    }

    private void OnLoginResponse(LoginResponse response)
    {
        // 处理登录响应
    }

    private void OnPlayerInfoResponse(PlayerInfoResponse response)
    {
        // 处理玩家信息响应
    }

    private void OnChatMessage(ChatMessage message)
    {
        // 处理聊天消息
    }
}
```

## 线程安全

### 消息队列机制

MessageDispatcher提供了线程安全的消息队列，用于将网络线程接收的消息转发到Unity主线程：

```csharp
// 在网络接收线程中（TcpClient的接收线程）
private void OnReceive(byte[] packet)
{
    if (MessagePacket.Unpack(packet, out byte mainId, out byte subId, out byte[] payload))
    {
        // 加入主线程队列（线程安全）
        _dispatcher.EnqueueMessage(mainId, subId, payload);
    }
}

// 在Unity主线程中（MonoBehaviour的Update）
private void Update()
{
    // 处理队列中的所有消息
    _dispatcher.ProcessMessageQueue();
}
```

### 为什么需要消息队列？

1. **Unity API限制**: Unity的大部分API只能在主线程调用
2. **线程安全**: 避免多线程访问Unity对象导致的问题
3. **性能优化**: 批量处理消息，减少锁竞争

## 消息处理器类型

### 1. 泛型处理器（推荐）

自动反序列化，类型安全：

```csharp
_dispatcher.RegisterHandler<LoginResponse>(
    MessageModule.Login,
    LoginMessageId.LoginResponse,
    (response) => {
        // response已经是LoginResponse类型
        Debug.Log($"登录结果: {response.ResultCode}");
    }
);
```

### 2. 原始字节处理器

手动反序列化，更灵活：

```csharp
_dispatcher.RegisterHandler(
    MessageModule.Login,
    LoginMessageId.LoginResponse,
    (payload) => {
        // 手动反序列化
        var response = ProtobufUtil.Deserialize<LoginResponse>(payload);
        Debug.Log($"登录结果: {response.ResultCode}");
    }
);
```

## 模块化消息处理

### 按模块组织处理器

```csharp
public class LoginMessageHandler
{
    private MessageDispatcher _dispatcher;

    public LoginMessageHandler(MessageDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        _dispatcher.RegisterHandler<LoginResponse>(
            MessageModule.Login,
            LoginMessageId.LoginResponse,
            OnLoginResponse
        );

        _dispatcher.RegisterHandler<LogoutResponse>(
            MessageModule.Login,
            LoginMessageId.LogoutResponse,
            OnLogoutResponse
        );

        _dispatcher.RegisterHandler<RegisterResponse>(
            MessageModule.Login,
            LoginMessageId.RegisterResponse,
            OnRegisterResponse
        );
    }

    private void OnLoginResponse(LoginResponse response)
    {
        // 处理登录响应
    }

    private void OnLogoutResponse(LogoutResponse response)
    {
        // 处理登出响应
    }

    private void OnRegisterResponse(RegisterResponse response)
    {
        // 处理注册响应
    }

    public void Unregister()
    {
        _dispatcher.UnregisterHandler(MessageModule.Login, LoginMessageId.LoginResponse);
        _dispatcher.UnregisterHandler(MessageModule.Login, LoginMessageId.LogoutResponse);
        _dispatcher.UnregisterHandler(MessageModule.Login, LoginMessageId.RegisterResponse);
    }
}
```

### 使用模块处理器

```csharp
public class NetworkManager
{
    private MessageDispatcher _dispatcher;
    private LoginMessageHandler _loginHandler;
    private PlayerMessageHandler _playerHandler;
    private ChatMessageHandler _chatHandler;

    public void Initialize()
    {
        _dispatcher = new MessageDispatcher();

        // 创建各模块处理器
        _loginHandler = new LoginMessageHandler(_dispatcher);
        _playerHandler = new PlayerMessageHandler(_dispatcher);
        _chatHandler = new ChatMessageHandler(_dispatcher);
    }

    public void Shutdown()
    {
        // 注销所有处理器
        _loginHandler?.Unregister();
        _playerHandler?.Unregister();
        _chatHandler?.Unregister();

        _dispatcher?.ClearAllHandlers();
    }
}
```

## 错误处理

### 异常捕获

MessageDispatcher会自动捕获处理器中的异常，防止单个消息处理失败影响其他消息：

```csharp
private void OnLoginResponse(LoginResponse response)
{
    // 即使这里抛出异常，也不会影响其他消息的处理
    throw new Exception("测试异常");
}
```

异常会被记录到日志：
```
[Error] 处理消息时发生异常: 主ID=2, 子ID=2, 错误=测试异常
```

### 反序列化失败

使用泛型处理器时，如果反序列化失败，会自动记录错误：

```csharp
_dispatcher.RegisterHandler<LoginResponse>(
    MessageModule.Login,
    LoginMessageId.LoginResponse,
    OnLoginResponse
);

// 如果payload无法反序列化为LoginResponse，会记录错误日志
// [Error] 反序列化消息失败: 主ID=2, 子ID=2, 错误=...
```

### 未注册的消息

收到未注册的消息时，会记录警告：

```
[Warning] 未注册的消息: 主ID=5, 子ID=10
```

## 性能优化

### 1. 批量处理消息

`ProcessMessageQueue()` 会一次性处理队列中的所有消息，减少锁竞争：

```csharp
private void Update()
{
    // 一次性处理所有待处理消息
    _dispatcher.ProcessMessageQueue();
}
```

### 2. 避免频繁注册/注销

在初始化时注册所有处理器，避免运行时频繁注册/注销：

```csharp
// 好的做法：初始化时注册
public void Initialize()
{
    RegisterAllHandlers();
}

// 不好的做法：每次使用时注册
public void SendLoginRequest()
{
    _dispatcher.RegisterHandler<LoginResponse>(...);  // 避免这样做
    // ...
}
```

### 3. 监控队列大小

如果队列积压过多消息，可能需要优化处理逻辑：

```csharp
private void Update()
{
    int pendingCount = _dispatcher.GetPendingMessageCount();
    if (pendingCount > 100)
    {
        Logger.Warning($"消息队列积压: {pendingCount}条消息");
    }

    _dispatcher.ProcessMessageQueue();
}
```

## 调试技巧

### 1. 查看已注册的处理器

```csharp
Debug.Log($"已注册处理器数量: {_dispatcher.GetHandlerCount()}");
```

### 2. 检查处理器是否存在

```csharp
if (!_dispatcher.HasHandler(MessageModule.Login, LoginMessageId.LoginResponse))
{
    Debug.LogWarning("登录响应处理器未注册");
}
```

### 3. 监控消息队列

```csharp
private void Update()
{
    int pending = _dispatcher.GetPendingMessageCount();
    if (pending > 0)
    {
        Debug.Log($"待处理消息: {pending}条");
    }

    _dispatcher.ProcessMessageQueue();
}
```

## 最佳实践

### 1. 统一注册时机

在游戏初始化时注册所有消息处理器：

```csharp
public class GameEntry : MonoBehaviour
{
    private void Start()
    {
        NetworkManager.Instance.Initialize();
        // 所有消息处理器在这里注册
    }
}
```

### 2. 模块化组织

按功能模块组织消息处理器，便于维护：

```
NetworkHandlers/
├── LoginHandler.cs
├── PlayerHandler.cs
├── BattleHandler.cs
├── ChatHandler.cs
└── ShopHandler.cs
```

### 3. 使用泛型处理器

优先使用泛型处理器，类型安全且代码简洁：

```csharp
// 推荐
_dispatcher.RegisterHandler<LoginResponse>(mainId, subId, OnLoginResponse);

// 不推荐（除非有特殊需求）
_dispatcher.RegisterHandler(mainId, subId, (payload) => {
    var response = ProtobufUtil.Deserialize<LoginResponse>(payload);
    OnLoginResponse(response);
});
```

### 4. 及时清理

在不需要时注销处理器，避免内存泄漏：

```csharp
private void OnDestroy()
{
    _dispatcher?.ClearAllHandlers();
}
```

### 5. 主线程处理

始终在Unity主线程的Update中调用ProcessMessageQueue：

```csharp
private void Update()
{
    _dispatcher?.ProcessMessageQueue();
}
```

## 完整示例

```csharp
using UnityEngine;
using Framework.Network;
using Cysharp.Threading.Tasks;

public class NetworkManager : MonoBehaviour
{
    private TcpClient _client;
    private MessageDispatcher _dispatcher;

    private void Start()
    {
        Initialize();
        ConnectToServer().Forget();
    }

    private void Initialize()
    {
        _dispatcher = new MessageDispatcher();
        RegisterMessageHandlers();
    }

    private void RegisterMessageHandlers()
    {
        // 系统消息
        _dispatcher.RegisterHandler<HeartbeatResponse>(
            MessageModule.System,
            SystemMessageId.HeartbeatResponse,
            OnHeartbeat
        );

        // 登录消息
        _dispatcher.RegisterHandler<LoginResponse>(
            MessageModule.Login,
            LoginMessageId.LoginResponse,
            OnLoginResponse
        );

        // 玩家消息
        _dispatcher.RegisterHandler<PlayerInfoResponse>(
            MessageModule.Player,
            PlayerMessageId.GetInfoResponse,
            OnPlayerInfo
        );
    }

    private async UniTask ConnectToServer()
    {
        _client = new TcpClient();
        _client.OnReceive += OnReceiveMessage;

        try
        {
            await _client.ConnectAsync("127.0.0.1", 8888);
            Debug.Log("连接成功");
        }
        catch (Exception ex)
        {
            Debug.LogError($"连接失败: {ex.Message}");
        }
    }

    private void OnReceiveMessage(byte[] packet)
    {
        if (MessagePacket.Unpack(packet, out byte mainId, out byte subId, out byte[] payload))
        {
            // 加入主线程队列
            _dispatcher.EnqueueMessage(mainId, subId, payload);
        }
    }

    private void Update()
    {
        // 处理消息队列
        _dispatcher?.ProcessMessageQueue();
    }

    private void OnHeartbeat(HeartbeatResponse response)
    {
        Debug.Log($"心跳: {response.ServerTime}");
    }

    private void OnLoginResponse(LoginResponse response)
    {
        if (response.ResultCode == 0)
        {
            Debug.Log("登录成功");
        }
        else
        {
            Debug.LogError($"登录失败: {response.Message}");
        }
    }

    private void OnPlayerInfo(PlayerInfoResponse response)
    {
        Debug.Log($"玩家信息: {response.PlayerName}");
    }

    private void OnDestroy()
    {
        _client?.Disconnect();
        _dispatcher?.ClearAllHandlers();
    }
}
```

## 总结

MessageDispatcher提供了：
- ✓ 灵活的消息路由机制
- ✓ 线程安全的消息队列
- ✓ 自动异常捕获
- ✓ 泛型处理器支持
- ✓ 模块化消息处理

配合TcpClient和MessagePacket，构成了完整的网络消息处理流程。
