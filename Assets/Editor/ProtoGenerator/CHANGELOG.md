# Proto类生成器 - 更新日志

## 版本 1.1.0 - 协议号自动提取

### 新功能
✓ **从消息名称自动提取协议号**
  - 支持格式：`前缀_主协议号_子协议号_类名`
  - 示例：`GC2GS_001_001_LoginRequest`
  - 自动解析：MainId=1, SubId=1, 类名=LoginRequest

✓ **支持一个文件包含多个消息定义**
  - 可以在一个.txt文件中定义多个message
  - 适合按模块组织消息（如登录模块的所有消息）
  - 每个消息生成独立的.cs文件

✓ **文件名包含完整协议号**
  - 生成的文件名使用完整的消息名称
  - 例如：`GC2GS_001_001_LoginRequest.cs`
  - 便于通过文件名快速识别协议号和消息类型

✓ **智能命名验证**
  - 自动检测消息名称是否符合规范
  - 不符合规范时给出警告提示

### 消息命名规范

**格式：** `前缀_主协议号_子协议号_类名`

**组成部分：**
- **前缀**：`GC2GS`（客户端→服务器）或 `GS2GC`（服务器→客户端）
- **主协议号**：3位数字（001-999），表示功能模块
- **子协议号**：3位数字（001-999），表示具体消息
- **类名**：实际生成的C#类名

**示例：**
```
message GC2GS_001_001_LoginRequest {
    string Username = 1;
    string Password = 2;
}

message GC2GS_001_002_RegisterRequest {
    string Username = 1;
    string Password = 2;
    string Email = 3;
}
```

**生成结果：**
- 文件名：`GC2GS_001_001_LoginRequest.cs` 和 `GC2GS_001_002_RegisterRequest.cs`
- 类名：`LoginRequest` 和 `RegisterRequest`
- MainId：1（从001转换）
- SubId：1和2（从001和002转换）

**优势：**
- 文件名包含完整协议号，便于快速查找
- 类名简洁，便于代码中使用
- 通过文件名即可识别消息的模块和类型

### 文件组织建议

```
ProtoDefinitions/
├── GC2GS/
│   ├── C001_LoginMessages/
│   │   └── LoginMessages.txt      # 包含所有登录相关的请求消息
│   └── C002_PlayerMessages/
│       └── PlayerMessages.txt     # 包含所有玩家相关的请求消息
└── GS2GC/
    ├── S001_LoginMessages/
    │   └── LoginMessages.txt      # 包含所有登录相关的响应消息
    └── S002_PlayerMessages/
        └── PlayerMessages.txt     # 包含所有玩家相关的响应消息
```

### 迁移指南

**旧格式（已弃用）：**
```
// MainId: 2, SubId: 1
message LoginRequest {
    string Username = 1;
}
```

**新格式（推荐）：**
```
message GC2GS_001_001_LoginRequest {
    string Username = 1;
}
```

### 技术细节

- 使用正则表达式 `^(GC2GS|GS2GC)_(\d{3})_(\d{3})_(\w+)$` 匹配消息名称
- 协议号自动转换为byte类型（001→1, 010→10）
- 支持向后兼容（不符合规范的消息名称仍可使用，但会警告）

---

## 版本 1.0.0 - 初始版本

### 核心功能
✓ 自动发现和解析.txt定义文件
✓ 支持message和enum定义
✓ 生成ProtoContract属性和IMessage接口
✓ 智能依赖关系解析
✓ 保持目录结构映射
✓ Unity编辑器菜单集成
