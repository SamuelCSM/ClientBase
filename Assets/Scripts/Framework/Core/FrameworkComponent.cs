namespace Framework.Core
{
    /// <summary>
    /// 框架组件抽象基类
    /// 所有Manager继承此类，定义统一的生命周期接口
    /// </summary>
    public abstract class FrameworkComponent
    {
        /// <summary>
        /// 初始化（在GameEntry.Awake中调用）
        /// </summary>
        public virtual void OnInit()
        {
        }

        /// <summary>
        /// 每帧更新（在GameEntry.Update中调用）
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public virtual void OnUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 延迟更新（在GameEntry.LateUpdate中调用）
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public virtual void OnLateUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 固定更新（在GameEntry.FixedUpdate中调用）
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间间隔</param>
        public virtual void OnFixedUpdate(float fixedDeltaTime)
        {
        }

        /// <summary>
        /// 关闭清理（在GameEntry.OnApplicationQuit中调用）
        /// </summary>
        public virtual void OnShutdown()
        {
        }
    }
}
