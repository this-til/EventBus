namespace EventBus;

/// <summary>
/// 一个注册者过滤器
/// </summary>
public interface IEventRegistrantFilter {
    /// <summary>
    /// 如果返回true代表它被过滤掉了，它无法被注册进系统
    /// </summary>
    bool isFilter(IEventBus eventBus, object registrant);
}