using System;
using System.Reflection;

namespace Til.EventBus {
    /// <summary>
    /// 一个注册者过滤器
    /// </summary>
    public interface IEventRegistrantFilter {
        /// <summary>
        /// 如果返回true代表它被过滤掉了，它无法被注册进系统
        /// </summary>
        bool isFilter(IEventBus eventBus, object registrant);
    }

    public class EventRegistrantExcludeAttributeFilter : SingletonPatternClass<EventRegistrantExcludeAttributeFilter>, IEventRegistrantFilter {
        public bool isFilter(IEventBus eventBus, object registrant) {
            EventSupplierExcludeAttribute? eventSupplierExcludeAttribute = registrant.GetType().GetCustomAttribute<EventSupplierExcludeAttribute>();
            if (eventSupplierExcludeAttribute is null) {
                return false;
            }
            switch (registrant) {
                case Type:
                    return eventSupplierExcludeAttribute.excludeState;
                default:
                    return eventSupplierExcludeAttribute.excludeStateInstance;
            }
        }
    }

    public class EventRegistrantTypeFilter : SingletonPatternClass<EventRegistrantTypeFilter>, IEventRegistrantFilter {
        public bool isFilter(IEventBus eventBus, object registrant) {
            if (registrant.GetType().IsPrimitive) {
                eventBus.getLog()?.Warn($"注册项[{registrant}]是基础数据类型，他不能被注册");
                return true;
            }
            if (registrant.GetType().IsValueType) {
                eventBus.getLog()?.Warn($"注册项[{registrant}]是结构体，他不能被注册");
                return true;
            }
            if (registrant.GetType().IsEnum) {
                eventBus.getLog()?.Warn($"注册项[{registrant}]是枚举，他不能被注册");
                return true;
            }
            if (registrant is IEventBus) {
                eventBus.getLog()?.Warn($"注册项[{registrant}]是{typeof(IEventBus)}，他不能被注册");
                return true;
            }
            return false;
        }
    }
}