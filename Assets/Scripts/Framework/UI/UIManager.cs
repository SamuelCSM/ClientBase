using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Framework
{
    /// <summary>
    /// UI注册信息
    /// </summary>
    public class UIRegisterInfo
    {
        /// <summary>
        /// UI类型
        /// </summary>
        public Type UIType { get; set; }
        
        /// <summary>
        /// Addressables地址
        /// </summary>
        public string Address { get; set; }
        
        /// <summary>
        /// UI层级
        /// </summary>
        public UILayer Layer { get; set; }
        
        /// <summary>
        /// 是否允许多实例（默认false）
        /// </summary>
        public bool AllowMultiple { get; set; }
    }

    /// <summary>
    /// UI管理器
    /// 负责UI的加载、显示、隐藏、销毁等管理
    /// </summary>
    public class UIManager : Core.FrameworkComponent
    {
        // UI根节点
        private Transform _uiRoot;
        
        // 各层级的Canvas
        private readonly Dictionary<UILayer, Canvas> _layerCanvases = new Dictionary<UILayer, Canvas>();
        
        // UI注册信息字典（类型 -> 注册信息）
        private readonly Dictionary<Type, UIRegisterInfo> _uiRegisterInfos = new Dictionary<Type, UIRegisterInfo>();
        
        // 已打开的UI字典（类型 -> UI实例列表）
        // 注意：这里存储的是UIBase实例，不是MonoBehaviour
        private readonly Dictionary<Type, List<object>> _openedUIs = new Dictionary<Type, List<object>>();
        
        // UI GameObject字典（UIBase实例 -> GameObject）
        private readonly Dictionary<object, GameObject> _uiGameObjects = new Dictionary<object, GameObject>();
        
        // UI栈（用于返回上一个UI）
        private readonly Stack<object> _uiStack = new Stack<object>();
        
        // UI对象池字典（类型 -> 对象池）
        private readonly Dictionary<Type, GameObjectPool> _uiPools = new Dictionary<Type, GameObjectPool>();

        #region 生命周期

        public override void OnInit()
        {
            CreateUIRoot();
            CreateLayerCanvases();
            Logger.Log("UIManager 初始化");
        }

        public override void OnUpdate(float deltaTime)
        {
            // UI管理器不需要每帧更新
        }

        public override void OnShutdown()
        {
            // 关闭所有UI
            CloseAllUI();
            
            // 清空UI对象池
            foreach (var pool in _uiPools.Values)
            {
                pool.Clear();
            }
            _uiPools.Clear();
            
            // 清空数据
            _openedUIs.Clear();
            _uiGameObjects.Clear();
            _uiStack.Clear();
            _uiRegisterInfos.Clear();
            
            Logger.Log("UIManager 关闭");
        }

        #endregion

        #region UI根节点和Canvas创建

        /// <summary>
        /// 创建UI根节点
        /// </summary>
        private void CreateUIRoot()
        {
            GameObject uiRootObj = new GameObject("UIRoot");
            GameObject.DontDestroyOnLoad(uiRootObj);
            _uiRoot = uiRootObj.transform;
            
            // 添加Canvas Scaler
            Canvas rootCanvas = uiRootObj.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            UnityEngine.UI.CanvasScaler scaler = uiRootObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            uiRootObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        /// <summary>
        /// 创建各层级的Canvas
        /// </summary>
        private void CreateLayerCanvases()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                CreateLayerCanvas(layer);
            }
        }

        /// <summary>
        /// 创建指定层级的Canvas
        /// </summary>
        /// <param name="layer">UI层级</param>
        private void CreateLayerCanvas(UILayer layer)
        {
            GameObject canvasObj = new GameObject($"Canvas_{layer}");
            canvasObj.transform.SetParent(_uiRoot);
            canvasObj.transform.localPosition = Vector3.zero;
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = Vector3.one;
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = (int)layer * 100; // 每个层级间隔100
            
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // 设置RectTransform
            RectTransform rectTransform = canvasObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            _layerCanvases[layer] = canvas;
        }

        #endregion

        #region UI注册

        /// <summary>
        /// 注册UI
        /// </summary>
        /// <typeparam name="T">UI类型（UIBase子类）</typeparam>
        /// <param name="address">Addressables地址</param>
        /// <param name="layer">UI层级</param>
        /// <param name="allowMultiple">是否允许多实例（默认false）</param>
        public void RegisterUI<T>(string address, UILayer layer, bool allowMultiple = false)
        {
            Type uiType = typeof(T);
            if (_uiRegisterInfos.ContainsKey(uiType))
            {
                Logger.Warning($"UIManager.RegisterUI: UI已注册 - {uiType.Name}");
                return;
            }
            
            _uiRegisterInfos[uiType] = new UIRegisterInfo
            {
                UIType = uiType,
                Address = address,
                Layer = layer,
                AllowMultiple = allowMultiple
            };
            
            Logger.Log($"UIManager.RegisterUI: 注册UI - {uiType.Name}, Layer: {layer}, AllowMultiple: {allowMultiple}");
        }

        #endregion

        #region UI打开/关闭

        /// <summary>
        /// 打开UI
        /// </summary>
        /// <typeparam name="TWindow">UI窗口类型</typeparam>
        /// <typeparam name="TView">UI视图类型</typeparam>
        /// <param name="userData">用户数据</param>
        /// <param name="usePool">是否使用对象池（默认true）</param>
        /// <returns>UI实例</returns>
        public async UniTask<TWindow> OpenUIAsync<TWindow, TView>(object userData = null, bool usePool = true) 
            where TWindow : UIBase<TView>, new()
            where TView : UIView
        {
            Type uiType = typeof(TWindow);
            
            // 获取注册信息
            if (!_uiRegisterInfos.TryGetValue(uiType, out var registerInfo))
            {
                Logger.Error($"UIManager.OpenUIAsync: UI未注册 - {uiType.Name}");
                return null;
            }
            
            // 检查是否允许多实例
            if (!registerInfo.AllowMultiple && _openedUIs.ContainsKey(uiType) && _openedUIs[uiType].Count > 0)
            {
                Logger.Warning($"UIManager.OpenUIAsync: UI已打开且不允许多实例 - {uiType.Name}");
                return _openedUIs[uiType][0] as TWindow;
            }
            
            // 加载UI GameObject
            GameObject uiObj = null;
            if (usePool)
            {
                uiObj = await GetUIFromPool(uiType, registerInfo.Address, registerInfo.Layer);
            }
            else
            {
                uiObj = await Core.GameEntry.Resource.InstantiateAsync(registerInfo.Address, GetLayerCanvas(registerInfo.Layer).transform);
            }
            
            if (uiObj == null)
            {
                Logger.Error($"UIManager.OpenUIAsync: 加载UI失败 - {uiType.Name}");
                return null;
            }
            
            // 获取UI视图组件
            TView view = uiObj.GetComponent<TView>();
            if (view == null)
            {
                Logger.Error($"UIManager.OpenUIAsync: UI视图组件不存在 - {typeof(TView).Name}");
                GameObject.Destroy(uiObj);
                return null;
            }
            
            // 创建UI窗口实例（纯C#对象）
            TWindow window = new TWindow();
            
            // 初始化和打开UI
            window.Initialize(registerInfo.Layer, view, uiObj);
            window.Open(userData);
            
            // 添加到已打开列表
            if (!_openedUIs.ContainsKey(uiType))
            {
                _openedUIs[uiType] = new List<object>();
            }
            _openedUIs[uiType].Add(window);
            
            // 记录GameObject映射
            _uiGameObjects[window] = uiObj;
            
            // 添加到UI栈
            _uiStack.Push(window);
            
            Logger.Log($"UIManager.OpenUIAsync: 打开UI - {uiType.Name}");
            return window;
        }

        /// <summary>
        /// 关闭UI（关闭指定实例）
        /// </summary>
        /// <param name="ui">UI实例</param>
        /// <param name="destroy">是否销毁（默认false，回收到对象池）</param>
        public void CloseUI(object ui, bool destroy = false)
        {
            if (ui == null)
            {
                return;
            }
            
            Type uiType = ui.GetType();
            
            // 从已打开列表移除
            if (_openedUIs.TryGetValue(uiType, out var uiList))
            {
                uiList.Remove(ui);
                if (uiList.Count == 0)
                {
                    _openedUIs.Remove(uiType);
                }
            }
            
            // 从UI栈移除
            if (_uiStack.Count > 0 && _uiStack.Peek() == ui)
            {
                _uiStack.Pop();
            }
            
            // 获取GameObject
            if (!_uiGameObjects.TryGetValue(ui, out var uiObj))
            {
                Logger.Error($"UIManager.CloseUI: 找不到UI对应的GameObject - {uiType.Name}");
                return;
            }
            
            // 调用关闭方法
            var closeMethod = uiType.GetMethod("Close", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            closeMethod?.Invoke(ui, null);
            
            // 销毁或回收
            if (destroy)
            {
                var destroyMethod = uiType.GetMethod("Destroy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                destroyMethod?.Invoke(ui, null);
                GameObject.Destroy(uiObj);
            }
            else
            {
                ReleaseUIToPool(uiType, uiObj);
            }
            
            // 移除GameObject映射
            _uiGameObjects.Remove(ui);
            
            Logger.Log($"UIManager.CloseUI: 关闭UI - {uiType.Name}");
        }

        /// <summary>
        /// 关闭UI（关闭指定类型的第一个实例）
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="destroy">是否销毁（默认false，回收到对象池）</param>
        public void CloseUI<T>(bool destroy = false)
        {
            Type uiType = typeof(T);
            if (_openedUIs.TryGetValue(uiType, out var uiList) && uiList.Count > 0)
            {
                CloseUI(uiList[0], destroy);
            }
        }

        /// <summary>
        /// 关闭UI（关闭指定类型的所有实例）
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="destroy">是否销毁（默认false，回收到对象池）</param>
        public void CloseAllUI<T>(bool destroy = false)
        {
            Type uiType = typeof(T);
            if (_openedUIs.TryGetValue(uiType, out var uiList))
            {
                // 复制列表，避免在遍历时修改
                var uiListCopy = new List<object>(uiList);
                foreach (var ui in uiListCopy)
                {
                    CloseUI(ui, destroy);
                }
            }
        }

        /// <summary>
        /// 关闭所有UI
        /// </summary>
        public void CloseAllUI()
        {
            List<Type> uiTypes = new List<Type>(_openedUIs.Keys);
            foreach (var uiType in uiTypes)
            {
                if (_openedUIs.TryGetValue(uiType, out var uiList))
                {
                    var uiListCopy = new List<object>(uiList);
                    foreach (var ui in uiListCopy)
                    {
                        CloseUI(ui, true);
                    }
                }
            }
            
            _uiStack.Clear();
        }

        /// <summary>
        /// 返回上一个UI
        /// </summary>
        public void GoBack()
        {
            if (_uiStack.Count <= 1)
            {
                Logger.Warning("UIManager.GoBack: UI栈为空或只有一个UI");
                return;
            }
            
            // 关闭当前UI
            object currentUI = _uiStack.Pop();
            CloseUI(currentUI, false);
            
            // 显示上一个UI
            if (_uiStack.Count > 0)
            {
                object previousUI = _uiStack.Peek();
                if (_uiGameObjects.TryGetValue(previousUI, out var previousObj))
                {
                    previousObj.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 返回到指定UI（关闭该UI之后打开的所有UI）
        /// </summary>
        /// <typeparam name="T">目标UI类型</typeparam>
        /// <param name="includeTarget">是否也关闭目标UI（默认false）</param>
        public void GoBackTo<T>(bool includeTarget = false)
        {
            Type targetType = typeof(T);
            
            // 从栈顶开始查找目标UI
            List<object> uisToClose = new List<object>();
            bool found = false;
            
            foreach (var ui in _uiStack)
            {
                if (ui.GetType() == targetType)
                {
                    found = true;
                    if (includeTarget)
                    {
                        uisToClose.Add(ui);
                    }
                    break;
                }
                uisToClose.Add(ui);
            }
            
            if (!found)
            {
                Logger.Warning($"UIManager.GoBackTo: 目标UI不在栈中 - {targetType.Name}");
                return;
            }
            
            // 关闭所有需要关闭的UI
            foreach (var ui in uisToClose)
            {
                _uiStack.Pop();
                CloseUI(ui, false);
            }
            
            // 显示栈顶UI
            if (_uiStack.Count > 0)
            {
                object topUI = _uiStack.Peek();
                if (_uiGameObjects.TryGetValue(topUI, out var topObj))
                {
                    topObj.SetActive(true);
                }
            }
            
            Logger.Log($"UIManager.GoBackTo: 返回到UI - {targetType.Name}, 关闭了{uisToClose.Count}个UI");
        }

        /// <summary>
        /// 返回到指定UI实例（关闭该实例之后打开的所有UI）
        /// </summary>
        /// <param name="targetUI">目标UI实例</param>
        /// <param name="includeTarget">是否也关闭目标UI（默认false）</param>
        public void GoBackTo(object targetUI, bool includeTarget = false)
        {
            if (targetUI == null)
            {
                Logger.Error("UIManager.GoBackTo: 目标UI实例为null");
                return;
            }
            
            // 从栈顶开始查找目标UI
            List<object> uisToClose = new List<object>();
            bool found = false;
            
            foreach (var ui in _uiStack)
            {
                if (ui == targetUI)
                {
                    found = true;
                    if (includeTarget)
                    {
                        uisToClose.Add(ui);
                    }
                    break;
                }
                uisToClose.Add(ui);
            }
            
            if (!found)
            {
                Logger.Warning($"UIManager.GoBackTo: 目标UI实例不在栈中 - {targetUI.GetType().Name}");
                return;
            }
            
            // 关闭所有需要关闭的UI
            foreach (var ui in uisToClose)
            {
                _uiStack.Pop();
                CloseUI(ui, false);
            }
            
            // 显示栈顶UI
            if (_uiStack.Count > 0)
            {
                object topUI = _uiStack.Peek();
                if (_uiGameObjects.TryGetValue(topUI, out var topObj))
                {
                    topObj.SetActive(true);
                }
            }
            
            Logger.Log($"UIManager.GoBackTo: 返回到UI实例 - {targetUI.GetType().Name}, 关闭了{uisToClose.Count}个UI");
        }

        /// <summary>
        /// 关闭栈顶N个UI
        /// </summary>
        /// <param name="count">要关闭的UI数量</param>
        public void GoBackCount(int count)
        {
            if (count <= 0)
            {
                Logger.Warning("UIManager.GoBackCount: count必须大于0");
                return;
            }
            
            int actualCount = Mathf.Min(count, _uiStack.Count - 1); // 至少保留一个UI
            
            for (int i = 0; i < actualCount; i++)
            {
                if (_uiStack.Count <= 1)
                {
                    break;
                }
                
                object ui = _uiStack.Pop();
                CloseUI(ui, false);
            }
            
            // 显示栈顶UI
            if (_uiStack.Count > 0)
            {
                object topUI = _uiStack.Peek();
                if (_uiGameObjects.TryGetValue(topUI, out var topObj))
                {
                    topObj.SetActive(true);
                }
            }
            
            Logger.Log($"UIManager.GoBackCount: 关闭了{actualCount}个UI");
        }

        /// <summary>
        /// 关闭所有UI并打开指定UI（清空UI栈）
        /// </summary>
        /// <typeparam name="TWindow">UI窗口类型</typeparam>
        /// <typeparam name="TView">UI视图类型</typeparam>
        /// <param name="userData">用户数据</param>
        /// <returns>UI实例</returns>
        public async UniTask<TWindow> GoToUIAsync<TWindow, TView>(object userData = null)
            where TWindow : UIBase<TView>, new()
            where TView : UIView
        {
            // 关闭所有UI
            CloseAllUI();
            
            // 打开新UI
            return await OpenUIAsync<TWindow, TView>(userData);
        }

        /// <summary>
        /// 获取UI栈深度
        /// </summary>
        /// <returns>栈深度</returns>
        public int GetStackDepth()
        {
            return _uiStack.Count;
        }

        /// <summary>
        /// 获取栈顶UI
        /// </summary>
        /// <returns>栈顶UI实例，栈为空则返回null</returns>
        public object GetTopUI()
        {
            return _uiStack.Count > 0 ? _uiStack.Peek() : null;
        }

        /// <summary>
        /// 获取栈顶UI（泛型版本）
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <returns>栈顶UI实例，类型不匹配或栈为空则返回null</returns>
        public T GetTopUI<T>() where T : class
        {
            if (_uiStack.Count > 0)
            {
                object topUI = _uiStack.Peek();
                return topUI as T;
            }
            return null;
        }

        /// <summary>
        /// 检查UI是否在栈中
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <returns>是否在栈中</returns>
        public bool IsUIInStack<T>()
        {
            Type targetType = typeof(T);
            foreach (var ui in _uiStack)
            {
                if (ui.GetType() == targetType)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查UI实例是否在栈中
        /// </summary>
        /// <param name="targetUI">目标UI实例</param>
        /// <returns>是否在栈中</returns>
        public bool IsUIInStack(object targetUI)
        {
            if (targetUI == null)
            {
                return false;
            }
            
            foreach (var ui in _uiStack)
            {
                if (ui == targetUI)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region UI查询

        /// <summary>
        /// 获取UI实例（获取第一个实例）
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <returns>UI实例，未打开则返回null</returns>
        public T GetUI<T>()
        {
            Type uiType = typeof(T);
            if (_openedUIs.TryGetValue(uiType, out var uiList) && uiList.Count > 0)
            {
                return (T)uiList[0];
            }
            return default(T);
        }

        /// <summary>
        /// 获取UI所有实例
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <returns>UI实例列表</returns>
        public List<T> GetAllUI<T>()
        {
            Type uiType = typeof(T);
            if (_openedUIs.TryGetValue(uiType, out var uiList))
            {
                List<T> result = new List<T>();
                foreach (var ui in uiList)
                {
                    result.Add((T)ui);
                }
                return result;
            }
            return new List<T>();
        }

        /// <summary>
        /// 检查UI是否已打开
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <returns>是否已打开</returns>
        public bool IsUIOpened<T>()
        {
            Type uiType = typeof(T);
            return _openedUIs.ContainsKey(uiType) && _openedUIs[uiType].Count > 0;
        }

        /// <summary>
        /// 获取UI打开数量
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <returns>打开数量</returns>
        public int GetUICount<T>()
        {
            Type uiType = typeof(T);
            if (_openedUIs.TryGetValue(uiType, out var uiList))
            {
                return uiList.Count;
            }
            return 0;
        }

        #endregion

        #region UI对象池

        /// <summary>
        /// 从对象池获取UI
        /// </summary>
        private async UniTask<GameObject> GetUIFromPool(Type uiType, string address, UILayer layer)
        {
            if (!_uiPools.TryGetValue(uiType, out var pool))
            {
                pool = new GameObjectPool(address, GetLayerCanvas(layer).transform);
                _uiPools[uiType] = pool;
            }
            
            return await pool.GetAsync();
        }

        /// <summary>
        /// 回收UI到对象池
        /// </summary>
        private void ReleaseUIToPool(Type uiType, GameObject uiObj)
        {
            if (_uiPools.TryGetValue(uiType, out var pool))
            {
                pool.Release(uiObj);
            }
            else
            {
                GameObject.Destroy(uiObj);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取指定层级的Canvas
        /// </summary>
        private Canvas GetLayerCanvas(UILayer layer)
        {
            return _layerCanvases[layer];
        }

        #endregion
    }
}
