# Unity Addressables 使用指南

## 📋 目录
1. [什么是Addressables](#什么是addressables)
2. [打开Addressables窗口](#打开addressables窗口)
3. [资源分组说明](#资源分组说明)
4. [如何添加资源到Addressables](#如何添加资源到addressables)
5. [资源打包构建](#资源打包构建)
6. [在代码中加载资源](#在代码中加载资源)
7. [常见问题](#常见问题)

---

## 什么是Addressables

Addressables是Unity的资源管理系统，它提供：
- **异步加载** - 不阻塞主线程
- **内存管理** - 自动引用计数和资源释放
- **热更新支持** - 可以从远程服务器下载资源
- **资源分组** - 按功能模块组织资源

---

## 打开Addressables窗口

### 方法1：通过菜单打开
1. 在Unity编辑器顶部菜单栏，点击 **Window** → **Asset Management** → **Addressables** → **Groups**
2. 会打开Addressables Groups窗口

### 方法2：快捷键
- Windows: `Ctrl + Shift + A`
- Mac: `Cmd + Shift + A`

### 首次使用
如果是第一次使用Addressables，可能需要：
1. 点击菜单 **Window** → **Asset Management** → **Addressables** → **Settings**
2. 在弹出的窗口中点击 **Create Addressables Settings**

---

## 资源分组说明

本项目已经预先配置了以下资源分组：

### 📁 Framework（框架资源）
- **用途**: 存放框架相关的**资源文件**（不是代码！）
- **示例**: 框架UI预制体（Loading界面、通用弹窗）、通用材质、通用Shader、默认字体
- **加载时机**: 游戏启动时预加载
- **注意**: Framework代码（C#脚本）在 `Assets/Scripts/Framework/` 中，不需要放到Addressables

### 📁 Common（通用资源）
- **用途**: 存放游戏通用资源
- **示例**: 通用特效、通用音效、通用图标
- **加载时机**: 游戏启动后按需加载

### 📁 UI（UI资源）
- **用途**: 存放所有UI预制体和UI相关资源
- **示例**: 登录界面、主界面、背包界面等
- **加载时机**: 打开UI时动态加载

### 📁 Scene（场景资源）
- **用途**: 存放场景文件
- **示例**: 主城场景、战斗场景、副本场景
- **加载时机**: 切换场景时加载

### 📁 Default Local Group（默认本地组）
- **用途**: Unity默认分组，一般不使用
- **建议**: 将资源放到上述专用分组中

---

## 如何添加资源到Addressables

### 方法1：拖拽添加（推荐）

1. **打开Addressables Groups窗口**
   - 菜单：Window → Asset Management → Addressables → Groups

2. **选择要添加的资源**
   - 在Project窗口中选择资源（预制体、材质、音频等）

3. **拖拽到对应分组**
   - 将资源拖拽到Addressables Groups窗口中的对应分组
   - 例如：UI预制体拖到"UI"分组

4. **设置Address（资源地址）**
   - 资源添加后会自动生成Address（默认是资源路径）
   - 可以点击Address进行修改，建议使用简短易记的名称
   - 例如：`UI/LoginUI`、`Audio/BGM_Main`

### 方法2：Inspector面板添加

1. **选择资源**
   - 在Project窗口中选择要添加的资源

2. **勾选Addressable**
   - 在Inspector面板顶部，勾选 **Addressable** 复选框

3. **设置分组和地址**
   - 在Inspector中可以看到Address和Group设置
   - 修改Address为你想要的名称
   - 选择合适的Group（分组）

### 方法3：右键菜单添加

1. **选择资源**
   - 在Project窗口中右键点击资源

2. **标记为Addressable**
   - 选择 **Mark as Addressable**

3. **调整设置**
   - 在Addressables Groups窗口中找到该资源
   - 修改Address和Group

---

## 资源打包构建

### 步骤1：打开Build窗口

1. 打开Addressables Groups窗口
2. 点击顶部菜单栏的 **Build** → **New Build** → **Default Build Script**

### 步骤2：选择构建配置

**开发阶段（推荐）：**
- **Play Mode Script**: Use Asset Database (fastest)
  - 不需要打包，直接从Asset Database加载
  - 适合快速迭代开发
  - 在Addressables Groups窗口顶部的下拉菜单中选择

**测试阶段：**
- **Play Mode Script**: Use Existing Build
  - 使用已打包的资源
  - 测试真实的加载流程

**发布阶段：**
- 点击 **Build** → **New Build** → **Default Build Script**
- 等待构建完成
- 构建产物在：`ServerData/` 和 `Library/com.unity.addressables/`

### 步骤3：验证构建

构建完成后，检查：
- `ServerData/[BuildTarget]/` 目录下有资源包文件
- 控制台没有错误信息

---

## 在代码中加载资源

### 使用框架的ResourceManager

本框架已经封装了Addressables，使用非常简单：

#### 1. 异步加载资源

```csharp
using Framework.Core;
using Cysharp.Threading.Tasks;

public class Example : MonoBehaviour
{
    async void Start()
    {
        // 加载预制体
        GameObject prefab = await GameEntry.Resource.LoadAssetAsync<GameObject>("UI/LoginUI");
        
        // 加载音频
        AudioClip clip = await GameEntry.Resource.LoadAssetAsync<AudioClip>("Audio/BGM_Main");
        
        // 加载材质
        Material mat = await GameEntry.Resource.LoadAssetAsync<Material>("Materials/Character");
    }
}
```

#### 2. 实例化GameObject

```csharp
async void CreateUI()
{
    // 直接实例化预制体
    GameObject uiInstance = await GameEntry.Resource.InstantiateAsync("UI/LoginUI");
    
    // 指定父节点
    GameObject uiInstance2 = await GameEntry.Resource.InstantiateAsync(
        "UI/MainUI", 
        transform  // 父节点
    );
}
```

#### 3. 预加载资源

```csharp
async void PreloadResources()
{
    // 预加载多个资源
    List<string> addresses = new List<string>
    {
        "UI/LoginUI",
        "UI/MainUI",
        "Audio/BGM_Main"
    };
    
    await GameEntry.Resource.PreloadAssetsAsync(addresses);
}
```

#### 4. 释放资源

```csharp
void ReleaseResources()
{
    // 释放资源
    GameEntry.Resource.ReleaseAsset("UI/LoginUI");
    
    // 释放实例化的GameObject
    GameEntry.Resource.ReleaseInstance(uiInstance);
}
```

---

## 常见问题

### Q1: 为什么找不到Addressables窗口？

**A:** 确保已安装Addressables包：
1. 打开 **Window** → **Package Manager**
2. 搜索 **Addressables**
3. 点击 **Install** 安装

### Q2: 资源加载失败，返回null？

**A:** 检查以下几点：
1. 资源是否已添加到Addressables（勾选Addressable）
2. Address名称是否正确（区分大小写）
3. 是否已经构建资源包（或使用Asset Database模式）
4. 检查控制台是否有错误信息

### Q3: 如何查看资源的Address？

**A:** 两种方法：
1. 在Addressables Groups窗口中查看
2. 选择资源，在Inspector面板中查看

### Q4: 开发时每次都要Build吗？

**A:** 不需要！
- 在Addressables Groups窗口顶部
- 将 **Play Mode Script** 设置为 **Use Asset Database (fastest)**
- 这样就不需要Build，直接从Asset Database加载

### Q5: 如何删除Addressables中的资源？

**A:** 三种方法：
1. 在Addressables Groups窗口中右键资源 → **Remove Addressable**
2. 选择资源，在Inspector中取消勾选 **Addressable**
3. 在Addressables Groups窗口中选中资源，按 **Delete** 键

### Q6: 资源打包后在哪里？

**A:** 构建产物位置：
- **本地缓存**: `Library/com.unity.addressables/aa/[Platform]/`
- **服务器资源**: `ServerData/[Platform]/`（用于热更新）

### Q7: 如何实现热更新？

**A:** 热更新流程：
1. 构建资源包（Build → New Build）
2. 将 `ServerData/` 目录上传到服务器
3. 在代码中调用：
```csharp
// 检查更新大小
long size = await GameEntry.Resource.GetDownloadSizeAsync("UI");

// 下载更新
await GameEntry.Resource.DownloadDependenciesAsync("UI", progress =>
{
    Debug.Log($"下载进度: {progress * 100}%");
});
```

### Q8: 如何给资源打标签（Label）？

**A:** 标签用于批量管理资源：
1. 在Addressables Groups窗口中选择资源
2. 在Inspector面板中找到 **Labels** 部分
3. 点击 **+** 添加标签
4. 可以通过标签批量加载资源

---

## 📚 推荐资源

- [Unity Addressables官方文档](https://docs.unity3d.com/Packages/com.unity.addressables@latest)
- [Addressables视频教程](https://learn.unity.com/tutorial/addressables)

---

## 💡 最佳实践

1. **合理分组**
   - 按功能模块分组（UI、Audio、Scene等）
   - 相关资源放在同一组，方便批量加载和卸载

2. **命名规范**
   - Address使用统一的命名规范
   - 建议格式：`类型/名称`，如 `UI/LoginUI`、`Audio/BGM_Main`

3. **开发模式**
   - 开发时使用 **Use Asset Database** 模式
   - 发布前切换到 **Use Existing Build** 测试

4. **内存管理**
   - 及时释放不用的资源
   - 使用对象池管理频繁创建销毁的对象

5. **预加载**
   - 在合适的时机预加载资源（如Loading界面）
   - 避免游戏过程中卡顿

---

## 🎯 快速开始示例

### 示例1：创建一个简单的UI

```csharp
using UnityEngine;
using Framework.Core;
using Cysharp.Threading.Tasks;

public class UIExample : MonoBehaviour
{
    async void Start()
    {
        // 1. 加载UI预制体
        GameObject loginUI = await GameEntry.Resource.InstantiateAsync("UI/LoginUI");
        
        // 2. 设置父节点（可选）
        loginUI.transform.SetParent(transform, false);
        
        // 3. 使用完毕后释放
        // GameEntry.Resource.ReleaseInstance(loginUI);
    }
}
```

### 示例2：加载并播放音频

```csharp
using UnityEngine;
using Framework.Core;
using Cysharp.Threading.Tasks;

public class AudioExample : MonoBehaviour
{
    async void Start()
    {
        // 加载音频资源
        AudioClip clip = await GameEntry.Resource.LoadAssetAsync<AudioClip>("Audio/BGM_Main");
        
        // 播放音频
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
```

---

**祝你使用愉快！如有问题，请查看常见问题部分或查阅官方文档。** 🎉
