using System;
using System.Reflection;

namespace EventBus; 

/// <summary>
/// 注册事件过滤器
/// </summary>
public interface IEventTriggerFilter {
    /// <summary>
    /// 如果返回true代表它被过滤掉了，它无法被注册进系统
    /// </summary>
    bool isFilter(IEventBus eventBus, object obj, Type eventType, MethodInfo methodInfo, EventAttribute? eventAttribute);
}