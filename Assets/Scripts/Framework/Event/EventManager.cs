using System;
using System.Collections.Generic;
using Framework.Core;

namespace Framework
{
    /// <summary>
    /// 事件管理器
    /// 提供事件的注册、注销和触发功能，支持优先级和防止循环触发
    /// </summary>
    public class EventManager : FrameworkComponent
    {
        // 事件监听器字典（事件ID -> 监听器列表）
        private Dictionary<int, List<EventListener>> _eventListeners = new Dictionary<int, List<EventListener>>();

        // 正在触发的事件集合（用于防止循环触发）
        private HashSet<int> _triggeringEvents = new HashSet<int>();

        // 是否需要排序（优先级变化时设置为 true）
        private Dictionary<int, bool> _needSort = new Dictionary<int, bool>();

        /// <summary>
        /// 初始化
        /// </summary>
        public override void OnInit()
        {
            base.OnInit();
            Logger.Log("[EventManager] 事件管理器初始化完成");
        }

        /// <summary>
        /// 关闭清理
        /// </summary>
        public override void OnShutdown()
        {
            Clear();
            Logger.Log("[EventManager] 事件管理器已关闭");
            base.OnShutdown();
        }

        #region 注册事件（无参数）

        /// <summary>
        /// 注册事件（无参数）
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="callback">回调函数</param>
        /// <param name="priority">优先级（数值越大优先级越高，默认为0）</param>
        public void AddListener(int eventId, Action callback, int priority = 0)
        {
            if (callback == null)
            {
                Logger.Warning($"[EventManager] 注册事件失败，回调为 null，事件ID: {eventId}");
                return;
            }

            EventListener listener = new EventListener
            {
                EventId = eventId,
                Callback = callback,
                Priority = priority
            };

            AddListenerInternal(eventId, listener);
        }

        #endregion

        #region 注册事件（1个参数）

        /// <summary>
        /// 注册事件（1个参数）
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventId">事件ID</param>
        /// <param name="callback">回调函数</param>
        /// <param name="priority">优先级（数值越大优先级越高，默认为0）</param>
        public void AddListener<T>(int eventId, Action<T> callback, int priority = 0)
        {
            if (callback == null)
            {
                Logger.Warning($"[EventManager] 注册事件失败，回调为 null，事件ID: {eventId}");
                return;
            }

            EventListener listener = new EventListener
            {
                EventId = eventId,
                Callback = callback,
                Priority = priority
            };

            AddListenerInternal(eventId, listener);
        }

        #endregion

        #region 注册事件（2个参数）

        /// <summary>
        /// 注册事件（2个参数）
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <param name="eventId">事件ID</param>
        /// <param name="callback">回调函数</param>
        /// <param name="priority">优先级（数值越大优先级越高，默认为0）</param>
        public void AddListener<T1, T2>(int eventId, Action<T1, T2> callback, int priority = 0)
        {
            if (callback == null)
            {
                Logger.Warning($"[EventManager] 注册事件失败，回调为 null，事件ID: {eventId}");
                return;
            }

            EventListener listener = new EventListener
            {
                EventId = eventId,
                Callback = callback,
                Priority = priority
            };

            AddListenerInternal(eventId, listener);
        }

        #endregion

        #region 注销事件

        /// <summary>
        /// 注销事件（无参数）
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="callback">回调函数</param>
        public void RemoveListener(int eventId, Action callback)
        {
            if (callback == null)
                return;

            RemoveListenerInternal(eventId, callback);
        }

        /// <summary>
        /// 注销事件（1个参数）
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventId">事件ID</param>
        /// <param name="callback">回调函数</param>
        public void RemoveListener<T>(int eventId, Action<T> callback)
        {
            if (callback == null)
                return;

            RemoveListenerInternal(eventId, callback);
        }

        /// <summary>
        /// 注销事件（2个参数）
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <param name="eventId">事件ID</param>
        /// <param name="callback">回调函数</param>
        public void RemoveListener<T1, T2>(int eventId, Action<T1, T2> callback)
        {
            if (callback == null)
                return;

            RemoveListenerInternal(eventId, callback);
        }

        /// <summary>
        /// 移除指定事件的所有监听器
        /// </summary>
        /// <param name="eventId">事件ID</param>
        public void RemoveAllListeners(int eventId)
        {
            if (_eventListeners.ContainsKey(eventId))
            {
                _eventListeners.Remove(eventId);
                _needSort.Remove(eventId);
                Logger.Debug($"[EventManager] 移除事件所有监听器: {EventDefine.GetEventName(eventId)} (ID: {eventId})");
            }
        }

        #endregion

        #region 触发事件

        /// <summary>
        /// 触发事件（无参数）
        /// </summary>
        /// <param name="eventId">事件ID</param>
        public void TriggerEvent(int eventId)
        {
            // 检查是否正在触发该事件（防止循环触发）
            if (_triggeringEvents.Contains(eventId))
            {
                Logger.Warning($"[EventManager] 检测到事件循环触发，已阻止: {EventDefine.GetEventName(eventId)} (ID: {eventId})");
                return;
            }

            if (!_eventListeners.ContainsKey(eventId))
                return;

            // 标记正在触发
            _triggeringEvents.Add(eventId);

            try
            {
                // 排序（如果需要）
                SortListenersIfNeeded(eventId);

                // 复制监听器列表（避免在触发过程中修改列表导致问题）
                List<EventListener> listeners = new List<EventListener>(_eventListeners[eventId]);

                // 触发所有监听器
                foreach (EventListener listener in listeners)
                {
                    try
                    {
                        if (listener.Callback is Action action)
                        {
                            action.Invoke();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[EventManager] 触发事件回调异常: {EventDefine.GetEventName(eventId)} (ID: {eventId}), 错误: {ex.Message}");
                    }
                }
            }
            finally
            {
                // 移除触发标记
                _triggeringEvents.Remove(eventId);
            }
        }

        /// <summary>
        /// 触发事件（1个参数）
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventId">事件ID</param>
        /// <param name="arg">参数</param>
        public void TriggerEvent<T>(int eventId, T arg)
        {
            // 检查是否正在触发该事件（防止循环触发）
            if (_triggeringEvents.Contains(eventId))
            {
                Logger.Warning($"[EventManager] 检测到事件循环触发，已阻止: {EventDefine.GetEventName(eventId)} (ID: {eventId})");
                return;
            }

            if (!_eventListeners.ContainsKey(eventId))
                return;

            // 标记正在触发
            _triggeringEvents.Add(eventId);

            try
            {
                // 排序（如果需要）
                SortListenersIfNeeded(eventId);

                // 复制监听器列表
                List<EventListener> listeners = new List<EventListener>(_eventListeners[eventId]);

                // 触发所有监听器
                foreach (EventListener listener in listeners)
                {
                    try
                    {
                        if (listener.Callback is Action<T> action)
                        {
                            action.Invoke(arg);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[EventManager] 触发事件回调异常: {EventDefine.GetEventName(eventId)} (ID: {eventId}), 错误: {ex.Message}");
                    }
                }
            }
            finally
            {
                // 移除触发标记
                _triggeringEvents.Remove(eventId);
            }
        }

        /// <summary>
        /// 触发事件（2个参数）
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <param name="eventId">事件ID</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        public void TriggerEvent<T1, T2>(int eventId, T1 arg1, T2 arg2)
        {
            // 检查是否正在触发该事件（防止循环触发）
            if (_triggeringEvents.Contains(eventId))
            {
                Logger.Warning($"[EventManager] 检测到事件循环触发，已阻止: {EventDefine.GetEventName(eventId)} (ID: {eventId})");
                return;
            }

            if (!_eventListeners.ContainsKey(eventId))
                return;

            // 标记正在触发
            _triggeringEvents.Add(eventId);

            try
            {
                // 排序（如果需要）
                SortListenersIfNeeded(eventId);

                // 复制监听器列表
                List<EventListener> listeners = new List<EventListener>(_eventListeners[eventId]);

                // 触发所有监听器
                foreach (EventListener listener in listeners)
                {
                    try
                    {
                        if (listener.Callback is Action<T1, T2> action)
                        {
                            action.Invoke(arg1, arg2);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[EventManager] 触发事件回调异常: {EventDefine.GetEventName(eventId)} (ID: {eventId}), 错误: {ex.Message}");
                    }
                }
            }
            finally
            {
                // 移除触发标记
                _triggeringEvents.Remove(eventId);
            }
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 添加监听器（内部方法）
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="listener">监听器</param>
        private void AddListenerInternal(int eventId, EventListener listener)
        {
            if (!_eventListeners.ContainsKey(eventId))
            {
                _eventListeners[eventId] = new List<EventListener>();
            }

            _eventListeners[eventId].Add(listener);

            // 标记需要排序
            _needSort[eventId] = true;

            Logger.Debug($"[EventManager] 注册事件: {EventDefine.GetEventName(eventId)} (ID: {eventId}), 优先级: {listener.Priority}");
        }

        /// <summary>
        /// 移除监听器（内部方法）
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="callback">回调函数</param>
        private void RemoveListenerInternal(int eventId, Delegate callback)
        {
            if (!_eventListeners.ContainsKey(eventId))
                return;

            List<EventListener> listeners = _eventListeners[eventId];
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (listeners[i].Callback.Equals(callback))
                {
                    listeners.RemoveAt(i);
                    Logger.Debug($"[EventManager] 注销事件: {EventDefine.GetEventName(eventId)} (ID: {eventId})");
                    break;
                }
            }

            // 如果没有监听器了，移除该事件
            if (listeners.Count == 0)
            {
                _eventListeners.Remove(eventId);
                _needSort.Remove(eventId);
            }
        }

        /// <summary>
        /// 排序监听器（如果需要）
        /// </summary>
        /// <param name="eventId">事件ID</param>
        private void SortListenersIfNeeded(int eventId)
        {
            if (_needSort.ContainsKey(eventId) && _needSort[eventId])
            {
                _eventListeners[eventId].Sort((a, b) => b.Priority.CompareTo(a.Priority));
                _needSort[eventId] = false;
            }
        }

        /// <summary>
        /// 清除所有事件监听器
        /// </summary>
        public void Clear()
        {
            _eventListeners.Clear();
            _triggeringEvents.Clear();
            _needSort.Clear();
            Logger.Debug("[EventManager] 清除所有事件监听器");
        }

        #endregion
    }

    /// <summary>
    /// 事件监听器
    /// </summary>
    internal class EventListener
    {
        /// <summary>
        /// 事件ID
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// 回调函数
        /// </summary>
        public Delegate Callback { get; set; }

        /// <summary>
        /// 优先级（数值越大优先级越高）
        /// </summary>
        public int Priority { get; set; }
    }
}
