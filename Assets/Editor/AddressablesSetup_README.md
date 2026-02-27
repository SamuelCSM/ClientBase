# Addressables 配置指南

## 安装步骤

### 1. 安装 Addressables 包
1. 打开 Unity Editor
2. 打开 Window > Package Manager
3. 点击左上角的 "+" 按钮
4. 选择 "Add package by name..."
5. 输入 `com.unity.addressables`
6. 点击 "Add"

### 2. 初始化 Addressables 配置
安装完成后，在 Unity Editor 中：
1. 点击菜单 `Framework > Setup Addressables`
2. 脚本会自动创建 Addressables 设置和资源分组

### 3. 验证配置
点击菜单 `Framework > Validate Addressables Installation` 验证配置是否成功。

---

## 资源分组说明

配置脚本会自动创建以下资源分组：

- **Framework**: 框架核心资源（如管理器预制体等）
- **Common**: 通用资源（如通用材质、纹理等）
- **UI**: UI资源（UI预制体、图集等）
- **Scene**: 场景资源（游戏场景）

---

## 如何标记资源为 Addressable

### 方法1：通过 Inspector 面板
1. 在 Project 窗口中选择要标记的资源（预制体、材质、纹理等）
2. 在 Inspector 窗口顶部找到 "Addressable" 复选框
3. 勾选 "Addressable" 复选框
4. 设置以下属性：
   - **Address**: 资源的唯一地址（用于代码加载），例如 `UI/LoginPanel`
   - **Group**: 选择资源所属的分组（Framework/Common/UI/Scene）
   - **Labels**: 添加标签（可选），用于批量加载

### 方法2：通过 Addressables Groups 窗口
1. 打开 `Window > Asset Management > Addressables > Groups`
2. 将资源从 Project 窗口拖拽到对应的 Group 中
3. 双击资源名称可以修改 Address

---

## 资源地址命名规范

建议使用以下命名规范：

```
类型/子类型/资源名称

示例：
- UI/Login/LoginPanel          (登录面板)
- UI/Main/MainPanel            (主界面面板)
- UI/Common/Button             (通用按钮)
- Prefab/Character/Player      (玩家预制体)
- Prefab/Effect/Explosion      (爆炸特效)
- Audio/BGM/MainTheme          (背景音乐)
- Audio/SFX/ButtonClick        (音效)
- Scene/Login                  (登录场景)
- Scene/Main                   (主场景)
```

---

## 常用标签建议

标签用于批量加载和管理资源：

- `preload`: 需要预加载的资源
- `resident`: 常驻内存的资源（不会被自动卸载）
- `ui_common`: 通用UI资源
- `ui_login`: 登录相关UI
- `ui_main`: 主界面UI
- `scene_login`: 登录场景
- `scene_main`: 主场景

---

## 代码中如何加载资源

### 1. 加载单个资源

```csharp
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class Example : MonoBehaviour
{
    // 方法1：使用 UniTask（推荐）
    async UniTask LoadAssetExample()
    {
        // 加载预制体
        var handle = Addressables.LoadAssetAsync<GameObject>("UI/Login/LoginPanel");
        GameObject prefab = await handle.ToUniTask();
        
        // 实例化
        GameObject instance = Instantiate(prefab);
        
        // 使用完后释放（重要！）
        Addressables.Release(handle);
    }
    
    // 方法2：使用回调
    void LoadAssetWithCallback()
    {
        Addressables.LoadAssetAsync<GameObject>("UI/Login/LoginPanel").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject prefab = handle.Result;
                GameObject instance = Instantiate(prefab);
            }
            else
            {
                Debug.LogError($"加载失败: {handle.OperationException}");
            }
        };
    }
}
```

### 2. 直接实例化资源

```csharp
async UniTask InstantiateExample()
{
    // 直接实例化（Addressables 会自动管理引用计数）
    var handle = Addressables.InstantiateAsync("Prefab/Character/Player");
    GameObject instance = await handle.ToUniTask();
    
    // 使用完后释放实例（会自动释放资源）
    Addressables.ReleaseInstance(instance);
}
```

### 3. 批量加载资源（通过标签）

```csharp
async UniTask LoadAssetsByLabel()
{
    // 加载所有带 "preload" 标签的资源
    var handle = Addressables.LoadAssetsAsync<GameObject>("preload", null);
    var assets = await handle.ToUniTask();
    
    Debug.Log($"预加载了 {assets.Count} 个资源");
    
    // 使用完后释放
    Addressables.Release(handle);
}
```

### 4. 加载场景

```csharp
using UnityEngine.SceneManagement;

async UniTask LoadSceneExample()
{
    // 加载场景（Additive 模式）
    var handle = Addressables.LoadSceneAsync("Scene/Main", LoadSceneMode.Additive);
    await handle.ToUniTask();
    
    Debug.Log("场景加载完成");
}
```

---

## 资源释放（重要！）

Addressables 使用引用计数管理资源生命周期，必须正确释放资源：

```csharp
// 释放 LoadAssetAsync 加载的资源
Addressables.Release(handle);

// 释放 InstantiateAsync 实例化的对象
Addressables.ReleaseInstance(gameObject);

// 卸载场景
Addressables.UnloadSceneAsync(sceneHandle);
```

**注意事项：**
- 每次 `LoadAssetAsync` 都会增加引用计数，必须调用 `Release` 减少引用计数
- 当引用计数为 0 时，资源才会被真正卸载
- 使用 `InstantiateAsync` 创建的对象，必须用 `ReleaseInstance` 释放

---

## 资源打包

### 1. 构建 Addressables 资源包

1. 打开 `Window > Asset Management > Addressables > Groups`
2. 点击 `Build > New Build > Default Build Script`
3. 等待打包完成

打包后的资源会生成在：
- Windows: `ServerData/Windows/`
- Android: `ServerData/Android/`
- iOS: `ServerData/iOS/`

### 2. 清理构建缓存

如果需要重新打包：
1. 点击 `Build > Clean Build > All`
2. 然后重新执行打包

---

## 最佳实践

### 1. 资源分组策略
- **按功能分组**: UI、角色、场景、音频等
- **按更新频率分组**: 经常更新的资源单独分组
- **按大小分组**: 大资源单独打包，小资源合并打包

### 2. 打包模式选择
- **PackTogether**: 将分组内所有资源打包到一个 AssetBundle（默认）
- **PackSeparately**: 每个资源单独打包
- **PackTogetherByLabel**: 按标签打包

修改方式：
1. 打开 Addressables Groups 窗口
2. 选择分组，在 Inspector 中找到 `BundledAssetGroupSchema`
3. 修改 `Bundle Mode`

### 3. 资源预加载
在游戏启动时预加载常用资源：

```csharp
async UniTask PreloadCommonAssets()
{
    // 预加载所有带 "preload" 标签的资源
    var handle = Addressables.LoadAssetsAsync<GameObject>("preload", null);
    await handle.ToUniTask();
    
    // 不要释放！保持在内存中
    // Addressables.Release(handle); // 不调用
}
```

### 4. 常驻资源
对于需要常驻内存的资源（如通用UI、音效等）：
- 添加 `resident` 标签
- 在游戏启动时加载
- 游戏运行期间不释放

---

## 调试和分析

### 1. 查看资源加载情况
打开 `Window > Asset Management > Addressables > Event Viewer`
可以实时查看：
- 正在加载的资源
- 已加载的资源
- 引用计数
- 内存占用

### 2. 分析工具
打开 `Window > Asset Management > Addressables > Analyze`
可以分析：
- 重复资源
- 资源依赖关系
- Bundle 大小

---

## 常见问题

### Q1: 资源加载失败？
- 检查 Address 是否正确
- 检查资源是否已标记为 Addressable
- 检查是否已执行打包（编辑器模式下可以不打包）

### Q2: 内存占用过高？
- 检查是否正确释放了资源
- 使用 Event Viewer 查看引用计数
- 避免重复加载同一资源

### Q3: 如何在编辑器中测试？
Addressables 支持三种播放模式（在 Groups 窗口顶部切换）：
- **Use Asset Database (fastest)**: 直接从 AssetDatabase 加载，最快
- **Simulate Groups (advanced)**: 模拟打包，但不真正打包
- **Use Existing Build**: 使用已打包的资源

开发时推荐使用第一种模式。
