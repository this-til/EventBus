using System;
using System.Reflection;

namespace EventBus; 

/// <summary>
/// 事件触发器
/// 用来调用事件回调
/// </summary>
public interface IEventTrigger {
    /// <summary>
    /// 事件被调用
    /// </summary>
    void invoke(Event @event);

    /// <summary>
    /// 获取事件的注册者
    /// </summary>
    object getEventRegistrant();

    /// <summary>
    /// 获取事件类型
    /// </summary>
    Type getEventType();

    /// <summary>
    /// 获取事件调用的优先级
    /// </summary>
    int getEventPriority();
}