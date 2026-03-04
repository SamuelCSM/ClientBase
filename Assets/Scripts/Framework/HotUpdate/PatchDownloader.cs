using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace Framework.HotUpdate
{
    /// <summary>
    /// 补丁下载器
    /// 负责从服务器下载补丁文件，支持断点续传
    /// </summary>
    public class PatchDownloader
    {
        private UnityWebRequest _currentRequest;
        private bool _isCancelled;
        private long _downloadedSize;
        private long _totalSize;
        
        /// <summary>
        /// 已下载大小
        /// </summary>
        public long DownloadedSize => _downloadedSize;
        
        /// <summary>
        /// 总大小
        /// </summary>
        public long TotalSize => _totalSize;
        
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url">下载URL</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="onProgress">进度回调(0-1)</param>
        /// <returns>是否下载成功</returns>
        public async UniTask<bool> DownloadFileAsync(string url, string savePath, Action<float> onProgress = null)
        {
            _isCancelled = false;
            _downloadedSize = 0;
            
            try
            {
                Logger.Log($"[PatchDownloader] 开始下载: {url} -> {savePath}");
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // 检查是否支持断点续传
                long startPosition = 0;
                if (File.Exists(savePath))
                {
                    FileInfo fileInfo = new FileInfo(savePath);
                    startPosition = fileInfo.Length;
                    _downloadedSize = startPosition;
                    Logger.Log($"[PatchDownloader] 检测到已下载 {startPosition} 字节，尝试断点续传");
                }
                
                // 创建下载请求
                _currentRequest = UnityWebRequest.Get(url);
                
                // 设置断点续传
                if (startPosition > 0)
                {
                    _currentRequest.SetRequestHeader("Range", $"bytes={startPosition}-");
                }
                
                // 开始下载
                var operation = _currentRequest.SendWebRequest();
                
                // 等待下载完成，同时报告进度
                while (!operation.isDone)
                {
                    if (_isCancelled)
                    {
                        _currentRequest.Abort();
                        Logger.Warning("[PatchDownloader] 下载已取消");
                        return false;
                    }
                    
                    float progress = operation.progress;
                    onProgress?.Invoke(progress);
                    
                    await UniTask.Yield();
                }
                
                // 检查下载结果
                if (_currentRequest.result != UnityWebRequest.Result.Success)
                {
                    Logger.Error($"[PatchDownloader] 下载失败: {_currentRequest.error}");
                    return false;
                }
                
                // 保存文件
                byte[] data = _currentRequest.downloadHandler.data;
                
                if (startPosition > 0)
                {
                    // 断点续传，追加写入
                    using (FileStream fs = new FileStream(savePath, FileMode.Append, FileAccess.Write))
                    {
                        fs.Write(data, 0, data.Length);
                    }
                }
                else
                {
                    // 全新下载，覆盖写入
                    File.WriteAllBytes(savePath, data);
                }
                
                _downloadedSize = new FileInfo(savePath).Length;
                _totalSize = _downloadedSize;
                
                Logger.Log($"[PatchDownloader] 下载完成: {savePath} ({_downloadedSize} 字节)");
                onProgress?.Invoke(1.0f);
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"[PatchDownloader] 下载异常: {ex.Message}");
                return false;
            }
            finally
            {
                _currentRequest?.Dispose();
                _currentRequest = null;
            }
        }
        
        /// <summary>
        /// 取消下载
        /// </summary>
        public void CancelDownload()
        {
            _isCancelled = true;
            Logger.Log("[PatchDownloader] 请求取消下载");
        }
        
        /// <summary>
        /// 获取已下载大小
        /// </summary>
        public long GetDownloadedSize()
        {
            return _downloadedSize;
        }
        
        /// <summary>
        /// 获取总大小
        /// </summary>
        public long GetTotalSize()
        {
            return _totalSize;
        }
    }
}
