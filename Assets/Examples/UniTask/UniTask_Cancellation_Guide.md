# UniTask 取消任务完全指南

## 核心概念

UniTask 使用 `CancellationToken` 来取消异步任务，这是 .NET 标准的取消机制。**任何类都可以使用**，不限于 MonoBehaviour。

## 基本用法

### 1. 手动取消（适用于所有类）

```csharp
// 创建取消令牌源
var cts = new CancellationTokenSource();

// 启动异步任务，传入取消令牌
var task = SomeAsyncMethod(cts.Token);

// 需要取消时调用
cts.Cancel();

// 使用完毕后释放资源
cts.Dispose();
```

### 2. 超时自动取消

```csharp
var cts = new CancellationTokenSource();
cts.CancelAfter(5000); // 5秒后自动取消

try
{
    await SomeAsyncMethod(cts.Token);
}
catch (OperationCanceledException)
{
    Debug.Log("任务超时被取消");
}
finally
{
    cts.Dispose();
}
```

### 3. MonoBehaviour 生命周期自动取消

UniTask 提供了便捷的扩展方法，可以自动绑定到 GameObject 生命周期：

```csharp
public class MyBehaviour : MonoBehaviour
{
    private async void Start()
    {
        // GameObject 销毁时自动取消
        await SomeAsyncMethod(this.GetCancellationTokenOnDestroy());
    }
}
```

**其他生命周期取消令牌：**
- `this.GetCancellationTokenOnDestroy()` - GameObject 销毁时取消
- `destroyCancellationToken` - 同上的简写属性

## 在普通类中使用

普通类（非 MonoBehaviour）完全可以管理取消令牌：

```csharp
public class MyManager : IDisposable
{
    private CancellationTokenSource _cts;
    
    public MyManager()
    {
        _cts = new CancellationTokenSource();
    }
    
    public void StartWork()
    {
        DoWorkAsync(_cts.Token).Forget();
    }
    
    public void StopWork()
    {
        _cts?.Cancel();
    }
    
    private async UniTaskVoid DoWorkAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // 执行工作
                await UniTask.Delay(1000, cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("工作被取消");
        }
    }
    
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
```

## 使用场景对比

### MonoBehaviour 中

**优点：**
- 可以使用 `GetCancellationTokenOnDestroy()` 自动管理
- 与 Unity 生命周期集成

**示例：**
```csharp
public class Player : MonoBehaviour
{
    private async void Start()
    {
        // 自动绑定到生命周期
        await PlayAnimationAsync(this.GetCancellationTokenOnDestroy());
    }
}
```

### 普通类中

**优点：**
- 更灵活的控制
- 不依赖 Unity 生命周期
- 可以在任何地方使用

**示例：**
```csharp
public class NetworkManager
{
    private CancellationTokenSource _cts = new CancellationTokenSource();
    
    public void Connect()
    {
        ConnectAsync(_cts.Token).Forget();
    }
    
    public void Disconnect()
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource(); // 重新创建以便下次使用
    }
}
```

## 最佳实践

### 1. 总是处理 OperationCanceledException

```csharp
try
{
    await SomeAsyncMethod(cancellationToken);
}
catch (OperationCanceledException)
{
    // 任务被取消是正常情况，不是错误
    Debug.Log("任务被取消");
}
```

### 2. 及时释放 CancellationTokenSource

```csharp
var cts = new CancellationTokenSource();
try
{
    await SomeAsyncMethod(cts.Token);
}
finally
{
    cts.Dispose(); // 确保释放资源
}
```

### 3. 在类中管理取消令牌

```csharp
public class MyClass : IDisposable
{
    private CancellationTokenSource _cts;
    
    public MyClass()
    {
        _cts = new CancellationTokenSource();
    }
    
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

### 4. 组合多个取消令牌

```csharp
var cts1 = new CancellationTokenSource();
var cts2 = new CancellationTokenSource();

// 任意一个取消都会触发
var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    cts1.Token, 
    cts2.Token
);

await SomeAsyncMethod(linkedCts.Token);
```

### 5. 检查取消状态

```csharp
private async UniTask DoWorkAsync(CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        // 执行工作
        await UniTask.Yield();
        
        // 或者手动抛出异常
        token.ThrowIfCancellationRequested();
    }
}
```

## 常见问题

### Q: 普通类可以使用取消令牌吗？
**A:** 可以！`CancellationTokenSource` 是 .NET 标准库的一部分，任何 C# 类都可以使用。

### Q: 必须在 MonoBehaviour 中才能取消任务吗？
**A:** 不是！MonoBehaviour 只是提供了一些便捷方法（如 `GetCancellationTokenOnDestroy()`），但普通类完全可以自己管理取消令牌。

### Q: 如何在单例类中使用？
**A:** 单例类可以持有 `CancellationTokenSource` 成员变量，在需要时创建和取消。

```csharp
public class GameManager : Singleton<GameManager>
{
    private CancellationTokenSource _cts;
    
    public void StartGame()
    {
        _cts = new CancellationTokenSource();
        RunGameLoopAsync(_cts.Token).Forget();
    }
    
    public void StopGame()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

### Q: 取消后可以重新使用同一个 CancellationTokenSource 吗？
**A:** 不可以。取消后需要创建新的 `CancellationTokenSource`。

```csharp
// 错误做法
_cts.Cancel();
await SomeMethod(_cts.Token); // 这个令牌已经被取消了

// 正确做法
_cts.Cancel();
_cts.Dispose();
_cts = new CancellationTokenSource(); // 创建新的
await SomeMethod(_cts.Token);
```

## 总结

- ✅ **任何类都可以使用** CancellationToken
- ✅ **不限于 MonoBehaviour**
- ✅ **普通类通过持有 CancellationTokenSource 成员变量来管理**
- ✅ **MonoBehaviour 可以使用便捷的生命周期绑定方法**
- ✅ **记得在使用完毕后 Dispose**
