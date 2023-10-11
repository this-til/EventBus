using System.Reflection;

namespace EventBus; 

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class EventAttribute : System.Attribute {
    public EventAttributeType eventAttributeType;

    /// <summary>
    /// 优先级
    /// </summary>
    public int priority;
}

[Flags]
public enum EventAttributeType {
    /// <summary>
    /// 方法不作为事件
    /// </summary>
    no = 1 << 0,

    /// <summary>
    /// 是测试事件
    /// </summary>
    test = 1 << 1,
}