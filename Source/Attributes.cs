using System;

namespace Til.EventBus;

/// <summary>
/// 事件属性
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class EventAttribute : Attribute {
    /// <summary>
    /// 事件类型
    /// </summary>
    public EventAttributeType eventAttributeType;

    /// <summary>
    /// 优先级
    /// </summary>
    public int priority;
}

/// <summary>
/// 排除供应商
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class EventSupplierExcludeAttribute : Attribute {
    /// <summary>
    /// 排除状态
    /// </summary>
    public bool excludeState = true;
    /// <summary>
    /// 排除状态实例
    /// </summary>
    public bool excludeStateInstance = true;
}