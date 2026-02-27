using UnityEngine;

namespace Framework
{
    /// <summary>
    /// UI视图基类
    /// 用于序列化UI组件引用，不包含业务逻辑
    /// 在Unity编辑器中拖拽赋值UI组件
    /// </summary>
    public abstract class UIView : MonoBehaviour
    {
        // 子类在这里定义需要序列化的UI组件
        // 例如：
        // public Button btnClose;
        // public Text txtTitle;
        // public Image imgIcon;
    }
}
