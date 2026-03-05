# Addressables 快速设置指南

## 🚀 快速开始（3步搞定）

### 第1步：打开Addressables窗口

在Unity编辑器中：
1. 点击顶部菜单 **Window** → **Asset Management** → **Addressables** → **Groups**
2. 会打开一个新窗口，显示所有资源分组

![Addressables窗口位置](https://docs.unity3d.com/Packages/com.unity.addressables@1.19/manual/images/AddressableAssetsWindow.png)

---

### 第2步：添加资源

#### 方法A：拖拽添加（最简单）

1. 在 **Project** 窗口中找到你的资源（预制体、音频、材质等）
2. 直接拖拽到 **Addressables Groups** 窗口中的对应分组
   - UI预制体 → 拖到 **UI** 分组
   - 音频文件 → 拖到 **Common** 分组
   - 场景文件 → 拖到 **Scene** 分组

#### 方法B：右键添加

1. 在 **Project** 窗口中右键点击资源
2. 选择 **Mark as Addressable**
3. 资源会被添加到默认分组

#### 方法C：Inspector添加

1. 在 **Project** 窗口中选择资源
2. 在 **Inspector** 面板顶部勾选 **Addressable**
3. 可以在Inspector中修改Address和Group

---

### 第3步：设置开发模式（重要！）

在 **Addressables Groups** 窗口顶部：

1. 找到 **Play Mode Script** 下拉菜单
2. 选择 **Use Asset Database (fastest)**

✅ 这样设置后，开发时不需要打包，资源直接从Asset Database加载，速度最快！

---

## 📁 资源分组说明

本项目已配置好以下分组，直接使用即可：

| 分组名称 | 用途 | 示例资源 |
|---------|------|---------|
| **Framework** | 框架核心资源 | 框架UI、通用材质 |
| **Common** | 游戏通用资源 | 通用特效、通用音效 |
| **UI** | 所有UI资源 | 登录界面、主界面、背包界面 |
| **Scene** | 场景文件 | 主城场景、战斗场景 |

---

## 💻 在代码中使用

### 加载资源（异步）

```csharp
using Framework.Core;
using Cysharp.Threading.Tasks;

// 加载预制体
GameObject prefab = await GameEntry.Resource.LoadAssetAsync<GameObject>("UI/LoginUI");

// 加载音频
AudioClip clip = await GameEntry.Resource.LoadAssetAsync<AudioClip>("Audio/BGM");

// 加载材质
Material mat = await GameEntry.Resource.LoadAssetAsync<Material>("Materials/Character");
```

### 实例化GameObject

```csharp
// 直接实例化
GameObject obj = await GameEntry.Resource.InstantiateAsync("UI/LoginUI");

// 指定父节点
GameObject obj2 = await GameEntry.Resource.InstantiateAsync("UI/MainUI", parentTransform);
```

### 释放资源

```csharp
// 释放资源
GameEntry.Resource.ReleaseAsset("UI/LoginUI");

// 释放实例
GameEntry.Resource.ReleaseInstance(gameObject);
```

---

## 🎯 Address命名建议

Address是资源的唯一标识，建议使用以下格式：

```
类型/名称

示例：
UI/LoginUI
UI/MainUI
UI/BagUI
Audio/BGM_Main
Audio/SFX_Click
Materials/Character_Skin01
Prefabs/Enemy_Boss01
```

---

## ⚙️ 发布前的打包

开发完成后，发布前需要打包资源：

1. 在 **Addressables Groups** 窗口
2. 点击 **Build** → **New Build** → **Default Build Script**
3. 等待构建完成
4. 构建产物在 `ServerData/` 目录

---

## ❓ 常见问题

### Q: 资源加载返回null？
**A:** 检查：
1. 资源是否已添加到Addressables（勾选Addressable）
2. Address名称是否正确（区分大小写）
3. Play Mode Script是否设置为 Use Asset Database

### Q: 找不到Addressables窗口？
**A:** 
1. 打开 **Window** → **Package Manager**
2. 搜索 **Addressables**
3. 点击 **Install** 安装

### Q: 每次修改都要Build吗？
**A:** 不需要！开发时使用 **Use Asset Database** 模式，不需要Build。

---

## 📚 更多信息

详细使用指南请查看：`Assets/AddressableAssetsData/Addressables使用指南.md`

---

**就这么简单！现在你可以开始使用Addressables了！** 🎉
