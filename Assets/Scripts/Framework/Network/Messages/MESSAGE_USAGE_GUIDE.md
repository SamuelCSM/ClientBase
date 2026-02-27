# 网络消息使用指南

## 概述

本目录包含框架提供的示例Protobuf消息定义。这些消息展示了如何定义和使用网络消息。

## 消息列表

### 登录消息 (LoginMessages.cs)

#### LoginRequest - 登录请求
客户端发送给服务器的登录请求。

**字段说明：**
- `Username` (string): 用户名
- `Password` (string): 密码（建议客户端加密后传输）
- `ClientVersion` (string): 客户端版本号
- `DeviceId` (string): 设备唯一标识
- `Platform` (int): 平台类型（0=Windows, 1=Android, 2=iOS）

**消息ID：**
- 主ID: `MessageModule.Login` (1)
- 子ID: 1

**使用示例：**
```csharp
using Framework;
using Framework.Core;
using Framework.Network.Messages;

var networkManager = GameEntry.GetComponent<NetworkManager>();

var request = new LoginRequest
{
    Username = "player123",
    Password = "encrypted_password",
    ClientVersion = "1.0.0",
    DeviceId = SystemInfo.deviceUniqueIdentifier,
    Platform = 0 // Windows
};

networkManager.SendMessage(request);
```

#### LoginResponse - 登录响应
服务器返回的登录结果。

**字段说明：**
- `ResultCode` (int): 结果码（0=成功，其他值表示失败）
- `ErrorMessage` (string): 错误消息（失败时提供）
- `UserId` (long): 用户ID
- `SessionToken` (string): 会话令牌（用于后续请求验证）
- `Nickname` (string): 用户昵称
- `Level` (int): 用户等级
- `ServerTime` (long): 服务器时间戳

**消息ID：**
- 主ID: `MessageModule.Login` (1)
- 子ID: 2

**使用示例：**
```csharp
using Framework;
using Framework.Core;
using Framework.Network.Messages;

var networkManager = GameEntry.GetComponent<NetworkManager>();

// 注册登录响应处理器
networkManager.RegisterHandler<LoginResponse>(
    MessageModule.Login,
    2,
    OnLoginResponse
);

private void OnLoginResponse(LoginResponse response)
{
    if (response.ResultCode == 0)
    {
        Debug.Log($"登录成功！用户ID: {response.UserId}, 昵称: {response.Nickname}");
        // 保存会话令牌
        PlayerPrefs.SetString("SessionToken", response.SessionToken);
    }
    else
    {
        Debug.LogError($"登录失败: {response.ErrorMessage}");
    }
}
```

### 心跳消息 (HeartbeatMessages.cs)

#### HeartbeatRequest - 心跳请求
客户端定期发送的心跳包，用于保持连接活跃。

**字段说明：**
- `ClientTime` (long): 客户端时间戳（毫秒）
- `SequenceId` (int): 序列号（用于匹配请求和响应）

**消息ID：**
- 主ID: `MessageModule.System` (0)
- 子ID: 1

**使用示例：**
```csharp
using Framework;
using Framework.Core;
using Framework.Network.Messages;

var networkManager = GameEntry.GetComponent<NetworkManager>();

// 手动发送心跳（通常由NetworkManager自动处理）
var request = new HeartbeatRequest
{
    ClientTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    SequenceId = heartbeatSequence++
};

networkManager.SendMessage(request);
```

#### HeartbeatResponse - 心跳响应
服务器对心跳请求的响应。

**字段说明：**
- `ServerTime` (long): 服务器时间戳（毫秒）
- `SequenceId` (int): 序列号（与请求匹配）
- `ServerStatus` (int): 服务器状态（0=正常，1=维护中，2=即将关闭）

**消息ID：**
- 主ID: `MessageModule.System` (0)
- 子ID: 2

**使用示例：**
```csharp
using Framework;
using Framework.Core;
using Framework.Network.Messages;

var networkManager = GameEntry.GetComponent<NetworkManager>();

// 注册心跳响应处理器
networkManager.RegisterHandler<HeartbeatResponse>(
    MessageModule.System,
    2,
    OnHeartbeatResponse
);

private void OnHeartbeatResponse(HeartbeatResponse response)
{
    // 计算网络延迟
    long clientTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    long latency = clientTime - response.ServerTime;
    
    Debug.Log($"心跳响应 - 延迟: {latency}ms, 服务器状态: {response.ServerStatus}");
    
    // 检查服务器状态
    if (response.ServerStatus == 1)
    {
        Debug.LogWarning("服务器维护中");
    }
    else if (response.ServerStatus == 2)
    {
        Debug.LogWarning("服务器即将关闭");
    }
}
```

## 自定义消息

### 创建新消息的步骤

1. **定义消息类**

```csharp
using ProtoBuf;
using Framework.Network;

namespace Framework.Network.Messages
{
    [ProtoContract]
    public class YourRequest : IMessage
    {
        [ProtoMember(1)]
        public int YourField { get; set; }
        
        public byte GetMainId()
        {
            return MessageModule.YourModule; // 选择合适的模块ID
        }
        
        public byte GetSubId()
        {
            return 1; // 选择唯一的子ID
        }
    }
}
```

2. **添加ProtoContract和ProtoMember特性**
   - `[ProtoContract]`: 标记类为Protobuf消息
   - `[ProtoMember(n)]`: 标记字段，n为字段编号（从1开始）

3. **实现IMessage接口**
   - `GetMainId()`: 返回主消息ID（模块ID）
   - `GetSubId()`: 返回子消息ID（消息类型ID）

4. **注册消息处理器**

```csharp
networkManager.RegisterHandler<YourResponse>(
    MessageModule.YourModule,
    2,
    OnYourResponse
);
```

5. **发送消息**

```csharp
var request = new YourRequest { YourField = 123 };
networkManager.SendMessage(request);
```

## 消息ID规划

### 主ID（模块ID）

主ID用于区分不同的功能模块：

- `0` (MessageModule.System): 系统消息（心跳、错误等）
- `1` (MessageModule.Login): 登录模块
- `2` (MessageModule.Player): 玩家模块
- `3` (MessageModule.Battle): 战斗模块
- `4` (MessageModule.Social): 社交模块
- `5` (MessageModule.Shop): 商城模块
- `6-255`: 自定义模块

### 子ID（消息类型ID）

子ID用于区分同一模块内的不同消息类型：

- 通常使用奇数作为请求，偶数作为响应
- 例如：1=请求，2=响应，3=通知

**示例规划：**
```
登录模块 (MainId=1):
  - 1: LoginRequest
  - 2: LoginResponse
  - 3: LogoutRequest
  - 4: LogoutResponse
  - 5: KickNotify (服务器踢人通知)

玩家模块 (MainId=2):
  - 1: GetPlayerInfoRequest
  - 2: GetPlayerInfoResponse
  - 3: UpdatePlayerInfoRequest
  - 4: UpdatePlayerInfoResponse
  - 5: PlayerLevelUpNotify
```

## 最佳实践

### 1. 字段编号管理
- ProtoMember编号从1开始
- 不要重复使用已删除字段的编号
- 保留1-15用于常用字段（编码效率更高）

### 2. 向后兼容
- 不要修改已有字段的编号
- 不要修改已有字段的类型
- 新增字段使用新的编号
- 使用可选字段而不是必需字段

### 3. 消息设计
- 保持消息简洁，只包含必要字段
- 大消息考虑分页或分批传输
- 敏感数据（如密码）在客户端加密后传输

### 4. 错误处理
- 统一使用ResultCode表示操作结果
- 0表示成功，非0表示各种错误
- 提供ErrorMessage字段用于调试

### 5. 时间戳
- 使用Unix时间戳（毫秒）
- 客户端和服务器时间同步
- 可通过心跳响应获取服务器时间

## 相关文档

- [消息ID设计文档](../MESSAGE_ID_DESIGN.md)
- [NetworkManager使用指南](../NETWORK_MANAGER_GUIDE.md)
- [Protobuf设置指南](../PROTOBUF_SETUP.md)
- [TCP客户端使用指南](../TCP_CLIENT_GUIDE.md)
