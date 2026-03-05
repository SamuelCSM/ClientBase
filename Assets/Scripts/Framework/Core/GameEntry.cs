using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

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
        /// 配置管理器
        /// </summary>
        public static Data.ConfigManager RefData { get; private set; }

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
        public static Framework.SceneManager Scene { get; private set; }

        /// <summary>
        /// 定时器管理器
        /// </summary>
        public static TimerManager Timer { get; private set; }

        /// <summary>
        /// 热更新管理器
        /// </summary>
        public static HotUpdate.HotUpdateManager HotUpdate { get; private set; }

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
        /// 游戏启动流程
        /// 在所有Manager初始化完成后执行
        /// </summary>
        private async void Start()
        {
            Debug.Log("[GameEntry] 开始游戏启动流程...");

            try
            {
                // 1. 检查本地版本
                Debug.Log("[GameEntry] 步骤1: 检查本地版本");
                Framework.HotUpdate.UpdateInfo localVersion = Framework.HotUpdate.VersionManager.GetLocalVersion();
                Debug.Log($"[GameEntry] 本地版本: {localVersion.AppVersion}, 资源版本: {localVersion.ResourceVersion}, 代码版本: {localVersion.CodeVersion}");

                // 2. 检查更新（如果配置了更新服务器URL）
                string updateUrl = GetUpdateServerUrl();

                if (!string.IsNullOrEmpty(updateUrl))
                {
                    Debug.Log($"[GameEntry] 步骤2: 检查更新 - {updateUrl}");

                    Framework.HotUpdate.UpdateInfo serverVersion = await HotUpdate.CheckUpdateAsync(updateUrl);

                    if (serverVersion != null && serverVersion.Type != Framework.HotUpdate.UpdateType.None)
                    {
                        Debug.Log($"[GameEntry] 发现更新: {serverVersion.Type}");

                        // 如果是强制更新，提示用户
                        if (serverVersion.ForceUpdate)
                        {
                            Debug.Log("[GameEntry] 需要强制更新");
                            // TODO: 显示强制更新UI，引导用户下载新版本
                            // 这里暂时跳过，实际项目中应该显示UI并阻止继续
                        }

                        // 3. 下载补丁文件
                        if (serverVersion.Type == Framework.HotUpdate.UpdateType.HotUpdate)
                        {
                            Debug.Log("[GameEntry] 步骤3: 下载补丁文件");

                            await HotUpdate.DownloadPatchAsync(serverVersion, progress =>
                            {
                                Debug.Log($"[GameEntry] 下载进度: {progress * 100:F2}%");
                                // TODO: 更新下载进度UI
                            });

                            Debug.Log("[GameEntry] 补丁下载完成");
                        }
                    }
                    else
                    {
                        Debug.Log("[GameEntry] 无需更新，使用本地版本");
                    }
                }
                else
                {
                    Debug.Log("[GameEntry] 步骤2: 跳过更新检查（未配置更新服务器）");
                }

                // 4. 加载HybridCLR程序集
                Debug.Log("[GameEntry] 步骤4: 加载热更新程序集");
                HotUpdate.LoadHotUpdateAssembly();

                // 5. 加载AOT泛型补充元数据
                Debug.Log("[GameEntry] 步骤5: 加载AOT泛型补充元数据");
                HotUpdate.LoadMetadata();

                // 6. 调用热更新入口
                Debug.Log("[GameEntry] 步骤6: 启动热更新逻辑");
                HotUpdate.StartHotfix();

                Debug.Log("[GameEntry] 游戏启动流程完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameEntry] 游戏启动流程失败: {ex.Message}");
                Debug.LogError($"[GameEntry] 堆栈跟踪: {ex.StackTrace}");

                // TODO: 显示错误UI，提示用户重启或重新安装
                // 这里暂时只记录错误
            }
        }

        /// <summary>
        /// 获取更新服务器URL
        /// 可以从配置文件、PlayerPrefs或其他地方读取
        /// </summary>
        /// <returns>更新服务器URL，如果未配置则返回null</returns>
        private string GetUpdateServerUrl()
        {
            // TODO: 从配置文件或其他地方读取更新服务器URL
            // 这里暂时返回空，表示不检查更新
            // 实际项目中应该从配置文件读取，例如：
            // return PlayerPrefs.GetString("UpdateServerUrl", "");
            // 或者从Resources中的配置文件读取

            // 示例URL（实际使用时需要替换为真实的服务器地址）
            // return "http://your-update-server.com/updates";

            return string.Empty;
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
            UI = AddComponent<UIManager>();
            Network = AddComponent<NetworkManager>();
            RefData = AddComponent<Data.ConfigManager>();
            Audio = AddComponent<AudioManager>();

            // 待实现的Manager（当实现后取消注释）
            Scene = AddComponent<Framework.SceneManager>();
            HotUpdate = AddComponent<HotUpdate.HotUpdateManager>();

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
}
