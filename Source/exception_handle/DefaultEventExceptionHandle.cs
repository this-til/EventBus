namespace EventBus;

public class DefaultEventExceptionHandle : SingletonPatternClass<DefaultEventExceptionHandle>, IEventExceptionHandle {
    public ExceptionHandleType doCatch(IEventBus iEventBus, IEventTrigger eventTrigger, Event @event, Exception exception) {
        return ExceptionHandleType.@throw;
    }
}