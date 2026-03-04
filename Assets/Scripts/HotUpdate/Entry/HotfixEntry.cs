using UnityEngine;

namespace HotUpdate.Entry
{
    /// <summary>
    /// 热更新入口类
    /// 这是热更新代码的启动点，由Framework层的HotUpdateManager调用
    /// </summary>
    public class HotfixEntry : IHotfixEntry
    {
        /// <summary>
        /// 热更新启动方法
        /// </summary>
        public void Start()
        {
            Debug.Log("[HotfixEntry] 热更新代码启动成功！");
            
            // TODO: 在这里初始化热更新层的逻辑
            // 例如：
            // - 加载配置表
            // - 初始化游戏逻辑管理器
            // - 打开登录界面
            // - 连接游戏服务器
            
            InitializeHotUpdateLogic();
        }
        
        /// <summary>
        /// 初始化热更新逻辑
        /// </summary>
        private void InitializeHotUpdateLogic()
        {
            Debug.Log("[HotfixEntry] 初始化热更新逻辑...");
            
            // 示例：加载配置表
            // GameEntry.Data.LoadConfig<ItemConfigTable>();
            
            // 示例：打开登录UI
            // await GameEntry.UI.OpenUIAsync<LoginUI>();
            
            Debug.Log("[HotfixEntry] 热更新逻辑初始化完成");
        }
    }
    
    /// <summary>
    /// 热更新入口接口
    /// Framework层通过此接口调用热更新代码
    /// </summary>
    public interface IHotfixEntry
    {
        void Start();
    }
}
