using System;
using System.Reflection;

namespace Til.EventBus {
    /// <summary>
    /// 事件创造工厂
    /// </summary>
    public interface IEventTriggerFactory {
        /// <summary>
        /// 创建一个事件触发器
        /// 如果不能创建请返回null,
        /// 返回null代表放弃对监听方法进行构建
        /// </summary>
        IEventTrigger? create(IEventBus eventBus, object obj, Type eventType, Type methodInfoReturnType, MethodInfo methodInfo, EventAttribute? eventAttribute);
    }

    /// <summary>
    /// 定义一个单例模式类，继承自SingletonPatternClass
    /// </summary>
    public class DefaultEventTriggerFactory : SingletonPatternClass<DefaultEventTriggerFactory>, IEventTriggerFactory {
        /// <summary>
        /// 实现IEventTriggerFactory接口，重写create方法
        /// </summary>
        /// <param name="eventBus"></param>
        /// <param name="obj"></param>
        /// <param name="eventType"></param>
        /// <param name="methodInfoReturnType"></param>
        /// <param name="methodInfo"></param>
        /// <param name="eventAttribute"></param>
        /// <returns></returns>
        public IEventTrigger? create(IEventBus eventBus, object obj, Type eventType, Type methodInfoReturnType, MethodInfo methodInfo, EventAttribute? eventAttribute) {
            // 调用EventTrigger的create方法，创建事件触发器
            return EventTrigger.create(methodInfo, eventType, methodInfoReturnType, obj, eventAttribute);
        }
    }
}