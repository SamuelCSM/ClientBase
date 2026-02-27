using UnityEngine;

namespace Framework.Core
{
    /// <summary>
    /// MonoBehaviour单例基类（支持DontDestroyOnLoad）
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[MonoSingleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).ToString() + " (Singleton)";

                            DontDestroyOnLoad(singletonObject);
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Awake生命周期（子类重写时必须调用base.Awake()）
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[MonoSingleton] Instance of '{typeof(T)}' already exists. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 应用退出时标记
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        /// <summary>
        /// 销毁时清理实例
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// 销毁单例实例
        /// </summary>
        public static void DestroyInstance()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}
