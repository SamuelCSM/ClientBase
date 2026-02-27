using System;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// UI窗口基类
    /// 所有UI逻辑类都应该继承此类
    /// 负责UI的业务逻辑处理，不继承MonoBehaviour
    /// </summary>
    /// <typeparam name="TView">对应的UIView类型</typeparam>
    public abstract class UIBase<TView> where TView : UIView
    {
        // UI视图（序列化组件的容器）
        protected TView View { get; private set; }
        
        // UI GameObject
        protected GameObject GameObject { get; private set; }
        
        // UI Transform
        protected Transform Transform => GameObject?.transform;
        
        // UI层级（在注册时确定，不可更改）
        private UILayer _layer;
        
        // UI数据
        private object _userData;
        
        // 是否已初始化
        private bool _isInitialized;
        
        // 是否已打开
        private bool _isOpened;

        /// <summary>
        /// UI层级
        /// </summary>
        public UILayer Layer => _layer;

        /// <summary>
        /// UI是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// UI是否已打开
        /// </summary>
        public bool IsOpened => _isOpened;

        /// <summary>
        /// UI用户数据
        /// </summary>
        public object UserData => _userData;

        /// <summary>
        /// 初始化UI（仅调用一次）
        /// </summary>
        internal void Initialize(UILayer layer, TView view, GameObject gameObject)
        {
            if (_isInitialized)
            {
                return;
            }

            _layer = layer;
            View = view;
            GameObject = gameObject;
            _isInitialized = true;

            OnInit();
        }

        /// <summary>
        /// 打开UI
        /// </summary>
        /// <param name="userData">用户数据</param>
        internal void Open(object userData = null)
        {
            if (!_isInitialized)
            {
                Logger.Error($"UIBase.Open: UI未初始化 - {GetType().Name}");
                return;
            }

            _userData = userData;
            _isOpened = true;

            GameObject.SetActive(true);
            OnOpen(userData);
        }

        /// <summary>
        /// 关闭UI
        /// </summary>
        internal void Close()
        {
            if (!_isOpened)
            {
                return;
            }

            _isOpened = false;

            OnClose();
            GameObject.SetActive(false);
        }

        /// <summary>
        /// 销毁UI
        /// </summary>
        internal void Destroy()
        {
            if (_isOpened)
            {
                Close();
            }

            OnDestroy();
            
            // 清理引用
            View = null;
            GameObject = null;
        }

        #region 生命周期方法（子类重写）

        /// <summary>
        /// 初始化（仅调用一次）
        /// 用于查找组件、注册事件等
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// 打开UI
        /// 用于刷新UI数据、播放动画等
        /// </summary>
        /// <param name="userData">用户数据</param>
        protected virtual void OnOpen(object userData)
        {
        }

        /// <summary>
        /// 关闭UI
        /// 用于清理临时数据、停止动画等
        /// </summary>
        protected virtual void OnClose()
        {
        }

        /// <summary>
        /// 销毁UI
        /// 用于注销事件、释放资源等
        /// </summary>
        protected virtual void OnDestroy()
        {
        }

        #endregion

        #region 辅助方法（已废弃，建议使用View直接访问序列化组件）

        // 注意：以下方法已不推荐使用
        // 建议在UIView中序列化所需组件，然后通过View属性访问
        // 例如：View.btnClose.onClick.AddListener(OnCloseClick);

        #endregion

        #region 数据绑定辅助方法

        /// <summary>
        /// 获取强类型用户数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <returns>用户数据</returns>
        protected T GetUserData<T>() where T : class
        {
            return _userData as T;
        }

        #endregion
    }
}
