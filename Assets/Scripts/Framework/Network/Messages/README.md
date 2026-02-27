# 网络消息定义

## 目录结构

```
Messages/
├── LoginMessages.cs          # 登录相关消息
├── HeartbeatMessages.cs      # 心跳相关消息
├── MESSAGE_USAGE_GUIDE.md    # 详细使用指南
└── README.md                 # 本文件
```

## 消息列表

### 登录消息 (LoginMessages.cs)

| 消息类型 | 主ID | 子ID | 说明 |
|---------|------|------|------|
| LoginRequest | 1 | 1 | 客户端登录请求 |
| LoginResponse | 1 | 2 | 服务器登录响应 |

### 心跳消息 (HeartbeatMessages.cs)

| 消息类型 | 主ID | 子ID | 说明 |
|---------|------|------|------|
| HeartbeatRequest | 0 | 1 | 客户端心跳请求 |
| HeartbeatResponse | 0 | 2 | 服务器心跳响应 |

## 快速开始

### 发送登录请求

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
    Platform = 0
};

networkManager.SendMessage(request);
```

### 处理登录响应

```csharp
networkManager.RegisterHandler<LoginResponse>(
    MessageModule.Login,
    2,
    response =>
    {
        if (response.ResultCode == 0)
        {
            Debug.Log($"登录成功！用户: {response.Nickname}");
        }
        else
        {
            Debug.LogError($"登录失败: {response.ErrorMessage}");
        }
    }
);
```

## 自定义消息

要创建自定义消息，请参考 [MESSAGE_USAGE_GUIDE.md](MESSAGE_USAGE_GUIDE.md) 中的详细说明。

基本步骤：
1. 创建类并添加 `[ProtoContract]` 特性
2. 实现 `IMessage` 接口
3. 为字段添加 `[ProtoMember(n)]` 特性
4. 定义唯一的主ID和子ID

## 注意事项

- 所有消息必须实现 `IMessage` 接口
- 所有消息必须添加 `[ProtoContract]` 特性
- 所有字段必须添加 `[ProtoMember(n)]` 特性
- 消息ID（主ID+子ID）必须唯一
- 建议使用奇数作为请求，偶数作为响应

## 相关文档

- [MESSAGE_USAGE_GUIDE.md](MESSAGE_USAGE_GUIDE.md) - 详细使用指南
- [../MESSAGE_ID_DESIGN.md](../MESSAGE_ID_DESIGN.md) - 消息ID设计文档
- [../NETWORK_MANAGER_GUIDE.md](../NETWORK_MANAGER_GUIDE.md) - 网络管理器使用指南
