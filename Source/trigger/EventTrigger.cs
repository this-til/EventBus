using System;
using System.Reflection;

namespace EventBus;

public abstract class EventTrigger : IEventTrigger {
    protected readonly MethodInfo methodInfo;
    protected readonly object registrant;
    protected readonly EventAttribute? eventAttribute;
    protected readonly Type type;

    protected EventTrigger(MethodInfo methodInfo, object registrant, EventAttribute? eventAttribute, Type eventType) {
        this.methodInfo = methodInfo;
        this.registrant = registrant;
        this.eventAttribute = eventAttribute;
        this.type = eventType;
    }

    public object getEventRegistrant() {
        return registrant;
    }

    public int getEventPriority() {
        return eventAttribute?.priority ?? 0;
    }

    public abstract void invoke(Event @event);

    public Type getEventType() => type;

    public static EventTrigger? create(MethodInfo methodInfo, Type eventType, object use, EventAttribute? eventAttribute) {
        Type type = typeof(EventTrigger<>).MakeGenericType(eventType);
        object? obj = Activator.CreateInstance(type, methodInfo, use, eventAttribute);
        return obj as EventTrigger;
    }
}

public class EventTrigger<T> : EventTrigger where T : Event {
    public delegate void eventDelegate(T @event);

    protected readonly eventDelegate @delegate;

    public EventTrigger(MethodInfo methodInfo, object use, EventAttribute? eventAttribute) : base(methodInfo, use, eventAttribute, typeof(T)) {
        this.@delegate = (eventDelegate)methodInfo.CreateDelegate(typeof(eventDelegate), methodInfo.IsStatic ? null : use);
    }

    public override void invoke(Event @event) {
        @delegate((T)@event);
    }
}