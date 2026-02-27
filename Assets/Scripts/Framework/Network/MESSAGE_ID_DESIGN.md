# 消息ID设计说明

## 设计概述

消息ID采用**主ID（MainId）+ 子ID（SubId）**的两级结构，而不是单一的消息ID。

## 消息协议格式

```
┌──────────┬──────────┬──────────┬──────────┬──────────────┐
│  Length  │  MainId  │  SubId   │ Reserved │   Payload    │
│ (4 bytes)│(1 byte)  │(1 byte)  │(2 bytes) │  (N bytes)   │
└──────────┴──────────┴──────────┴──────────┴──────────────┘
```

### 字段说明

- **Length** (4字节): 消息总长度，包含头部
- **MainId** (1字节): 主消息ID，表示功能模块（0-255）
- **SubId** (1字节): 子消息ID，表示该模块下的具体消息类型（0-255）
- **Reserved** (2字节): 保留字段，用于未来扩展
- **Payload** (N字节): Protobuf序列化的消息体

## 设计优势

### 1. 更好的消息组织
- 按功能模块分类，结构清晰
- 每个模块独立管理自己的消息类型
- 便于团队协作开发

### 2. 灵活的扩展性
- 支持最多256个功能模块
- 每个模块支持最多256种消息类型
- 总共可支持 256 × 256 = 65,536 种消息

### 3. 便于消息路由
- 可以根据主ID快速定位到对应的模块处理器
- 模块内部再根据子ID分发到具体的消息处理器
- 支持模块级别的消息拦截和过滤

### 4. 易于维护
- 新增模块不影响现有模块
- 模块内消息ID独立编号，避免冲突
- 便于代码组织和文档管理

## 消息模块定义

推荐在 `MessageModule` 类中定义所有模块ID：

```csharp
public static class MessageModule
{
    public const byte System = 1;      // 系统模块（心跳、ping等）
    public const byte Login = 2;       // 登录认证模块
    public const byte Player = 3;      // 玩家数据模块
    public const byte Battle = 4;      // 战斗模块
    public const byte Social = 5;      // 社交模块
    public const byte Shop = 6;        // 商店模块
    public const byte Chat = 7;        // 聊天模块
    public const byte Guild = 8;       // 公会模块
    public const byte Mail = 9;        // 邮件模块
    public const byte Task = 10;       // 任务模块
    // ... 可扩展到255
}
```

## 消息定义示例

### 基础消息定义

```csharp
[ProtoContract]
public class LoginRequest : IMessage
{
    [ProtoMember(1)]
    public string Username { get; set; }
    
    [ProtoMember(2)]
    public string Password { get; set; }
    
    // 主ID: 登录模块
    public byte GetMainId() => MessageModule.Login;
    
    // 子ID: 登录请求
    public byte GetSubId() => 1;
}

[ProtoContract]
public class LoginResponse : IMessage
{
    [ProtoMember(1)]
    public int ResultCode { get; set; }
    
    [ProtoMember(2)]
    public string Token { get; set; }
    
    // 主ID: 登录模块
    public byte GetMainId() => MessageModule.Login;
    
    // 子ID: 登录响应
    public byte GetSubId() => 2;
}
```

### 模块内子ID定义

为了更好地管理子ID，推荐为每个模块创建子ID常量类：

```csharp
// 登录模块的子ID定义
public static class LoginMessageId
{
    public const byte LoginRequest = 1;
    public const byte LoginResponse = 2;
    public const byte LogoutRequest = 3;
    public const byte LogoutResponse = 4;
    public const byte RegisterRequest = 5;
    public const byte RegisterResponse = 6;
}

// 使用示例
[ProtoContract]
public class LoginRequest : IMessage
{
    // ...
    
    public byte GetMainId() => MessageModule.Login;
    public byte GetSubId() => LoginMessageId.LoginRequest;
}
```

## 消息发送

### 方式1：使用主ID和子ID

```csharp
var message = new LoginRequest { Username = "player", Password = "pass" };
byte[] payload = ProtobufUtil.Serialize(message);
byte[] packet = MessagePacket.Pack(
    MessageModule.Login,      // 主ID
    LoginMessageId.LoginRequest,  // 子ID
    payload
);
client.Send(packet);
```

### 方式2：使用消息对象（推荐）

```csharp
var message = new LoginRequest { Username = "player", Password = "pass" };
byte[] payload = ProtobufUtil.Serialize(message);
byte[] packet = MessagePacket.Pack(message, payload);  // 自动获取主ID和子ID
client.Send(packet);
```

## 消息接收和解析

### 基础解析

```csharp
private void OnReceive(byte[] packet)
{
    if (!MessagePacket.Unpack(packet, out byte mainId, out byte subId, out byte[] payload))
    {
        Logger.Error("消息包解析失败");
        return;
    }
    
    Logger.Debug($"收到消息: 主ID={mainId}, 子ID={subId}");
    
    // 根据主ID和子ID处理消息
    HandleMessage(mainId, subId, payload);
}
```

### 模块化处理

```csharp
private void HandleMessage(byte mainId, byte subId, byte[] payload)
{
    switch (mainId)
    {
        case MessageModule.Login:
            HandleLoginMessage(subId, payload);
            break;
            
        case MessageModule.Player:
            HandlePlayerMessage(subId, payload);
            break;
            
        case MessageModule.Battle:
            HandleBattleMessage(subId, payload);
            break;
            
        default:
            Logger.Warning($"未处理的模块ID: {mainId}");
            break;
    }
}

private void HandleLoginMessage(byte subId, byte[] payload)
{
    switch (subId)
    {
        case LoginMessageId.LoginResponse:
            var response = ProtobufUtil.Deserialize<LoginResponse>(payload);
            OnLoginResponse(response);
            break;
            
        case LoginMessageId.LogoutResponse:
            // 处理登出响应
            break;
            
        default:
            Logger.Warning($"未处理的登录消息: {subId}");
            break;
    }
}
```

## MessagePacket工具方法

### 打包方法

```csharp
// 使用主ID和子ID打包
byte[] Pack(byte mainId, byte subId, byte[] payload)

// 使用消息对象打包（推荐）
byte[] Pack(IMessage message, byte[] payload)
```

### 解包方法

```csharp
// 完整解包
bool Unpack(byte[] packet, out byte mainId, out byte subId, out byte[] payload)
```

### 辅助方法

```csharp
// 获取主ID
byte GetMainId(byte[] packet)

// 获取子ID
byte GetSubId(byte[] packet)

// 获取完整消息ID（主ID和子ID组合为ushort）
ushort GetMessageId(byte[] packet)

// 组合主ID和子ID为完整消息ID
ushort CombineMessageId(byte mainId, byte subId)

// 拆分完整消息ID为主ID和子ID
void SplitMessageId(ushort messageId, out byte mainId, out byte subId)

// 获取消息长度
int GetMessageLength(byte[] packet)

// 验证消息包有效性
bool IsValid(byte[] packet)
```

## 消息ID分配建议

### 主ID分配原则

1. **系统级消息** (1-10): 心跳、ping、时间同步等
2. **账号相关** (11-20): 登录、注册、账号管理
3. **玩家数据** (21-50): 玩家信息、背包、装备等
4. **游戏玩法** (51-100): 战斗、副本、任务等
5. **社交功能** (101-150): 好友、公会、聊天等
6. **商业功能** (151-200): 商店、充值、交易等
7. **预留扩展** (201-255): 未来功能

### 子ID分配原则

1. **请求消息**: 奇数 (1, 3, 5, 7...)
2. **响应消息**: 偶数 (2, 4, 6, 8...)
3. **通知消息**: 从100开始 (100, 101, 102...)

示例：
```
主ID=2 (Login模块)
  子ID=1: LoginRequest (请求)
  子ID=2: LoginResponse (响应)
  子ID=3: LogoutRequest (请求)
  子ID=4: LogoutResponse (响应)
  子ID=100: KickoutNotify (通知)
```

## 与MessageDispatcher集成

消息分发器可以利用主ID和子ID实现两级路由：

```csharp
public class MessageDispatcher
{
    // 模块处理器字典
    private Dictionary<byte, IModuleHandler> _moduleHandlers;
    
    public void DispatchMessage(byte mainId, byte subId, byte[] payload)
    {
        // 根据主ID找到模块处理器
        if (_moduleHandlers.TryGetValue(mainId, out var handler))
        {
            // 模块处理器根据子ID分发消息
            handler.HandleMessage(subId, payload);
        }
        else
        {
            Logger.Warning($"未注册的模块: {mainId}");
        }
    }
}
```

## 兼容性说明

如果需要与旧系统兼容（使用单一ushort消息ID），可以使用以下方法：

```csharp
// 将主ID和子ID组合为ushort
ushort fullMsgId = MessagePacket.CombineMessageId(mainId, subId);

// 将ushort拆分为主ID和子ID
MessagePacket.SplitMessageId(fullMsgId, out byte mainId, out byte subId);
```

组合规则：`fullMsgId = (mainId << 8) | subId`
- 高8位为主ID
- 低8位为子ID

## 总结

主ID+子ID的消息设计提供了：
- ✓ 清晰的模块划分
- ✓ 灵活的扩展能力
- ✓ 高效的消息路由
- ✓ 便于团队协作
- ✓ 易于维护和管理

这种设计特别适合大型游戏项目，能够有效组织和管理数百种消息类型。
