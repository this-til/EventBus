using System;
using System.Reflection;

namespace EventBus {
    /// <summary>
    /// 事件创造工厂
    /// </summary>
    public interface IEventTriggerFactory {
        /// <summary>
        /// 创建一个事件触发器
        /// 如果不能创建请返回null,
        /// 返回null代表放弃对监听方法进行构建
        /// </summary>
        IEventTrigger? create(IEventBus eventBus, object obj, Type eventType, MethodInfo methodInfo, EventAttribute? eventAttribute);
    }

    public class DefaultEventTriggerFactory : SingletonPatternClass<DefaultEventTriggerFactory>, IEventTriggerFactory {
        public IEventTrigger? create(IEventBus eventBus, object obj, Type eventType, MethodInfo methodInfo, EventAttribute? eventAttribute) {
            return EventTrigger.create(methodInfo, eventType, obj, eventAttribute);
        }
    }
}