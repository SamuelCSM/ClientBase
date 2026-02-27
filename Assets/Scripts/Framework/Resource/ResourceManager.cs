using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

namespace Framework
{
	/// <summary>
	/// 资源管理器
	/// 封装 Addressables，提供统一的资源加载接口和引用计数管理
	/// </summary>
	public class ResourceManager : Core.FrameworkComponent
    {
        // 资源引用计数字典
        private Dictionary<string, int> _referenceCount = new Dictionary<string, int>();
        
        // 资源句柄缓存
        private Dictionary<string, AsyncOperationHandle> _handleCache = new Dictionary<string, AsyncOperationHandle>();
        
        // 实例化对象到地址的映射
        private Dictionary<GameObject, string> _instanceToAddress = new Dictionary<GameObject, string>();

        #region 生命周期

        public override void OnInit()
        {
            Logger.Log("ResourceManager 初始化");
        }

        public override void OnUpdate(float deltaTime)
        {
            // 资源管理器不需要每帧更新
        }

        public override void OnShutdown()
        {
            // 清理所有资源
            foreach (var handle in _handleCache.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            
            _handleCache.Clear();
            _referenceCount.Clear();
            _instanceToAddress.Clear();
            
            Logger.Log("ResourceManager 关闭");
        }

        #endregion

        #region 异步资源加载

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <returns>加载的资源</returns>
        public async UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Logger.Error("LoadAssetAsync: address 不能为空");
                return null;
            }

            try
            {
                // 检查缓存
                if (_handleCache.TryGetValue(address, out var cachedHandle))
                {
                    // 增加引用计数
                    AddReference(address);
                    return cachedHandle.Result as T;
                }

                // 加载资源
                var handle = Addressables.LoadAssetAsync<T>(address);
                await handle.Task;
                T asset = handle.Result;

                if (asset == null)
                {
                    Logger.Error($"LoadAssetAsync: 加载资源失败 - {address}");
                    return null;
                }

                // 缓存句柄
                _handleCache[address] = handle;
                
                // 初始化引用计数
                AddReference(address);

                Logger.Log($"LoadAssetAsync: 加载资源成功 - {address}");
                return asset;
            }
            catch (Exception e)
            {
                Logger.Error($"LoadAssetAsync: 加载资源异常 - {address}, 错误: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 异步加载资源（带进度回调）
        /// </summary>
        public async UniTask<T> LoadAssetAsync<T>(string address, Action<float> onProgress) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Logger.Error("LoadAssetAsync: address 不能为空");
                return null;
            }

            try
            {
                // 检查缓存
                if (_handleCache.TryGetValue(address, out var cachedHandle))
                {
                    AddReference(address);
                    onProgress?.Invoke(1f);
                    return cachedHandle.Result as T;
                }

                // 加载资源
                var handle = Addressables.LoadAssetAsync<T>(address);
                
                // 监听进度
                while (!handle.IsDone)
                {
                    onProgress?.Invoke(handle.PercentComplete);
                    await UniTask.Yield();
                }

                T asset = handle.Result;
                if (asset == null)
                {
                    Logger.Error($"LoadAssetAsync: 加载资源失败 - {address}");
                    return null;
                }

                _handleCache[address] = handle;
                AddReference(address);
                onProgress?.Invoke(1f);

                Logger.Log($"LoadAssetAsync: 加载资源成功 - {address}");
                return asset;
            }
            catch (Exception e)
            {
                Logger.Error($"LoadAssetAsync: 加载资源异常 - {address}, 错误: {e.Message}");
                return null;
            }
        }

        #endregion

        #region 同步资源加载

        /// <summary>
        /// 同步加载资源（仅用于已预加载的资源）
        /// </summary>
        public T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Logger.Error("LoadAsset: address 不能为空");
                return null;
            }

            // 检查缓存
            if (_handleCache.TryGetValue(address, out var cachedHandle))
            {
                AddReference(address);
                return cachedHandle.Result as T;
            }

            Logger.Warning($"LoadAsset: 资源未预加载，建议使用 LoadAssetAsync - {address}");
            
            // 同步加载（会阻塞主线程，不推荐）
            var handle = Addressables.LoadAssetAsync<T>(address);
            T asset = handle.WaitForCompletion();

            if (asset != null)
            {
                _handleCache[address] = handle;
                AddReference(address);
            }

            return asset;
        }

        #endregion

        #region GameObject 实例化

        /// <summary>
        /// 异步实例化 GameObject
        /// </summary>
        public async UniTask<GameObject> InstantiateAsync(string address, Transform parent = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                Logger.Error("InstantiateAsync: address 不能为空");
                return null;
            }

            try
            {
                var handle = Addressables.InstantiateAsync(address, parent);
                await handle.Task;
                GameObject instance = handle.Result;

                if (instance == null)
                {
                    Logger.Error($"InstantiateAsync: 实例化失败 - {address}");
                    return null;
                }

                // 记录实例到地址的映射
                _instanceToAddress[instance] = address;
                
                // 增加引用计数
                AddReference(address);

                Logger.Log($"InstantiateAsync: 实例化成功 - {address}");
                return instance;
            }
            catch (Exception e)
            {
                Logger.Error($"InstantiateAsync: 实例化异常 - {address}, 错误: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 异步实例化 GameObject（指定位置和旋转）
        /// </summary>
        public async UniTask<GameObject> InstantiateAsync(
            string address, 
            Vector3 position, 
            Quaternion rotation, 
            Transform parent = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                Logger.Error("InstantiateAsync: address 不能为空");
                return null;
            }

            try
            {
                var handle = Addressables.InstantiateAsync(address, position, rotation, parent);
                await handle.Task;
                GameObject instance = handle.Result;

                if (instance == null)
                {
                    Logger.Error($"InstantiateAsync: 实例化失败 - {address}");
                    return null;
                }

                _instanceToAddress[instance] = address;
                AddReference(address);

                Logger.Log($"InstantiateAsync: 实例化成功 - {address}");
                return instance;
            }
            catch (Exception e)
            {
                Logger.Error($"InstantiateAsync: 实例化异常 - {address}, 错误: {e.Message}");
                return null;
            }
        }

        #endregion

        #region 资源预加载

        /// <summary>
        /// 预加载资源列表
        /// </summary>
        public async UniTask PreloadAssetsAsync(List<string> addresses, Action<float> onProgress = null)
        {
            if (addresses == null || addresses.Count == 0)
            {
                Logger.Warning("PreloadAssetsAsync: 预加载列表为空");
                return;
            }

            int totalCount = addresses.Count;
            int loadedCount = 0;

            foreach (var address in addresses)
            {
                await LoadAssetAsync<UnityEngine.Object>(address);
                loadedCount++;
                onProgress?.Invoke((float)loadedCount / totalCount);
            }

            Logger.Log($"PreloadAssetsAsync: 预加载完成，共 {totalCount} 个资源");
        }

        /// <summary>
        /// 通过标签预加载资源
        /// </summary>
        public async UniTask PreloadAssetsByLabelAsync(string label, Action<float> onProgress = null)
        {
            if (string.IsNullOrEmpty(label))
            {
                Logger.Error("PreloadAssetsByLabelAsync: label 不能为空");
                return;
            }

            try
            {
                var handle = Addressables.LoadAssetsAsync<UnityEngine.Object>(label, null);
                
                while (!handle.IsDone)
                {
                    onProgress?.Invoke(handle.PercentComplete);
                    await UniTask.Yield();
                }

                var assets = handle.Result;
                Logger.Log($"PreloadAssetsByLabelAsync: 预加载标签 '{label}' 完成，共 {assets.Count} 个资源");
                
                onProgress?.Invoke(1f);
            }
            catch (Exception e)
            {
                Logger.Error($"PreloadAssetsByLabelAsync: 预加载标签异常 - {label}, 错误: {e.Message}");
            }
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源
        /// </summary>
        public void ReleaseAsset(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            // 减少引用计数
            if (!RemoveReference(address))
            {
                return;
            }

            // 引用计数为 0，释放资源
            if (_handleCache.TryGetValue(address, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                
                _handleCache.Remove(address);
                Logger.Log($"ReleaseAsset: 释放资源 - {address}");
            }
        }

        /// <summary>
        /// 释放实例化的 GameObject
        /// </summary>
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            // 查找对应的地址
            if (_instanceToAddress.TryGetValue(instance, out var address))
            {
                _instanceToAddress.Remove(instance);
                RemoveReference(address);
                
                // 释放实例
                Addressables.ReleaseInstance(instance);
                
                Logger.Log($"ReleaseInstance: 释放实例 - {address}");
            }
            else
            {
                Logger.Warning($"ReleaseInstance: 实例不是通过 ResourceManager 创建的");
                UnityEngine.Object.Destroy(instance);
            }
        }

        #endregion

        #region 引用计数管理

        /// <summary>
        /// 增加引用计数
        /// </summary>
        private void AddReference(string address)
        {
            if (_referenceCount.ContainsKey(address))
            {
                _referenceCount[address]++;
            }
            else
            {
                _referenceCount[address] = 1;
            }
        }

        /// <summary>
        /// 减少引用计数
        /// </summary>
        /// <returns>引用计数是否为 0</returns>
        private bool RemoveReference(string address)
        {
            if (!_referenceCount.ContainsKey(address))
            {
                return false;
            }

            _referenceCount[address]--;
            
            if (_referenceCount[address] <= 0)
            {
                _referenceCount.Remove(address);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取资源引用计数
        /// </summary>
        public int GetReferenceCount(string address)
        {
            return _referenceCount.TryGetValue(address, out var count) ? count : 0;
        }

        #endregion

        #region 调试信息

        /// <summary>
        /// 打印所有已加载资源的信息
        /// </summary>
        public void PrintLoadedAssets()
        {
            Logger.Log("=== 已加载资源列表 ===");
            foreach (var kvp in _referenceCount)
            {
                Logger.Log($"  {kvp.Key} - 引用计数: {kvp.Value}");
            }
            Logger.Log($"总计: {_referenceCount.Count} 个资源");
        }

        #endregion
    }
}
