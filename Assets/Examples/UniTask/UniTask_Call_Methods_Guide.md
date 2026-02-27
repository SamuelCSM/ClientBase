# UniTask 调用方式完全指南

## 核心答案：不一定要用 async/await！

UniTask 提供了多种调用方式，你可以根据场景选择最合适的。

## 五种调用方式对比

### 方式1: 使用 async/await（推荐）

```csharp
private async void Start()
{
    // 等待任务完成
    await UniTaskHelper.DelayActionSeconds(() =>
    {
        Debug.Log("任务完成");
    }, 1f);
    
    Debug.Log("这行会在任务完成后执行");
}
```

**优点：**
- ✅ 代码清晰易读
- ✅ 可以等待结果
- ✅ 可以捕获异常
- ✅ 可以按顺序执行多个任务

**缺点：**
- ❌ 需要方法标记为 async

**适用场景：** 需要等待任务完成，或者需要按顺序执行多个任务

---

### 方式2: 使用 Forget()（最常用的"不等待"方式）

```csharp
private void Start()
{
    // 启动任务，不等待结果
    UniTaskHelper.DelayActionSeconds(() =>
    {
        Debug.Log("任务完成");
    }, 1f).Forget();
    
    Debug.Log("这行会立即执行，不等待任务");
}
```

**优点：**
- ✅ 简单直接
- ✅ 不需要 async 关键字
- ✅ 适合"发射后不管"的场景
- ✅ 没有编译器警告

**缺点：**
- ❌ 无法等待结果
- ❌ 异常会被忽略（除非配置全局异常处理）

**适用场景：** 启动一个任务，不关心何时完成，不需要等待结果

---

### 方式3: 返回 UniTask 让调用者决定

```csharp
// 定义方法时返回 UniTask
public UniTask DoWorkAsync()
{
    return UniTaskHelper.DelayActionSeconds(() =>
    {
        Debug.Log("工作完成");
    }, 1f);
}

// 调用时可以选择等待或不等待
private void Start()
{
    // 选择1: 等待
    await DoWorkAsync();
    
    // 选择2: 不等待
    DoWorkAsync().Forget();
}
```

**优点：**
- ✅ 灵活，让调用者决定是否等待
- ✅ 方法本身不需要 async 关键字

**适用场景：** 封装异步操作，让调用者决定如何使用

---

### 方式4: 使用 ContinueWith（链式调用）

```csharp
private void Start()
{
    UniTaskHelper.DelayActionSeconds(() =>
    {
        Debug.Log("第一个任务完成");
    }, 1f)
    .ContinueWith(() =>
    {
        Debug.Log("继续执行后续操作");
        return UniTask.CompletedTask;
    })
    .Forget();
}
```

**优点：**
- ✅ 可以链式处理多个任务
- ✅ 不需要 async 关键字

**缺点：**
- ❌ 代码可读性不如 async/await

**适用场景：** 需要链式处理多个任务，但不想用 async/await

---

### 方式5: 直接调用（不推荐）

```csharp
private void Start()
{
    // 会有编译器警告 CS4014
    UniTaskHelper.DelayActionSeconds(() =>
    {
        Debug.Log("任务完成");
    }, 1f);
}
```

**缺点：**
- ❌ 编译器会警告
- ❌ 容易忘记处理异常
- ❌ 不推荐使用

---

## 实际使用建议

### 场景1: 需要等待结果

```csharp
private async void OnButtonClick()
{
    // 显示加载界面
    ShowLoading();
    
    // 等待加载完成
    await LoadDataAsync();
    
    // 隐藏加载界面
    HideLoading();
}
```

**使用：** `async/await`

---

### 场景2: 启动后台任务，不关心何时完成

```csharp
private void Start()
{
    // 启动心跳任务
    StartHeartbeat().Forget();
    
    // 启动日志上传任务
    StartLogUpload().Forget();
}
```

**使用：** `.Forget()`

---

### 场景3: 在普通类中使用

```csharp
public class MyManager
{
    private CancellationTokenSource _cts = new CancellationTokenSource();
    
    // 方式1: 不等待
    public void StartWork()
    {
        DoWorkAsync(_cts.Token).Forget();
    }
    
    // 方式2: 返回 UniTask 让调用者决定
    public UniTask StartWorkAsync()
    {
        return DoWorkAsync(_cts.Token);
    }
    
    private async UniTask DoWorkAsync(CancellationToken token)
    {
        await UniTask.Delay(1000, cancellationToken: token);
    }
}
```

---

### 场景4: 按顺序执行多个任务

```csharp
private async void Start()
{
    // 任务1
    await Task1();
    
    // 任务2（等任务1完成后执行）
    await Task2();
    
    // 任务3（等任务2完成后执行）
    await Task3();
}
```

**使用：** `async/await`

---

### 场景5: 并行执行多个任务

```csharp
private async void Start()
{
    // 同时启动三个任务
    var task1 = Task1();
    var task2 = Task2();
    var task3 = Task3();
    
    // 等待所有任务完成
    await UniTask.WhenAll(task1, task2, task3);
}
```

**使用：** `async/await` + `UniTask.WhenAll`

---

## 快速决策树

```
需要等待任务完成吗？
├─ 是 → 使用 async/await
└─ 否 → 使用 .Forget()

需要处理异常吗？
├─ 是 → 使用 async/await + try/catch
└─ 否 → 使用 .Forget()

需要按顺序执行多个任务吗？
├─ 是 → 使用 async/await
└─ 否 → 使用 .Forget()

在普通类中使用？
├─ 需要等待 → 返回 UniTask
└─ 不需要等待 → 使用 .Forget()
```

## 总结

| 场景 | 推荐方式 | 是否需要 async |
|------|---------|---------------|
| 等待任务完成 | `await` | ✅ 是 |
| 不等待，发射后不管 | `.Forget()` | ❌ 否 |
| 让调用者决定 | 返回 `UniTask` | ❌ 否 |
| 链式调用 | `.ContinueWith()` | ❌ 否 |
| 按顺序执行多个任务 | `await` | ✅ 是 |
| 并行执行多个任务 | `await` + `WhenAll` | ✅ 是 |

**最常用的两种方式：**
1. **需要等待** → `async/await`
2. **不需要等待** → `.Forget()`

就像写普通脚本一样，使用 `.Forget()` 就可以了！🎯
