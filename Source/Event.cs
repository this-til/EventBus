namespace Til.EventBus;

/// <summary>
/// 事件基类
/// </summary>
public class Event {
    /// <summary>
    /// 事件能不能继续调用
    /// 如果为false将中断事件的继续调用
    /// </summary>
    public virtual bool isContinue() => true;
}