using System;
using System.Reflection;

namespace Til.EventBus;

/// <summary>
/// 一个注册者过滤器
/// </summary>
public interface IEventRegistrantFilter {
    /// <summary>
    /// 如果返回true代表它被过滤掉了，它无法被注册进系统
    /// </summary>
    bool isFilter(IEventBus eventBus, object registrant);
}

/// <summary>
/// 定义一个单例模式类，继承自SingletonPatternClass，实现IEventRegistrantFilter接口
/// </summary>
public class EventRegistrantExcludeAttributeFilter : SingletonPatternClass<EventRegistrantExcludeAttributeFilter>, IEventRegistrantFilter {
    /// <summary>
    /// 定义一个方法，接收IEventBus和object类型的参数
    /// </summary>
    /// <param name="eventBus"></param>
    /// <param name="registrant"></param>
    /// <returns></returns>
    public bool isFilter(IEventBus eventBus, object registrant) {
        // 获取registrant的类型，并获取其自定义属性EventSupplierExcludeAttribute
        EventSupplierExcludeAttribute? eventSupplierExcludeAttribute = registrant.GetType().GetCustomAttribute<EventSupplierExcludeAttribute>();
        // 如果获取到的属性为空，则返回false
        if (eventSupplierExcludeAttribute is null) {
            return false;
        }
        // 否则，根据registrant的类型进行判断
        switch (registrant) {
            case Type:
                return eventSupplierExcludeAttribute.excludeState;
            default:
                return eventSupplierExcludeAttribute.excludeStateInstance;
        }
    }
}

/// <summary>
/// 定义一个单例模式类，继承自SingletonPatternClass，实现IEventRegistrantFilter接口
/// </summary>
public class EventRegistrantTypeFilter : SingletonPatternClass<EventRegistrantTypeFilter>, IEventRegistrantFilter {
    /// <summary>
    ///  定义一个方法，接收IEventBus和object类型的参数
    /// </summary>
    /// <param name="eventBus"></param>
    /// <param name="registrant"></param>
    /// <returns></returns>
    public bool isFilter(IEventBus eventBus, object registrant) {
        // 如果registrant的类型是基础数据类型，则返回true
        if (registrant.GetType().IsPrimitive) {
            eventBus.getLog()?.Warn($"注册项[{registrant}]是基础数据类型，他不能被注册");
            return true;
        }
        // 如果registrant的类型是结构体，则返回true
        if (registrant.GetType().IsValueType) {
            eventBus.getLog()?.Warn($"注册项[{registrant}]是结构体，他不能被注册");
            return true;
        }
        // 如果registrant的类型是枚举，则返回true
        if (registrant.GetType().IsEnum) {
            eventBus.getLog()?.Warn($"注册项[{registrant}]是枚举，他不能被注册");
            return true;
        }
        // 如果registrant的类型是IEventBus，则返回true
        if (registrant is IEventBus) {
            eventBus.getLog()?.Warn($"注册项[{registrant}]是{typeof(IEventBus)}，他不能被注册");
            return true;
        }
        // 否则，返回false
        return false;
    }
}