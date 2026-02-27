using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Framework.Core;

namespace Framework
{
    /// <summary>
    /// 定时器管理器（基于 UniTaskHelper 实现）
    /// 提供一次性定时器和循环定时器功能，支持暂停/恢复/取消和时间缩放
    /// </summary>
    public class TimerManager : FrameworkComponent
    {
        // 定时器字典（定时器ID -> 定时器）
        private Dictionary<int, TimerInfo> _timers = new Dictionary<int, TimerInfo>();

        // 下一个定时器ID
        private int _nextTimerId = 1;

        // 管理器的取消令牌源（用于关闭时取消所有定时器）
        private CancellationTokenSource _managerCts;

        /// <summary>
        /// 初始化
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();
            _managerCts = new CancellationTokenSource();
            Logger.Log("[TimerManager] 定时器管理器初始化完成");
        }

        /// <summary>
        /// 关闭清理
        /// </summary>
        public override void OnShutdown()
        {
            CancelAllTimers();
            _managerCts?.Cancel();
            _managerCts?.Dispose();
            _managerCts = null;
            Logger.Log("[TimerManager] 定时器管理器已关闭");
            base.OnShutdown();
        }

        #region 创建定时器

        /// <summary>
        /// 添加一次性定时器
        /// </summary>
        /// <param name="onComplete">完成回调</param>
        /// <param name="duration">持续时间（秒）</param>
        /// <param name="useRealTime">是否使用真实时间（不受 Time.timeScale 影响）</param>
        /// <returns>定时器ID</returns>
        public int AddTimer(Action onComplete, float duration, bool useRealTime = false)
        {
            if (duration <= 0)
            {
                Logger.Warning($"[TimerManager] 定时器持续时间必须大于0，当前值: {duration}");
                return -1;
            }

            if (onComplete == null)
            {
                Logger.Warning("[TimerManager] 定时器回调不能为 null");
                return -1;
            }

            int timerId = _nextTimerId++;

            // 创建定时器专用的取消令牌源
            var timerCts = CancellationTokenSource.CreateLinkedTokenSource(_managerCts.Token);

            TimerInfo timer = new TimerInfo
            {
                Id = timerId,
                Interval = duration,
                Callback = onComplete,
                UseRealTime = useRealTime,
                IsLoop = false,
                LoopCount = 1,
                CurrentLoop = 0,
                StartTime = Time.time,
                CancellationTokenSource = timerCts,
                IsPaused = false,
                PausedElapsedTime = 0f
            };

            _timers[timerId] = timer;

            // 使用 UniTaskHelper 启动定时器
            RunOneShotTimerAsync(timer);

            Logger.Debug($"[TimerManager] 添加一次性定时器，ID: {timerId}, 持续时间: {duration}秒");

            return timerId;
        }

        /// <summary>
        /// 添加循环定时器
        /// </summary>
        /// <param name="onTick">每次触发的回调</param>
        /// <param name="interval">间隔时间（秒）</param>
        /// <param name="loopCount">循环次数（-1 表示无限循环）</param>
        /// <param name="useRealTime">是否使用真实时间（不受 Time.timeScale 影响）</param>
        /// <returns>定时器ID</returns>
        public int AddLoopTimer(Action onTick, float interval, int loopCount = -1, bool useRealTime = false)
        {
            if (interval <= 0)
            {
                Logger.Warning($"[TimerManager] 定时器间隔时间必须大于0，当前值: {interval}");
                return -1;
            }

            if (onTick == null)
            {
                Logger.Warning("[TimerManager] 定时器回调不能为 null");
                return -1;
            }

            int timerId = _nextTimerId++;

            // 创建定时器专用的取消令牌源
            var timerCts = CancellationTokenSource.CreateLinkedTokenSource(_managerCts.Token);

            TimerInfo timer = new TimerInfo
            {
                Id = timerId,
                Interval = interval,
                Callback = onTick,
                UseRealTime = useRealTime,
                IsLoop = true,
                LoopCount = loopCount,
                CurrentLoop = 0,
                StartTime = Time.time,
                CancellationTokenSource = timerCts,
                IsPaused = false,
                PausedElapsedTime = 0f
            };

            _timers[timerId] = timer;

            // 使用 UniTaskHelper 启动定时器
            if (loopCount < 0)
            {
                // 无限循环
                RunInfiniteLoopTimerAsync(timer);
            }
            else
            {
                // 有限循环
                RunFiniteLoopTimerAsync(timer);
            }

            string loopInfo = loopCount < 0 ? "无限" : loopCount.ToString();
            Logger.Debug($"[TimerManager] 添加循环定时器，ID: {timerId}, 间隔: {interval}秒, 循环次数: {loopInfo}");

            return timerId;
        }

        #endregion

        #region 定时器执行逻辑（使用 UniTaskHelper）

        /// <summary>
        /// 运行一次性定时器（使用 UniTaskHelper）
        /// </summary>
        private void RunOneShotTimerAsync(TimerInfo timer)
        {
            UniTaskHelper.RunDelayActionSeconds(() =>
            {
                // 检查定时器是否仍然存在（可能已被取消）
                if (!_timers.ContainsKey(timer.Id))
                    return;

                // 触发回调
                try
                {
                    timer.Callback?.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.Error($"[TimerManager] 定时器回调异常，ID: {timer.Id}, 错误: {ex.Message}");
                }

                // 清理定时器
                RemoveTimer(timer.Id);

            }, timer.Interval, timer.UseRealTime, timer.CancellationTokenSource.Token);
        }

        /// <summary>
        /// 运行有限循环定时器（使用 UniTaskHelper）
        /// </summary>
        private void RunFiniteLoopTimerAsync(TimerInfo timer)
        {
            UniTaskHelper.RunEveryIntervalCount((count) =>
            {
                // 检查定时器是否仍然存在（可能已被取消）
                if (!_timers.ContainsKey(timer.Id))
                    return;

                // 更新循环计数
                timer.CurrentLoop = count + 1;

                // 触发回调
                try
                {
                    timer.Callback?.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.Error($"[TimerManager] 定时器回调异常，ID: {timer.Id}, 错误: {ex.Message}");
                }

                // 如果是最后一次循环，清理定时器
                if (count + 1 >= timer.LoopCount)
                {
                    RemoveTimer(timer.Id);
                }

            }, timer.Interval, timer.LoopCount, timer.UseRealTime, timer.CancellationTokenSource.Token);
        }

        /// <summary>
        /// 运行无限循环定时器（使用 UniTaskHelper）
        /// </summary>
        private void RunInfiniteLoopTimerAsync(TimerInfo timer)
        {
            UniTaskHelper.RunEveryInterval(() =>
            {
                // 检查定时器是否仍然存在（可能已被取消）
                if (!_timers.ContainsKey(timer.Id))
                    return;

                // 更新循环计数
                timer.CurrentLoop++;

                // 触发回调
                try
                {
                    timer.Callback?.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.Error($"[TimerManager] 定时器回调异常，ID: {timer.Id}, 错误: {ex.Message}");
                }

            }, timer.Interval, timer.UseRealTime, timer.CancellationTokenSource.Token);
        }

        #endregion

        #region 控制定时器

        /// <summary>
        /// 暂停定时器
        /// 注意：由于使用 UniTaskHelper，暂停功能通过取消并重新创建定时器实现
        /// </summary>
        /// <param name="timerId">定时器ID</param>
        public void PauseTimer(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerInfo timer))
            {
                if (!timer.IsPaused)
                {
                    // 计算已过时间
                    float currentTime = timer.UseRealTime ? Time.realtimeSinceStartup : Time.time;
                    timer.PausedElapsedTime = currentTime - timer.StartTime;
                    
                    // 取消当前定时器
                    timer.CancellationTokenSource?.Cancel();
                    timer.CancellationTokenSource?.Dispose();
                    
                    // 标记为暂停
                    timer.IsPaused = true;
                    
                    Logger.Debug($"[TimerManager] 暂停定时器，ID: {timerId}, 已过时间: {timer.PausedElapsedTime}秒");
                }
            }
            else
            {
                Logger.Warning($"[TimerManager] 暂停定时器失败，定时器不存在，ID: {timerId}");
            }
        }

        /// <summary>
        /// 恢复定时器
        /// </summary>
        /// <param name="timerId">定时器ID</param>
        public void ResumeTimer(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerInfo timer))
            {
                if (timer.IsPaused)
                {
                    // 创建新的取消令牌源
                    timer.CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_managerCts.Token);
                    
                    // 计算剩余时间
                    float remainingTime = timer.Interval - timer.PausedElapsedTime;
                    
                    // 标记为未暂停
                    timer.IsPaused = false;
                    timer.StartTime = timer.UseRealTime ? Time.realtimeSinceStartup : Time.time;
                    timer.StartTime -= timer.PausedElapsedTime;
                    
                    // 重新启动定时器
                    if (timer.IsLoop)
                    {
                        // 先执行剩余时间，然后继续循环
                        ResumeLoopTimer(timer, remainingTime);
                    }
                    else
                    {
                        // 一次性定时器，执行剩余时间
                        ResumeOneShotTimer(timer, remainingTime);
                    }
                    
                    Logger.Debug($"[TimerManager] 恢复定时器，ID: {timerId}, 剩余时间: {remainingTime}秒");
                }
            }
            else
            {
                Logger.Warning($"[TimerManager] 恢复定时器失败，定时器不存在，ID: {timerId}");
            }
        }

        /// <summary>
        /// 恢复一次性定时器
        /// </summary>
        private void ResumeOneShotTimer(TimerInfo timer, float remainingTime)
        {
            UniTaskHelper.RunDelayActionSeconds(() =>
            {
                if (!_timers.ContainsKey(timer.Id))
                    return;

                try
                {
                    timer.Callback?.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.Error($"[TimerManager] 定时器回调异常，ID: {timer.Id}, 错误: {ex.Message}");
                }

                RemoveTimer(timer.Id);

            }, remainingTime, timer.UseRealTime, timer.CancellationTokenSource.Token);
        }

        /// <summary>
        /// 恢复循环定时器
        /// </summary>
        private void ResumeLoopTimer(TimerInfo timer, float remainingTime)
        {
            // 先执行剩余时间的第一次触发
            UniTaskHelper.RunDelayActionSeconds(() =>
            {
                if (!_timers.ContainsKey(timer.Id))
                    return;

                try
                {
                    timer.Callback?.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.Error($"[TimerManager] 定时器回调异常，ID: {timer.Id}, 错误: {ex.Message}");
                }

                timer.CurrentLoop++;

                // 检查是否达到循环次数限制
                if (timer.LoopCount > 0 && timer.CurrentLoop >= timer.LoopCount)
                {
                    RemoveTimer(timer.Id);
                    return;
                }

                // 继续后续循环
                timer.StartTime = timer.UseRealTime ? Time.realtimeSinceStartup : Time.time;
                timer.PausedElapsedTime = 0f;

                if (timer.LoopCount < 0)
                {
                    RunInfiniteLoopTimerAsync(timer);
                }
                else
                {
                    int remainingLoops = timer.LoopCount - timer.CurrentLoop;
                    timer.LoopCount = remainingLoops;
                    timer.CurrentLoop = 0;
                    RunFiniteLoopTimerAsync(timer);
                }

            }, remainingTime, timer.UseRealTime, timer.CancellationTokenSource.Token);
        }

        /// <summary>
        /// 取消定时器
        /// </summary>
        /// <param name="timerId">定时器ID</param>
        public void CancelTimer(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerInfo timer))
            {
                timer.CancellationTokenSource?.Cancel();
                RemoveTimer(timerId);
                Logger.Debug($"[TimerManager] 取消定时器，ID: {timerId}");
            }
            else
            {
                Logger.Warning($"[TimerManager] 取消定时器失败，定时器不存在，ID: {timerId}");
            }
        }

        /// <summary>
        /// 取消所有定时器
        /// </summary>
        public void CancelAllTimers()
        {
            int count = _timers.Count;
            
            foreach (var timer in _timers.Values)
            {
                timer.CancellationTokenSource?.Cancel();
                timer.CancellationTokenSource?.Dispose();
            }
            
            _timers.Clear();
            Logger.Debug($"[TimerManager] 取消所有定时器，共 {count} 个");
        }

        /// <summary>
        /// 移除定时器（内部方法）
        /// </summary>
        private void RemoveTimer(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerInfo timer))
            {
                timer.CancellationTokenSource?.Dispose();
                _timers.Remove(timerId);
            }
        }

        #endregion

        #region 查询定时器

        /// <summary>
        /// 获取定时器剩余时间
        /// </summary>
        /// <param name="timerId">定时器ID</param>
        /// <returns>剩余时间（秒），定时器不存在返回 -1</returns>
        public float GetRemainingTime(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerInfo timer))
            {
                if (timer.IsPaused)
                {
                    return timer.Interval - timer.PausedElapsedTime;
                }

                float currentTime = timer.UseRealTime ? Time.realtimeSinceStartup : Time.time;
                float elapsed = currentTime - timer.StartTime;
                return Mathf.Max(0f, timer.Interval - elapsed);
            }
            return -1f;
        }

        /// <summary>
        /// 获取定时器进度（0-1）
        /// </summary>
        /// <param name="timerId">定时器ID</param>
        /// <returns>进度值（0-1），定时器不存在返回 -1</returns>
        public float GetProgress(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerInfo timer))
            {
                if (timer.IsPaused)
                {
                    return Mathf.Clamp01(timer.PausedElapsedTime / timer.Interval);
                }

                float currentTime = timer.UseRealTime ? Time.realtimeSinceStartup : Time.time;
                float elapsed = currentTime - timer.StartTime;
                return Mathf.Clamp01(elapsed / timer.Interval);
            }
            return -1f;
        }

        /// <summary>
        /// 检查定时器是否正在运行
        /// </summary>
        /// <param name="timerId">定时器ID</param>
        /// <returns>是否正在运行</returns>
        public bool IsTimerRunning(int timerId)
        {
            return _timers.ContainsKey(timerId) && !_timers[timerId].IsPaused;
        }

        /// <summary>
        /// 检查定时器是否存在
        /// </summary>
        /// <param name="timerId">定时器ID</param>
        /// <returns>是否存在</returns>
        public bool HasTimer(int timerId)
        {
            return _timers.ContainsKey(timerId);
        }

        /// <summary>
        /// 检查定时器是否暂停
        /// </summary>
        /// <param name="timerId">定时器ID</param>
        /// <returns>是否暂停</returns>
        public bool IsTimerPaused(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerInfo timer))
            {
                return timer.IsPaused;
            }
            return false;
        }

        /// <summary>
        /// 获取当前定时器数量
        /// </summary>
        /// <returns>定时器数量</returns>
        public int GetTimerCount()
        {
            return _timers.Count;
        }

        #endregion
    }

    /// <summary>
    /// 定时器信息
    /// </summary>
    internal class TimerInfo
    {
        /// <summary>
        /// 定时器ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 间隔时间（秒）
        /// </summary>
        public float Interval { get; set; }

        /// <summary>
        /// 回调函数
        /// </summary>
        public Action Callback { get; set; }

        /// <summary>
        /// 是否使用真实时间（不受 Time.timeScale 影响）
        /// </summary>
        public bool UseRealTime { get; set; }

        /// <summary>
        /// 是否循环
        /// </summary>
        public bool IsLoop { get; set; }

        /// <summary>
        /// 循环次数（-1 表示无限循环）
        /// </summary>
        public int LoopCount { get; set; }

        /// <summary>
        /// 当前循环次数
        /// </summary>
        public int CurrentLoop { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public float StartTime { get; set; }

        /// <summary>
        /// 暂停时的已过时间
        /// </summary>
        public float PausedElapsedTime { get; set; }

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// 取消令牌源
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }
}
