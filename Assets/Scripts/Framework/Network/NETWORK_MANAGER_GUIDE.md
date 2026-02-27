# NetworkManager 使用指南

## 概述

`NetworkManager` 是Unity客户端框架的网络管理器，整合了 `TcpClient` 和 `MessageDispatcher`，提供统一的网络接口。它支持心跳机制、自动重连、消息发送和接收等功能。

## 主要特性

- **统一的网络接口**：简化网络操作，提供易用的API
- **心跳机制**：自动发送心跳包，保持连接活跃
- **自动重连**：连接断开时自动尝试重连（指数退避策略）
- **消息分发**：自动将网络消息分发到主线程处理
- **事件通知**：提供连接、断开、错误等事件回调

## 快速开始

### 1. 获取NetworkManager实例

```csharp
using Framework;
using Framework.Core;

// 通过GameEntry获取NetworkManager
var networkManager = GameEntry.GetComponent<NetworkManager>();
```

### 2. 连接服务器

```csharp
using Cysharp.Threading.Tasks;

public async UniTaskVoid ConnectToServer()
{
    var networkManager = GameEntry.GetComponent<NetworkManager>();
    
    // 注册事件
    networkManager.OnConnected += OnConnected;
    networkManager.OnDisconnected += OnDisconnected;
    networkManager.OnError += OnError;
    
    // 连接服务器
    await networkManager.ConnectAsync("127.0.0.1", 8888);
}

private void OnConnected()
{
    Debug.Log("连接成功！");
}

private void OnDisconnected()
{
    Debug.Log("连接断开！");
}

private void OnError(string error)
{
    Debug.LogError($"网络错误: {error}");
}
```

### 3. 注册消息处理器

```csharp
using Framework.Network;

public void RegisterHandlers()
{
    var networkManager = GameEntry.GetComponent<NetworkManager>();
    
    // 注册登录响应处理器
    networkManager.RegisterHandler<LoginResponse>(
        MessageModule.Login,  // 主ID
        1,                    // 子ID
        OnLoginResponse       // 处理器
    );
}

private void OnLoginResponse(LoginResponse response)
{
    if (response.Success)
    {
        Debug.Log($"登录成功！用户ID: {response.UserId}");
    }
    else
    {
        Debug.LogError($"登录失败: {response.ErrorMessage}");
    }
}
```

### 4. 发送消息

```csharp
public void SendLoginRequest(string username, string password)
{
    var networkManager = GameEntry.GetComponent<NetworkManager>();
    
    // 创建登录请求消息
    var request = new LoginRequest
    {
        Username = username,
        Password = password
    };
    
    // 发送消息
    networkManager.SendMessage(request);
}
```

### 5. 断开连接

```csharp
public void DisconnectFromServer()
{
    var networkManager = GameEntry.GetComponent<NetworkManager>();
    networkManager.Disconnect();
}
```

## 心跳机制

NetworkManager 内置了心跳机制，默认每30秒发送一次心跳包。

### 配置心跳

```csharp
var networkManager = GameEntry.GetComponent<NetworkManager>();

// 设置心跳间隔（秒）
networkManager.SetHeartbeatInterval(30f);

// 设置心跳消息ID
networkManager.SetHeartbeatMessageId(
    MessageModule.System,  // 主ID
    1                      // 子ID
);

// 启用/禁用心跳
networkManager.EnableHeartbeat(true);
```

### 心跳消息格式

心跳消息使用空payload，只包含消息头：
- 主ID：默认为 `MessageModule.System` (0)
- 子ID：默认为 1
- Payload：null

## 自动重连机制

NetworkManager 支持自动重连，当连接断开时会自动尝试重连。

### 重连策略

默认使用指数退避策略：
- 第1次重连：等待1秒
- 第2次重连：等待2秒
- 第3次重连：等待5秒
- 第4次重连：等待10秒
- 第5次重连：等待30秒

### 配置重连

```csharp
var networkManager = GameEntry.GetComponent<NetworkManager>();

// 启用/禁用自动重连
networkManager.EnableAutoReconnect(true);

// 设置最大重连次数
networkManager.SetMaxReconnectAttempts(5);

// 自定义重连间隔序列（秒）
float[] intervals = { 1f, 2f, 5f, 10f, 30f };
networkManager.SetReconnectIntervals(intervals);
```

### 检查重连状态

```csharp
var networkManager = GameEntry.GetComponent<NetworkManager>();

// 检查是否正在重连
if (networkManager.IsReconnecting)
{
    Debug.Log("正在尝试重连...");
}
```

## 完整示例

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using Framework;
using Framework.Core;
using Framework.Network;

public class NetworkExample : MonoBehaviour
{
    private NetworkManager _networkManager;

    private async void Start()
    {
        // 获取NetworkManager
        _networkManager = GameEntry.GetComponent<NetworkManager>();
        
        // 配置网络管理器
        ConfigureNetworkManager();
        
        // 注册消息处理器
        RegisterMessageHandlers();
        
        // 连接服务器
        await ConnectToServer();
    }

    private void ConfigureNetworkManager()
    {
        // 注册事件
        _networkManager.OnConnected += OnConnected;
        _networkManager.OnDisconnected += OnDisconnected;
        _networkManager.OnError += OnError;
        
        // 配置心跳
        _networkManager.SetHeartbeatInterval(30f);
        _networkManager.EnableHeartbeat(true);
        
        // 配置重连
        _networkManager.EnableAutoReconnect(true);
        _networkManager.SetMaxReconnectAttempts(5);
    }

    private void RegisterMessageHandlers()
    {
        // 注册登录响应
        _networkManager.RegisterHandler<LoginResponse>(
            MessageModule.Login,
            1,
            OnLoginResponse
        );
        
        // 注册其他消息处理器...
    }

    private async UniTask ConnectToServer()
    {
        try
        {
            await _networkManager.ConnectAsync("127.0.0.1", 8888);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"连接失败: {ex.Message}");
        }
    }

    private void OnConnected()
    {
        Debug.Log("连接成功！");
        
        // 发送登录请求
        SendLoginRequest("player1", "password123");
    }

    private void OnDisconnected()
    {
        Debug.Log("连接断开！");
    }

    private void OnError(string error)
    {
        Debug.LogError($"网络错误: {error}");
    }

    private void SendLoginRequest(string username, string password)
    {
        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };
        
        _networkManager.SendMessage(request);
    }

    private void OnLoginResponse(LoginResponse response)
    {
        if (response.Success)
        {
            Debug.Log($"登录成功！用户ID: {response.UserId}");
        }
        else
        {
            Debug.LogError($"登录失败: {response.ErrorMessage}");
        }
    }

    private void OnDestroy()
    {
        // 注销事件
        if (_networkManager != null)
        {
            _networkManager.OnConnected -= OnConnected;
            _networkManager.OnDisconnected -= OnDisconnected;
            _networkManager.OnError -= OnError;
            
            // 断开连接
            _networkManager.Disconnect();
        }
    }
}
```

## 注意事项

1. **消息类型约束**：发送的消息必须实现 `IMessage` 接口并且是引用类型（class）
2. **主线程处理**：所有消息处理器都在Unity主线程中执行，可以安全地访问Unity API
3. **异常处理**：消息处理器中的异常会被自动捕获，不会影响其他消息的处理
4. **主动断开**：调用 `Disconnect()` 会禁用自动重连，避免不必要的重连尝试
5. **事件注销**：在对象销毁时记得注销事件监听，避免内存泄漏

## 相关文档

- [TcpClient 使用指南](TCP_CLIENT_GUIDE.md)
- [MessageDispatcher 使用指南](MESSAGE_DISPATCHER_GUIDE.md)
- [Protobuf 设置指南](PROTOBUF_SETUP.md)
- [消息ID设计文档](MESSAGE_ID_DESIGN.md)
