# Proto类生成器

Unity编辑器工具，用于从.txt定义文件自动生成protobuf消息类。

## 项目结构

```
Assets/Editor/ProtoGenerator/
├── ProtoGenerator.asmdef          # 程序集定义
├── README.md                      # 项目说明文档
├── Models/                        # 数据模型
│   ├── MessageDefinition.cs       # 消息定义模型
│   ├── FieldDefinition.cs         # 字段定义模型
│   ├── EnumDefinition.cs          # 枚举定义模型
│   ├── EnumValueDefinition.cs     # 枚举值定义模型
│   ├── MessageType.cs             # 消息类型枚举
│   ├── GenerationContext.cs       # 生成上下文
│   ├── GenerationOptions.cs       # 生成选项
│   ├── GenerationResult.cs        # 生成结果
│   └── GenerationError.cs         # 错误信息模型
└── Core/                          # 核心功能
    └── ProtoGeneratorLogger.cs    # 日志工具
```

## 核心数据模型

### MessageDefinition
消息定义数据模型，包含消息的所有元数据：
- 消息名称和命名空间
- 消息类型（枚举/通用类/发送/接收）
- 字段列表
- 主ID和子ID
- 依赖关系

### FieldDefinition
字段定义数据模型，描述消息中的单个字段：
- 字段名和类型
- ProtoMember索引
- 是否可选
- 注释信息

### EnumDefinition
枚举定义数据模型，用于生成枚举类型。

### GenerationContext
生成上下文，包含生成过程所需的配置信息。

### GenerationResult
生成结果，包含成功/失败状态、生成的文件列表和错误信息。

## 使用说明

### 1. 准备定义文件

在 `Assets/ProtoDefinitions/` 目录下创建.txt定义文件，按照以下结构组织：

```
Assets/ProtoDefinitions/
├── Enum/
│   └── enum.txt                 # 所有枚举定义
├── Common/
│   └── common.txt               # 所有通用类定义
├── GC2GS/                       # 客户端到服务器消息
│   ├── C001_LoginMessages/
│   │   └── LoginMessages.txt    # 登录模块消息
│   └── C002_PlayerMessages/
│       └── PlayerMessages.txt   # 玩家模块消息
└── GS2GC/                       # 服务器到客户端消息
    ├── S001_LoginMessages/
    │   └── LoginMessages.txt
    └── S002_PlayerMessages/
        └── PlayerMessages.txt
```

**文件命名规则：**
- **Enum目录**：使用 `enum.txt` 文件，包含所有枚举定义
- **Common目录**：使用 `common.txt` 文件，包含所有通用类定义
- **GC2GS/GS2GC目录**：按模块组织，文件名可自定义（如 `LoginMessages.txt`）

### 2. 定义文件格式

**消息定义（新格式）：**
```
// 消息注释
message 前缀_主协议号_子协议号_消息名 {
    类型 字段名 = 索引;  // 字段注释
}
```

**枚举定义：**
```
// 枚举注释
enum 枚举名 {
    值名称 = 数值;  // 值注释
}
```

**通用类定义：**
```
// 通用类注释
message 类名 {
    类型 字段名 = 索引;  // 字段注释
}
```

**命名规则：**
- 前缀：`GC2GS`（客户端→服务器）或 `GS2GC`（服务器→客户端）
- 主协议号：3位数字（001-999）
- 子协议号：3位数字（001-999）
- 消息名：实际的类名

**示例：enum.txt（枚举文件）**
```
// 平台类型枚举
enum PlatformType {
    Windows = 0;  // Windows平台
    Android = 1;  // Android平台
    iOS = 2;      // iOS平台
}

// 错误码枚举
enum ErrorCode {
    Success = 0;  // 成功
    InvalidParameter = 1;  // 参数无效
}
```

**示例：common.txt（通用类文件）**
```
// 玩家信息通用类
message PlayerInfo {
    int64 PlayerId = 1;  // 玩家ID
    string PlayerName = 2;  // 玩家名称
    int32 Level = 3;  // 等级
}

// 物品信息通用类
message ItemInfo {
    int32 ItemId = 1;  // 物品ID
    int32 Count = 2;  // 数量
}
```

**示例：LoginMessages.txt（登录模块，一个文件包含多个消息）**
```
// 登录模块消息定义 - 主协议号 001

// 登录请求消息
message GC2GS_001_001_LoginRequest {
    string Username = 1;  // 用户名
    string Password = 2;  // 密码
    int32 Platform = 3;   // 平台类型
}

// 注册请求消息
message GC2GS_001_002_RegisterRequest {
    string Username = 1;  // 用户名
    string Password = 2;  // 密码
    string Email = 3;     // 邮箱
}
```

### 3. 生成代码

在Unity编辑器中，选择菜单：
- `Tools/ProtoGenerator/Generate All Classes` - 生成所有Proto类
- `Tools/ProtoGenerator/Open Definitions Folder` - 打开定义文件夹
- `Tools/ProtoGenerator/Open Output Folder` - 打开输出文件夹
- `Tools/ProtoGenerator/Clear Generated Files` - 清除生成的文件

### 4. 生成的代码结构

生成的代码将保存在：
```
Assets/Scripts/Framework/Network/Messages/
├── Enum/                    # 枚举类
│   ├── PlatformType.cs
│   ├── ErrorCode.cs
│   └── PlayerStatus.cs
├── Common/                  # 通用类
│   ├── PlayerInfo.cs
│   ├── ItemInfo.cs
│   └── Position.cs
├── GC2GS/                   # 发送协议
│   └── C001_LoginMessages/
│       ├── GC2GS_001_001_LoginRequest.cs
│       └── GC2GS_001_002_RegisterRequest.cs
└── GS2GC/                   # 接收协议
    └── S001_LoginMessages/
        ├── GS2GC_001_001_LoginResponse.cs
        └── GS2GC_001_002_RegisterResponse.cs
```

**重要说明：**
- 每个消息/枚举定义会生成独立的.cs文件
- 消息文件名使用完整的消息名称（包含前缀和协议号）
- 枚举和通用类文件名使用类名
- 例如：`GC2GS_001_001_LoginRequest.cs`、`PlatformType.cs`
- 这样便于通过文件名快速识别协议号和消息类型
- 一个.txt文件中的多个定义会生成多个.cs文件

## 开发进度

- [x] 任务1：建立项目结构和核心接口
- [x] 任务2：实现定义文件发现和解析功能
- [x] 任务3：检查点 - 确保解析功能正常工作
- [x] 任务4：实现依赖关系解析
- [x] 任务5：实现路径解析和目录映射
- [x] 任务6：实现代码生成器
- [x] 任务7：检查点 - 确保代码生成功能正常工作
- [x] 任务8：实现文件写入和管理
- [x] 任务9：实现主控制器和批处理功能
- [x] 任务10：实现错误处理和报告
- [x] 任务11：实现Unity编辑器集成
- [x] 任务12：集成和端到端测试
- [x] 任务13：最终检查点

## 功能特性

✓ 自动发现和解析.txt定义文件
✓ 支持message和enum定义
✓ 支持一个文件包含多个消息定义
✓ 从消息名称自动提取主协议号和子协议号
✓ 自动生成ProtoContract属性和IMessage接口
✓ 智能依赖关系解析
✓ 保持目录结构映射
✓ 完整的错误处理和日志记录
✓ Unity编辑器菜单集成
✓ 一键生成所有类

## 消息命名规范

**格式：** `前缀_主协议号_子协议号_类名`

**示例：**
- `GC2GS_001_001_LoginRequest` → 类名：`LoginRequest`，MainId：1，SubId：1
- `GS2GC_001_001_LoginResponse` → 类名：`LoginResponse`，MainId：1，SubId：1
- `GC2GS_002_003_GetPlayerListRequest` → 类名：`GetPlayerListRequest`，MainId：2，SubId：3

**一个文件多个消息示例：**
```
// LoginMessages.txt 文件内容
message GC2GS_001_001_LoginRequest { ... }
message GC2GS_001_002_RegisterRequest { ... }

// 生成结果：
// - GC2GS_001_001_LoginRequest.cs  （文件名包含完整协议号）
// - GC2GS_001_002_RegisterRequest.cs
```

**生成的代码：**
```csharp
[ProtoContract]
public class LoginRequest : IMessage
{
    [ProtoMember(1)]
    public string Username { get; set; }
    
    public byte GetMainId() { return 1; }  // 从 001 提取
    public byte GetSubId() { return 1; }   // 从 001 提取
}
```
