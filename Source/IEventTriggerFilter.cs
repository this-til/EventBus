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
    bool isFilter(IEventBus eventBus, object? obj, Type objType, Type eventType, MethodInfo methodInfo, EventAttribute? eventAttribute);
}

public class DefaultEventTriggerFilter : SingletonPatternClass<DefaultEventTriggerFilter>, IEventTriggerFilter {
    public bool isFilter(IEventBus eventBus, object? obj, Type objType,Type eventType, MethodInfo methodInfo, EventAttribute? eventAttribute) {
        if (eventAttribute is not null) {
            if (eventAttribute.eventAttributeType.HasFlag(EventAttributeType.no)) {
                return true;
            }
            if (eventAttribute.eventAttributeType.HasFlag(EventAttributeType.test)) {
                eventBus.getLog()?.Warn($"[{objType}]中的事件方法[{methodInfo}]是一个测试事件，请测试完成后删除");
            }
            if (methodInfo.GetCustomAttribute<ObsoleteAttribute>() != null) {
                eventBus.getLog()?.Warn($"[{objType}]中的事件方法[{methodInfo}]是被弃用的方法");
                return true;
            }
        }
        if (methodInfo.ContainsGenericParameters) {
            eventBus.getLog()?.Error($"[{objType}]中的事件方法[{methodInfo}]包含泛型参数，无法被注册进事件系统");
            return true;
        }
        return false;
    }
}