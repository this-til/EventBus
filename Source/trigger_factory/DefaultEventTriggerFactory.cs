using System.Reflection;

namespace EventBus; 

public class DefaultEventTriggerFactory : SingletonPatternClass<DefaultEventTriggerFactory>, IEventTriggerFactory {
    public IEventTrigger? create(IEventBus eventBus, object obj, Type eventType, MethodInfo methodInfo, EventAttribute? eventAttribute) {
        return EventTrigger.create(methodInfo, eventType, obj, eventAttribute);
    }
}