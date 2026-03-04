using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Framework.Core;

namespace Framework.HotUpdate
{
    /// <summary>
    /// 热更新管理器
    /// 负责版本检查、补丁下载、程序集加载和热更新启动
    /// </summary>
    public class HotUpdateManager : FrameworkComponent
    {
        private UpdateState _state = UpdateState.None;
        private Assembly _hotUpdateAssembly;
        private PatchDownloader _patchDownloader;
        private FileVerifier _fileVerifier;
        
        /// <summary>
        /// 当前更新状态
        /// </summary>
        public UpdateState State => _state;
        
        /// <summary>
        /// 更新可用事件
        /// </summary>
        public event Action<UpdateInfo> OnUpdateAvailable;
        
        /// <summary>
        /// 更新完成事件
        /// </summary>
        public event Action OnUpdateComplete;
        
        /// <summary>
        /// 更新错误事件
        /// </summary>
        public event Action<string> OnUpdateError;
        
        public override void OnInit()
        {
            base.OnInit();
            _patchDownloader = new PatchDownloader();
            _fileVerifier = new FileVerifier();
            Logger.Log("[HotUpdateManager] 热更新管理器初始化");
        }
        
        /// <summary>
        /// 检查更新
        /// </summary>
        /// <param name="updateUrl">更新服务器URL</param>
        /// <returns>更新信息</returns>
        public async UniTask<UpdateInfo> CheckUpdateAsync(string updateUrl)
        {
            _state = UpdateState.CheckingUpdate;
            Logger.Log($"[HotUpdateManager] 开始检查更新: {updateUrl}");
            
            try
            {
                // 获取本地版本
                UpdateInfo localVersion = VersionManager.GetLocalVersion();
                Logger.Log($"[HotUpdateManager] 本地版本: {localVersion.AppVersion}, 资源版本: {localVersion.ResourceVersion}, 代码版本: {localVersion.CodeVersion}");
                
                // 从服务器下载version.json
                string versionUrl = $"{updateUrl}/version.json";
                string tempPath = Path.Combine(Application.temporaryCachePath, "version_temp.json");
                
                bool downloadSuccess = await _patchDownloader.DownloadFileAsync(versionUrl, tempPath);
                
                if (!downloadSuccess)
                {
                    Logger.Error("[HotUpdateManager] 下载版本文件失败");
                    _state = UpdateState.Error;
                    OnUpdateError?.Invoke("下载版本文件失败");
                    return null;
                }
                
                // 解析版本信息
                string json = File.ReadAllText(tempPath);
                UpdateInfo serverVersion = JsonUtility.FromJson<UpdateInfo>(json);
                
                Logger.Log($"[HotUpdateManager] 服务器版本: {serverVersion.AppVersion}, 资源版本: {serverVersion.ResourceVersion}, 代码版本: {serverVersion.CodeVersion}");
                
                // 判断更新类型
                UpdateType updateType = VersionManager.DetermineUpdateType(localVersion, serverVersion);
                serverVersion.Type = updateType;
                
                // 检查版本兼容性
                if (!string.IsNullOrEmpty(serverVersion.MinCompatibleVersion))
                {
                    bool isCompatible = VersionManager.CheckCompatibility(localVersion.AppVersion, serverVersion.MinCompatibleVersion);
                    
                    if (!isCompatible)
                    {
                        Logger.Warning("[HotUpdateManager] 版本不兼容，需要强制更新");
                        serverVersion.ForceUpdate = true;
                        serverVersion.Type = UpdateType.FullUpdate;
                    }
                }
                
                // 触发更新可用事件
                if (updateType != UpdateType.None)
                {
                    OnUpdateAvailable?.Invoke(serverVersion);
                    Logger.Log($"[HotUpdateManager] 发现更新: {updateType}, 补丁数量: {serverVersion.PatchFiles.Count}");
                }
                else
                {
                    Logger.Log("[HotUpdateManager] 版本检查完成，无需更新");
                }
                
                _state = UpdateState.None;
                return serverVersion;
            }
            catch (Exception ex)
            {
                Logger.Error($"[HotUpdateManager] 检查更新失败: {ex.Message}");
                _state = UpdateState.Error;
                OnUpdateError?.Invoke(ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// 下载补丁
        /// </summary>
        /// <param name="updateInfo">更新信息</param>
        /// <param name="onProgress">进度回调</param>
        public async UniTask DownloadPatchAsync(UpdateInfo updateInfo, Action<float> onProgress = null)
        {
            _state = UpdateState.Downloading;
            
            if (updateInfo == null || updateInfo.PatchFiles == null || updateInfo.PatchFiles.Count == 0)
            {
                Logger.Log("[HotUpdateManager] 没有需要下载的补丁文件");
                _state = UpdateState.None;
                return;
            }
            
            Logger.Log($"[HotUpdateManager] 开始下载补丁，共{updateInfo.PatchFiles.Count}个文件");
            
            try
            {
                long totalSize = VersionManager.CalculateTotalSize(updateInfo.PatchFiles);
                long downloadedSize = 0;
                
                Logger.Log($"[HotUpdateManager] 总下载大小: {VersionManager.FormatFileSize(totalSize)}");
                
                // 下载目录
                string downloadDir = Application.persistentDataPath;
                
                // 下载每个补丁文件
                for (int i = 0; i < updateInfo.PatchFiles.Count; i++)
                {
                    PatchFile patchFile = updateInfo.PatchFiles[i];
                    string savePath = Path.Combine(downloadDir, patchFile.FileName);
                    
                    Logger.Log($"[HotUpdateManager] 下载文件 ({i + 1}/{updateInfo.PatchFiles.Count}): {patchFile.FileName}");
                    
                    // 下载文件
                    bool success = await _patchDownloader.DownloadFileAsync(patchFile.Url, savePath, progress =>
                    {
                        // 计算总进度
                        float fileProgress = progress * patchFile.Size;
                        float totalProgress = (downloadedSize + fileProgress) / totalSize;
                        onProgress?.Invoke(totalProgress);
                    });
                    
                    if (!success)
                    {
                        Logger.Error($"[HotUpdateManager] 下载文件失败: {patchFile.FileName}");
                        _state = UpdateState.Error;
                        OnUpdateError?.Invoke($"下载文件失败: {patchFile.FileName}");
                        return;
                    }
                    
                    // 校验文件
                    bool verified = await _fileVerifier.VerifyFileAsync(savePath, patchFile.MD5);
                    
                    if (!verified)
                    {
                        Logger.Error($"[HotUpdateManager] 文件校验失败: {patchFile.FileName}");
                        
                        // 删除损坏的文件
                        if (File.Exists(savePath))
                        {
                            File.Delete(savePath);
                        }
                        
                        _state = UpdateState.Error;
                        OnUpdateError?.Invoke($"文件校验失败: {patchFile.FileName}");
                        return;
                    }
                    
                    downloadedSize += patchFile.Size;
                }
                
                // 保存新版本信息到本地
                VersionManager.SaveLocalVersion(updateInfo);
                
                Logger.Log("[HotUpdateManager] 补丁下载完成");
                onProgress?.Invoke(1.0f);
                _state = UpdateState.Complete;
            }
            catch (Exception ex)
            {
                Logger.Error($"[HotUpdateManager] 下载补丁失败: {ex.Message}");
                _state = UpdateState.Error;
                OnUpdateError?.Invoke(ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// 加载热更新程序集
        /// 注意：此方法需要在HybridCLR安装后才能正常工作
        /// </summary>
        public void LoadHotUpdateAssembly()
        {
            Logger.Log("[HotUpdateManager] 开始加载热更新程序集");
            
            try
            {
#if !UNITY_EDITOR
                // 在真机上加载热更新DLL
                string dllPath = Path.Combine(Application.persistentDataPath, "HotUpdate.dll.bytes");
                
                if (!File.Exists(dllPath))
                {
                    Logger.Warning($"[HotUpdateManager] 热更新DLL不存在: {dllPath}，使用内置版本");
                    dllPath = Path.Combine(Application.streamingAssetsPath, "HotUpdate.dll.bytes");
                }
                
                byte[] dllBytes = File.ReadAllBytes(dllPath);
                _hotUpdateAssembly = Assembly.Load(dllBytes);
                
                Logger.Log($"[HotUpdateManager] 热更新程序集加载成功: {_hotUpdateAssembly.FullName}");
#else
                // 在编辑器中直接使用HotUpdate程序集
                _hotUpdateAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "HotUpdate");
                
                if (_hotUpdateAssembly == null)
                {
                    Logger.Error("[HotUpdateManager] 在编辑器中找不到HotUpdate程序集");
                    return;
                }
                
                Logger.Log($"[HotUpdateManager] 编辑器模式：使用HotUpdate程序集");
#endif
            }
            catch (Exception ex)
            {
                Logger.Error($"[HotUpdateManager] 加载热更新程序集失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 加载AOT泛型补充元数据
        /// 注意：此方法需要在HybridCLR安装后才能正常工作
        /// </summary>
        public void LoadMetadata()
        {
            Logger.Log("[HotUpdateManager] 开始加载AOT泛型补充元数据");
            
            try
            {
#if !UNITY_EDITOR
                // TODO: 加载AOT泛型补充元数据
                // HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes);
                
                Logger.Log("[HotUpdateManager] AOT泛型补充元数据加载完成");
#else
                Logger.Log("[HotUpdateManager] 编辑器模式：跳过元数据加载");
#endif
            }
            catch (Exception ex)
            {
                Logger.Error($"[HotUpdateManager] 加载元数据失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 启动热更新逻辑
        /// </summary>
        public void StartHotfix()
        {
            Logger.Log("[HotUpdateManager] 启动热更新逻辑");
            
            try
            {
                if (_hotUpdateAssembly == null)
                {
                    Logger.Error("[HotUpdateManager] 热更新程序集未加载，无法启动");
                    return;
                }
                
                // 通过反射创建HotfixEntry实例并调用Start方法
                Type entryType = _hotUpdateAssembly.GetType("HotUpdate.Entry.HotfixEntry");
                if (entryType == null)
                {
                    Logger.Error("[HotUpdateManager] 找不到HotfixEntry类型");
                    return;
                }
                
                object entryInstance = Activator.CreateInstance(entryType);
                MethodInfo startMethod = entryType.GetMethod("Start");
                
                if (startMethod == null)
                {
                    Logger.Error("[HotUpdateManager] 找不到Start方法");
                    return;
                }
                
                startMethod.Invoke(entryInstance, null);
                
                Logger.Log("[HotUpdateManager] 热更新逻辑启动成功");
                OnUpdateComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error($"[HotUpdateManager] 启动热更新逻辑失败: {ex.Message}");
                OnUpdateError?.Invoke(ex.Message);
                throw;
            }
        }
        
        public override void OnShutdown()
        {
            base.OnShutdown();
            _hotUpdateAssembly = null;
            Logger.Log("[HotUpdateManager] 热更新管理器关闭");
        }
    }
    
    /// <summary>
    /// 更新状态
    /// </summary>
    public enum UpdateState
    {
        None,              // 无状态
        CheckingUpdate,    // 检查更新中
        Downloading,       // 下载中
        Installing,        // 安装中
        Complete,          // 完成
        Error              // 错误
    }
}
