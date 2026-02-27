# Proto定义文件说明

## 目录结构

```
ProtoDefinitions/
├── Enum/           # 枚举定义
├── Common/         # 通用类定义
├── GC2GS/          # 客户端到服务器消息（发送协议）
└── GS2GC/          # 服务器到客户端消息（接收协议）
```

## 定义文件格式

### 消息定义（新格式 - 从消息名称提取协议号）

```
// 消息注释
message 前缀_主协议号_子协议号_消息名 {
    类型 字段名 = 索引;  // 字段注释
}
```

**命名规则：**
- 前缀：`GC2GS`（客户端到服务器）或 `GS2GC`（服务器到客户端）
- 主协议号：3位数字，如 `001`
- 子协议号：3位数字，如 `001`
- 消息名：实际的类名，如 `LoginRequest`

**完整示例：** `GC2GS_001_001_LoginRequest`
- `GC2GS` = 客户端到服务器
- `001` = 主协议号（登录模块）
- `001` = 子协议号（登录请求）
- `LoginRequest` = 生成的类名

### 示例：登录模块消息

```
// 登录模块消息定义 - 主协议号 001

// 登录请求消息
message GC2GS_001_001_LoginRequest {
    string Username = 1;  // 用户名
    string Password = 2;  // 密码
    string ClientVersion = 3;  // 客户端版本
    string DeviceId = 4;  // 设备ID
    int32 Platform = 5;  // 平台类型
}

// 注册请求消息
message GC2GS_001_002_RegisterRequest {
    string Username = 1;  // 用户名
    string Password = 2;  // 密码
    string Email = 3;  // 邮箱
    int32 Platform = 4;  // 平台类型
}
```

**说明：**
- 一个.txt文件可以包含多个消息定义
- 通常一个文件包含同一主协议号的所有消息
- 文件名建议使用主协议号命名，如 `C001_LoginMessages.txt`

### 枚举定义

```
// 枚举注释
enum EnumName {
    值名称 = 数值;  // 值注释
}
```

### 示例：平台类型

```
// 平台类型枚举
enum PlatformType {
    Windows = 0;  // Windows平台
    Android = 1;  // Android平台
    iOS = 2;  // iOS平台
    WebGL = 3;  // WebGL平台
}
```

## 支持的类型

### Protobuf基础类型
- `int32` → `int`
- `int64` → `long`
- `uint32` → `uint`
- `uint64` → `ulong`
- `bool` → `bool`
- `string` → `string`
- `double` → `double`
- `float` → `float`

### 自定义类型
- 可以引用Common目录下定义的通用类
- 可以引用Enum目录下定义的枚举

## 生成规则

### 目录映射

定义文件路径会映射到生成文件路径，生成的文件名使用完整的消息名称：

```
ProtoDefinitions/GC2GS/C001_LoginMessages/LoginRequest.txt
  包含: message GC2GS_001_001_LoginRequest { ... }
    ↓
Scripts/Framework/Network/Messages/GC2GS/C001_LoginMessages/GC2GS_001_001_LoginRequest.cs
  文件名: GC2GS_001_001_LoginRequest.cs （包含完整协议号）
  类名: LoginRequest
  MainId: 1, SubId: 1
```

**文件名规则：**
- 使用完整的消息名称作为文件名
- 包含前缀和协议号，便于识别
- 例如：`GC2GS_001_001_LoginRequest.cs`

### 命名空间

- Enum → `Framework.Network.Messages.Enum`
- Common → `Framework.Network.Messages.Common`
- GC2GS → `Framework.Network.Messages.GC2GS`
- GS2GC → `Framework.Network.Messages.GS2GC`

### 消息ID

消息ID从消息名称中自动提取：

```
message GC2GS_001_001_LoginRequest { ... }
         ^^^^ ^^^ ^^^
         前缀 主ID 子ID
```

生成的代码会实现IMessage接口：
```csharp
public byte GetMainId() { return 1; }   // 从 001 转换
public byte GetSubId() { return 1; }    // 从 001 转换
```

## 使用流程

1. 在对应目录下创建.txt定义文件
2. 按照格式编写消息或枚举定义
3. 在Unity编辑器中选择 `Tools/ProtoGenerator/Generate All Classes`
4. 生成的C#类会自动保存到对应目录

## 注意事项

- 文件必须使用.txt扩展名
- 消息名称必须遵循格式：`前缀_主协议号_子协议号_类名`
  - 前缀：`GC2GS` 或 `GS2GC`
  - 协议号：3位数字（001-999）
- 字段索引必须从1开始且唯一
- 一个文件可以包含多个消息定义
- 注释会被保留到生成的代码中
- 支持嵌套目录结构
