using UnityEngine;
using System.Collections.Generic;

namespace Framework.Core
{
    using Framework;
    /// <summary>
    /// 游戏入口单例
    /// 作为框架的唯一入口点，管理所有Manager的生命周期
    /// </summary>
    public class GameEntry : MonoSingleton<GameEntry>
    {
        // 所有框架组件列表
        private List<FrameworkComponent> _components = new List<FrameworkComponent>();

        // ==================== Manager 声明 ====================
        // 注意：这些Manager将在后续任务中逐步实现
        // 目前先声明接口，实际实例化会在各Manager实现后进行

        /// <summary>
        /// 资源管理器
        /// </summary>
        public static ResourceManager Resource { get; private set; }

        /// <summary>
        /// UI管理器
        /// </summary>
        public static Framework.UIManager UI { get; private set; }

        /// <summary>
        /// 网络管理器
        /// </summary>
        public static Framework.NetworkManager Network { get; private set; }

        /// <summary>
        /// 数据管理器
        /// </summary>
        public static DataManager Data { get; private set; }

        /// <summary>
        /// 事件管理器
        /// </summary>
        public static EventManager Event { get; private set; }

        /// <summary>
        /// 音频管理器
        /// </summary>
        public static AudioManager Audio { get; private set; }

        /// <summary>
        /// 场景管理器
        /// </summary>
        public static SceneManager Scene { get; private set; }

        /// <summary>
        /// 定时器管理器
        /// </summary>
        public static TimerManager Timer { get; private set; }

        /// <summary>
        /// 热更新管理器
        /// </summary>
        public static HotUpdateManager HotUpdate { get; private set; }

        // ==================== 生命周期方法 ====================

        /// <summary>
        /// 初始化（按依赖顺序初始化所有Manager）
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            Debug.Log("[GameEntry] 开始初始化框架...");

            // 按照依赖顺序初始化Manager
            // 注意：实际的Manager实例化会在各Manager类实现后进行
            // 目前这里只是预留初始化逻辑的位置

            // TODO: 在后续任务中，按以下顺序初始化Manager：
            // 1. Event（事件系统，最基础，其他模块可能依赖）
            // 2. Timer（定时器，基础功能）
            // 3. Resource（资源管理，UI/Audio/Scene依赖）
            // 4. Data（数据管理）
            // 5. Network（网络通信）
            // 6. UI（UI管理，依赖Resource）
            // 7. Audio（音频管理，依赖Resource）
            // 8. Scene（场景管理，依赖Resource）
            // 9. HotUpdate（热更新，最后初始化）

            InitializeManagers();

            Debug.Log("[GameEntry] 框架初始化完成");
        }

        /// <summary>
        /// 初始化所有Manager
        /// </summary>
        private void InitializeManagers()
        {
            // 注意：这里的初始化顺序很重要，需要按照依赖关系排序
            
            // 已实现的Manager
            Event = AddComponent<EventManager>();
            Timer = AddComponent<TimerManager>();
            Resource = AddComponent<ResourceManager>();
            UI = AddComponent<Framework.UIManager>();
            Network = AddComponent<Framework.NetworkManager>();

            // 待实现的Manager（当实现后取消注释）
            // Data = AddComponent<DataManager>();
            // Audio = AddComponent<AudioManager>();
            // Scene = AddComponent<SceneManager>();
            // HotUpdate = AddComponent<HotUpdateManager>();

            Debug.Log("[GameEntry] 所有Manager初始化完成");
        }

        /// <summary>
        /// 添加框架组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例</returns>
        private T AddComponent<T>() where T : FrameworkComponent, new()
        {
            T component = new T();
            _components.Add(component);
            component.OnInit();
            Debug.Log($"[GameEntry] 初始化组件: {typeof(T).Name}");
            return component;
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            
            // 调用所有组件的Update
            for (int i = 0; i < _components.Count; i++)
            {
                _components[i].OnUpdate(deltaTime);
            }
        }

        /// <summary>
        /// 延迟更新
        /// </summary>
        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            
            // 调用所有组件的LateUpdate
            for (int i = 0; i < _components.Count; i++)
            {
                _components[i].OnLateUpdate(deltaTime);
            }
        }

        /// <summary>
        /// 固定更新
        /// </summary>
        private void FixedUpdate()
        {
            float fixedDeltaTime = Time.fixedDeltaTime;
            
            // 调用所有组件的FixedUpdate
            for (int i = 0; i < _components.Count; i++)
            {
                _components[i].OnFixedUpdate(fixedDeltaTime);
            }
        }

        /// <summary>
        /// 应用退出时清理
        /// </summary>
        protected override void OnApplicationQuit()
        {
            Debug.Log("[GameEntry] 开始清理框架...");

            // 反向顺序关闭所有组件
            for (int i = _components.Count - 1; i >= 0; i--)
            {
                _components[i].OnShutdown();
                Debug.Log($"[GameEntry] 关闭组件: {_components[i].GetType().Name}");
            }

            _components.Clear();

            Debug.Log("[GameEntry] 框架清理完成");

            base.OnApplicationQuit();
        }
    }

    // ==================== Manager 占位类 ====================
    // 这些类将在后续任务中实现，目前只是占位以避免编译错误

    /// <summary>
    /// 数据管理器（占位）
    /// </summary>
    public class DataManager : FrameworkComponent
    {
    }

    /// <summary>
    /// 音频管理器（占位）
    /// </summary>
    public class AudioManager : FrameworkComponent
    {
    }

    /// <summary>
    /// 场景管理器（占位）
    /// </summary>
    public class SceneManager : FrameworkComponent
    {
    }

    /// <summary>
    /// 热更新管理器（占位）
    /// </summary>
    public class HotUpdateManager : FrameworkComponent
    {
    }
}
