using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

namespace Framework
{
    /// <summary>
    /// 场景管理器
    /// 管理场景的加载、卸载和切换，支持Addressables场景加载
    /// </summary>
    public class SceneManager : Core.FrameworkComponent
    {
        /// <summary>
        /// 当前场景名称
        /// </summary>
        private string _currentScene;

        /// <summary>
        /// 已加载场景的句柄缓存（场景名 -> 句柄）
        /// </summary>
        private System.Collections.Generic.Dictionary<string, AsyncOperationHandle<SceneInstance>> _loadedScenes 
            = new System.Collections.Generic.Dictionary<string, AsyncOperationHandle<SceneInstance>>();

        /// <summary>
        /// 预加载的场景句柄缓存
        /// </summary>
        private System.Collections.Generic.Dictionary<string, AsyncOperationHandle<SceneInstance>> _preloadedScenes 
            = new System.Collections.Generic.Dictionary<string, AsyncOperationHandle<SceneInstance>>();

        #region 事件

        /// <summary>
        /// 场景加载完成事件
        /// </summary>
        public event Action<string> OnSceneLoaded;

        /// <summary>
        /// 场景卸载完成事件
        /// </summary>
        public event Action<string> OnSceneUnloaded;

        #endregion

        #region 属性

        /// <summary>
        /// 当前场景名称
        /// </summary>
        public string CurrentScene => _currentScene;

        #endregion

        #region 生命周期

        public override void OnInit()
        {
            // 获取当前激活的场景
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            _currentScene = activeScene.name;
            
            Logger.Log($"[SceneManager] 初始化完成，当前场景: {_currentScene}");
        }

        public override void OnShutdown()
        {
            // 清理已加载的场景
            foreach (var kvp in _loadedScenes)
            {
                if (kvp.Value.IsValid())
                {
                    Addressables.UnloadSceneAsync(kvp.Value);
                }
            }
            _loadedScenes.Clear();

            // 清理预加载的场景
            foreach (var kvp in _preloadedScenes)
            {
                if (kvp.Value.IsValid())
                {
                    Addressables.UnloadSceneAsync(kvp.Value);
                }
            }
            _preloadedScenes.Clear();

            // 清空事件
            OnSceneLoaded = null;
            OnSceneUnloaded = null;

            Logger.Log("[SceneManager] 场景管理器已关闭");
        }

        #endregion

        #region 场景加载

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneName">场景名称或地址</param>
        /// <param name="onProgress">进度回调（0-1）</param>
        /// <param name="loadMode">加载模式（Single=替换当前场景，Additive=叠加场景）</param>
        public async UniTask LoadSceneAsync(
            string sceneName, 
            Action<float> onProgress = null,
            LoadSceneMode loadMode = LoadSceneMode.Single)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Logger.Error("[SceneManager] LoadSceneAsync: 场景名称不能为空");
                return;
            }

            Logger.Log($"[SceneManager] 开始加载场景: {sceneName}");

            try
            {
                AsyncOperationHandle<SceneInstance> handle;

                // 检查是否有预加载的场景
                if (_preloadedScenes.TryGetValue(sceneName, out var preloadedHandle))
                {
                    // 使用预加载的场景
                    Logger.Log($"[SceneManager] 使用预加载的场景: {sceneName}");
                    
                    // 激活预加载的场景
                    var activateOp = preloadedHandle.Result.ActivateAsync();
                    
                    while (!activateOp.isDone)
                    {
                        onProgress?.Invoke(activateOp.progress);
                        await UniTask.Yield();
                    }

                    handle = preloadedHandle;
                    _preloadedScenes.Remove(sceneName);
                }
                else
                {
                    // 正常加载场景
                    handle = Addressables.LoadSceneAsync(sceneName, loadMode);

                    while (!handle.IsDone)
                    {
                        onProgress?.Invoke(handle.PercentComplete);
                        await UniTask.Yield();
                    }

                    if (handle.Status != AsyncOperationStatus.Succeeded)
                    {
                        Logger.Error($"[SceneManager] 加载场景失败: {sceneName}");
                        return;
                    }
                }

                // 保存场景句柄
                _loadedScenes[sceneName] = handle;

                // 更新当前场景
                if (loadMode == LoadSceneMode.Single)
                {
                    _currentScene = sceneName;
                }

                onProgress?.Invoke(1f);
                Logger.Log($"[SceneManager] 场景加载完成: {sceneName}");

                // 触发场景加载事件
                OnSceneLoaded?.Invoke(sceneName);
            }
            catch (Exception ex)
            {
                Logger.Error($"[SceneManager] 加载场景异常: {sceneName}, 错误: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 场景卸载

        /// <summary>
        /// 异步卸载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public async UniTask UnloadSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Logger.Error("[SceneManager] UnloadSceneAsync: 场景名称不能为空");
                return;
            }

            // 不能卸载当前唯一的场景
            if (sceneName == _currentScene && UnityEngine.SceneManagement.SceneManager.sceneCount == 1)
            {
                Logger.Warning($"[SceneManager] 不能卸载唯一的场景: {sceneName}");
                return;
            }

            // 检查场景是否通过此管理器加载
            if (!_loadedScenes.TryGetValue(sceneName, out var handle))
            {
                Logger.Warning($"[SceneManager] 场景未通过SceneManager加载，尝试使用Unity原生方式卸载: {sceneName}");
                
                // 尝试使用Unity原生方式卸载
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
                if (scene.isLoaded)
                {
                    var unloadOp = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                    while (!unloadOp.isDone)
                    {
                        await UniTask.Yield();
                    }
                }
                return;
            }

            Logger.Log($"[SceneManager] 开始卸载场景: {sceneName}");

            try
            {
                // 卸载场景
                var unloadHandle = Addressables.UnloadSceneAsync(handle);
                await unloadHandle.Task;

                if (unloadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logger.Error($"[SceneManager] 卸载场景失败: {sceneName}");
                    return;
                }

                // 从缓存中移除
                _loadedScenes.Remove(sceneName);

                Logger.Log($"[SceneManager] 场景卸载完成: {sceneName}");

                // 触发场景卸载事件
                OnSceneUnloaded?.Invoke(sceneName);

                // 清理资源
                await Resources.UnloadUnusedAssets();
                System.GC.Collect();
            }
            catch (Exception ex)
            {
                Logger.Error($"[SceneManager] 卸载场景异常: {sceneName}, 错误: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 场景预加载

        /// <summary>
        /// 预加载场景（不激活）
        /// </summary>
        /// <param name="sceneName">场景名称或地址</param>
        public async UniTask PreloadSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Logger.Error("[SceneManager] PreloadSceneAsync: 场景名称不能为空");
                return;
            }

            // 检查是否已经预加载
            if (_preloadedScenes.ContainsKey(sceneName))
            {
                Logger.Warning($"[SceneManager] 场景已预加载: {sceneName}");
                return;
            }

            Logger.Log($"[SceneManager] 开始预加载场景: {sceneName}");

            try
            {
                // 加载场景但不激活
                var handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive, false);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Logger.Error($"[SceneManager] 预加载场景失败: {sceneName}");
                    return;
                }

                // 缓存预加载的场景句柄
                _preloadedScenes[sceneName] = handle;

                Logger.Log($"[SceneManager] 场景预加载完成: {sceneName}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[SceneManager] 预加载场景异常: {sceneName}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 取消预加载的场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void CancelPreloadScene(string sceneName)
        {
            if (_preloadedScenes.TryGetValue(sceneName, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.UnloadSceneAsync(handle);
                }
                _preloadedScenes.Remove(sceneName);
                Logger.Log($"[SceneManager] 取消预加载场景: {sceneName}");
            }
        }

        /// <summary>
        /// 检查场景是否已预加载
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <returns>如果已预加载返回true</returns>
        public bool IsScenePreloaded(string sceneName)
        {
            return _preloadedScenes.ContainsKey(sceneName);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取已加载的场景数量
        /// </summary>
        public int GetLoadedSceneCount()
        {
            return UnityEngine.SceneManagement.SceneManager.sceneCount;
        }

        /// <summary>
        /// 获取指定索引的场景名称
        /// </summary>
        public string GetSceneNameAt(int index)
        {
            if (index < 0 || index >= UnityEngine.SceneManagement.SceneManager.sceneCount)
            {
                return null;
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(index);
            return scene.name;
        }

        /// <summary>
        /// 检查场景是否已加载
        /// </summary>
        public bool IsSceneLoaded(string sceneName)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            return scene.isLoaded;
        }

        #endregion
    }
}
