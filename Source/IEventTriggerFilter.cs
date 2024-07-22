using System;
using System.Reflection;

namespace Til.EventBus {
    /// <summary>
    /// 注册事件过滤器
    /// </summary>
    public interface IEventTriggerFilter {
        /// <summary>
        /// 如果返回true代表它被过滤掉了，它无法被注册进系统
        /// </summary>
        bool isFilter(IEventBus eventBus, object? obj, Type objType, Type eventType, MethodInfo methodInfo, EventAttribute? eventAttribute);
    }
    /// <summary>
    /// 定义一个单例模式类，继承自SingletonPatternClass，实现IEventTriggerFilter接口
    /// </summary>
    public class DefaultEventTriggerFilter : SingletonPatternClass<DefaultEventTriggerFilter>, IEventTriggerFilter {
        /// <summary>
        /// 实现IEventTriggerFilter接口中的isFilter方法
        /// </summary>
        /// <param name="eventBus"></param>
        /// <param name="obj"></param>
        /// <param name="objType"></param>
        /// <param name="eventType"></param>
        /// <param name="methodInfo"></param>
        /// <param name="eventAttribute"></param>
        /// <returns></returns>
        public bool isFilter(IEventBus eventBus, object? obj, Type objType, Type eventType, MethodInfo methodInfo, EventAttribute? eventAttribute) {
            // 如果事件属性不为空
            if (eventAttribute is not null) {
                // 如果事件属性中不包含no属性，则返回false
                if (eventAttribute.eventAttributeType.HasFlag(EventAttributeType.no)) {
                    return true;
                }
                // 如果事件属性中包含test属性，则警告日志
                if (eventAttribute.eventAttributeType.HasFlag(EventAttributeType.test)) {
                    eventBus.getLog()?.Warn($"[{objType}]中的事件方法[{methodInfo}]是一个测试事件，请测试完成后删除");
                }
                // 如果事件方法包含ObsoleteAttribute属性，则警告日志
                if (methodInfo.GetCustomAttribute<ObsoleteAttribute>() != null) {
                    eventBus.getLog()?.Warn($"[{objType}]中的事件方法[{methodInfo}]是被弃用的方法");
                    return true;
                }
            }
            // 如果事件方法包含泛型参数，则错误日志
            if (methodInfo.ContainsGenericParameters) {
                eventBus.getLog()?.Error($"[{objType}]中的事件方法[{methodInfo}]包含泛型参数，无法被注册进事件系统");
                return true;
            }
            // 否则返回false
            return false;
        }
    }
}