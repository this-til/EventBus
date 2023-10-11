using System;
using System.Reflection;

namespace EventBus;

public class DefaultEventTriggerFilter : SingletonPatternClass<DefaultEventTriggerFilter>, IEventTriggerFilter {
    public bool isFilter(IEventBus eventBus, object obj, Type eventType, MethodInfo methodInfo, EventAttribute? eventAttribute) {
        Type type = obj.GetType();
        if (eventAttribute is not null) {
            if (eventAttribute.eventAttributeType.HasFlag(EventAttributeType.no)) {
                return true;
            }
            if (eventAttribute.eventAttributeType.HasFlag(EventAttributeType.test)) {
                eventBus.getLog().Warn($"[{type}]中的事件方法[{methodInfo}]是一个测试事件，请测试完成后删除");
            }
            if (methodInfo.GetCustomAttribute<ObsoleteAttribute>() != null) {
                eventBus.getLog().Warn($"[{type}]中的事件方法[{methodInfo}]是被弃用的方法");
                return true;
            }
        }
        if (methodInfo.ContainsGenericParameters) {
            eventBus.getLog().Error($"[{type}]中的事件方法[{methodInfo}]包含泛型参数，无法被注册进事件系统");
            return true;
        }
        return false;
    }
}