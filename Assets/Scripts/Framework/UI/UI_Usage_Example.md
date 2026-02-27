# UI框架使用说明

## 架构设计

新的UI框架采用View-Window完全分离设计：

1. **UIView** - 继承MonoBehaviour，负责序列化UI组件（在Unity编辑器中拖拽赋值）
2. **UIBase<TView>** - 纯C#类，不继承MonoBehaviour，只负责业务逻辑
3. **UIManager** - UI管理器，负责UI的加载、显示、隐藏、销毁等

## 优势

1. **完全分离** - 逻辑和Unity组件完全分离，UIBase是纯C#类
2. **易于测试** - UIBase不依赖MonoBehaviour，可以轻松进行单元测试
3. **组件直接访问** - 通过View属性直接访问序列化的UI组件
4. **层级预定义** - UI层级在注册时确定，无需每次打开时传参
5. **支持多实例** - 可以配置是否允许同一UI打开多个实例
6. **动态Canvas** - 自动创建各层级Canvas，便于管理和扩展

## 使用示例

### 1. 创建UIView类（序列化组件，继承MonoBehaviour）

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 登录界面视图
    /// 在Unity编辑器中拖拽赋值UI组件
    /// 挂载到UI预制体的根节点上
    /// </summary>
    public class LoginUIView : Framework.UIView
    {
        public Button btnLogin;
        public Button btnRegister;
        public InputField inputUsername;
        public InputField inputPassword;
        public Text txtTitle;
    }
}
```

### 2. 创建UIWindow类（业务逻辑，纯C#类）

```csharp
namespace Game.UI
{
    /// <summary>
    /// 登录界面窗口
    /// 处理登录界面的业务逻辑
    /// 注意：不继承MonoBehaviour，是纯C#类
    /// </summary>
    public class LoginUIWindow : Framework.UIBase<LoginUIView>
    {
        protected override void OnInit()
        {
            // 注册事件
            View.btnLogin.onClick.AddListener(OnLoginClick);
            View.btnRegister.onClick.AddListener(OnRegisterClick);
        }

        protected override void OnOpen(object userData)
        {
            // 刷新UI数据
            View.txtTitle.text = "欢迎登录";
            View.inputUsername.text = "";
            View.inputPassword.text = "";
        }

        protected override void OnClose()
        {
            // 清理临时数据
        }

        protected override void OnDestroy()
        {
            // 注销事件
            View.btnLogin.onClick.RemoveListener(OnLoginClick);
            View.btnRegister.onClick.RemoveListener(OnRegisterClick);
        }

        private void OnLoginClick()
        {
            string username = View.inputUsername.text;
            string password = View.inputPassword.text;
            // 处理登录逻辑
        }

        private void OnRegisterClick()
        {
            // 处理注册逻辑
        }
    }
}
```

### 3. 注册UI

```csharp
// 在游戏启动时注册UI
Framework.Core.GameEntry.UI.RegisterUI<LoginUIWindow>(
    "UI/LoginUI",                    // Addressables地址
    Framework.UILayer.Normal,        // UI层级
    false                            // 是否允许多实例
);
```

### 4. 打开UI

```csharp
// 打开UI
var loginUI = await Framework.Core.GameEntry.UI.OpenUIAsync<LoginUIWindow, LoginUIView>();

// 打开UI并传递数据
var loginUI = await Framework.Core.GameEntry.UI.OpenUIAsync<LoginUIWindow, LoginUIView>(userData);
```

### 5. 关闭UI

```csharp
// 关闭指定实例
Framework.Core.GameEntry.UI.CloseUI(loginUI);

// 关闭第一个实例
Framework.Core.GameEntry.UI.CloseUI<LoginUIWindow>();

// 关闭所有实例
Framework.Core.GameEntry.UI.CloseAllUI<LoginUIWindow>();
```

### 6. 查询UI

```csharp
// 获取第一个实例
var loginUI = Framework.Core.GameEntry.UI.GetUI<LoginUIWindow>();

// 获取所有实例
var allLoginUIs = Framework.Core.GameEntry.UI.GetAllUI<LoginUIWindow>();

// 检查是否已打开
bool isOpened = Framework.Core.GameEntry.UI.IsUIOpened<LoginUIWindow>();

// 获取打开数量
int count = Framework.Core.GameEntry.UI.GetUICount<LoginUIWindow>();
```

### 7. UI栈管理（高级功能）

```csharp
// 返回上一个UI（关闭当前UI，显示上一个）
Framework.Core.GameEntry.UI.GoBack();

// 返回到指定UI类型（关闭该UI之后打开的所有UI）
// 例如：MainUI -> BagUI -> ItemDetailUI，返回到MainUI会关闭BagUI和ItemDetailUI
Framework.Core.GameEntry.UI.GoBackTo<MainUIWindow>();

// 返回到指定UI类型并关闭该UI
Framework.Core.GameEntry.UI.GoBackTo<MainUIWindow>(includeTarget: true);

// 返回到指定UI实例
var mainUI = Framework.Core.GameEntry.UI.GetUI<MainUIWindow>();
Framework.Core.GameEntry.UI.GoBackTo(mainUI);

// 关闭栈顶N个UI
Framework.Core.GameEntry.UI.GoBackCount(2); // 关闭最上面的2个UI

// 关闭所有UI并打开指定UI（清空UI栈，常用于切换场景）
await Framework.Core.GameEntry.UI.GoToUIAsync<MainUIWindow, MainUIView>();

// 获取UI栈深度
int depth = Framework.Core.GameEntry.UI.GetStackDepth();

// 获取栈顶UI
var topUI = Framework.Core.GameEntry.UI.GetTopUI<MainUIWindow>();

// 检查UI是否在栈中
bool inStack = Framework.Core.GameEntry.UI.IsUIInStack<BagUIWindow>();
```

### 8. 实际使用场景示例

#### 场景1：普通层+弹窗层，返回时关闭两者

```csharp
// 打开主界面（Normal层）
var mainUI = await GameEntry.UI.OpenUIAsync<MainUIWindow, MainUIView>();

// 打开背包界面（Normal层）
var bagUI = await GameEntry.UI.OpenUIAsync<BagUIWindow, BagUIView>();

// 打开物品详情弹窗（Popup层）
var itemDetailUI = await GameEntry.UI.OpenUIAsync<ItemDetailUIWindow, ItemDetailUIView>();

// 返回到主界面（会关闭背包和物品详情）
GameEntry.UI.GoBackTo<MainUIWindow>();

// 或者返回到主界面并关闭主界面
GameEntry.UI.GoBackTo<MainUIWindow>(includeTarget: true);
```

#### 场景2：多层弹窗，一次性关闭多个

```csharp
// 打开多个弹窗
await GameEntry.UI.OpenUIAsync<ConfirmUIWindow, ConfirmUIView>();
await GameEntry.UI.OpenUIAsync<TipUIWindow, TipUIView>();
await GameEntry.UI.OpenUIAsync<RewardUIWindow, RewardUIView>();

// 一次性关闭最上面的2个弹窗
GameEntry.UI.GoBackCount(2);
```

#### 场景3：切换场景，清空所有UI

```csharp
// 从游戏场景切换到主城场景
await GameEntry.UI.GoToUIAsync<MainCityUIWindow, MainCityUIView>();
// 这会关闭所有UI，然后打开主城UI
```

## 多实例支持

如果需要同时打开同一UI的多个实例（例如多个提示框），在注册时设置`allowMultiple = true`：

```csharp
Framework.Core.GameEntry.UI.RegisterUI<TipUIWindow>(
    "UI/TipUI",
    Framework.UILayer.Popup,
    true  // 允许多实例
);

// 可以多次打开
var tip1 = await Framework.Core.GameEntry.UI.OpenUIAsync<TipUIWindow, TipUIView>("提示1");
var tip2 = await Framework.Core.GameEntry.UI.OpenUIAsync<TipUIWindow, TipUIView>("提示2");
```

## 动态Canvas的优势

1. **自动管理** - 无需手动在场景中创建Canvas
2. **层级清晰** - 每个层级独立的Canvas，SortingOrder自动管理
3. **易于扩展** - 添加新层级只需修改UILayer枚举
4. **运行时创建** - 不占用场景资源，DontDestroyOnLoad自动管理生命周期

## 注意事项

1. UIView类只负责序列化组件，不要在其中写业务逻辑
2. UIWindow类继承UIBase<TView>，通过View属性访问组件
3. UI层级在注册时确定，打开时无需传参
4. 使用对象池可以提升性能，避免频繁创建销毁
