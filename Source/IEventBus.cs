using System.Reflection;

namespace EventBus;

public interface IEventBus {
    /// <summary>
    /// 获取所有在这注册过的注册者
    /// </summary>
    ISet<object> getAllRegistered();

    /// <summary>
    /// 注册一个事件监听者
    /// </summary>
    /// <param name="registered">事件的注册者</param>
    void put(object registered);

    /// <summary>
    /// 发布一个事件
    /// 并且返回这个事件本身
    /// </summary>
    [Event(eventAttributeType = EventAttributeType.no)]
    Event onEvent(Event @event);

    /// <summary>
    /// 删除一个注册者
    /// </summary>
    /// <param name="registered">事件的注册者</param>
    void remove(object registered);

    /// <summary>
    /// 设置日志输出接口
    /// </summary>
    void setLog(ILogOut logger);

    /// <summary>
    /// 获取事件系统的一个日志输出接口
    /// </summary>
    ILogOut? getLog();

    void addEventRegistrantFilter(IEventRegistrantFilter eventRegistrantFilter);

    void addEventTriggerFilter(IEventTriggerFilter eventTriggerFilter);

    void addEventTriggerFactory(IEventTriggerFactory eventTriggerFactory);

    void addEventExceptionHandle(IEventExceptionHandle eventExceptionHandle);
}

[EventSupplierExclude]
public class EventBus : IEventBus {
    protected readonly HashSet<object> allRegistered = new HashSet<object>();
    protected readonly Dictionary<Type, List<IEventTrigger>> eventBus = new Dictionary<Type, List<IEventTrigger>>();
    protected readonly List<IEventRegistrantFilter> eventRegistrantFilterList = new List<IEventRegistrantFilter>();
    protected readonly List<IEventTriggerFilter> eventTriggerFilterList = new List<IEventTriggerFilter>();
    protected readonly List<IEventTriggerFactory> eventTriggerFactoryList = new List<IEventTriggerFactory>();
    protected readonly List<IEventExceptionHandle> eventExceptionHandles = new List<IEventExceptionHandle>();
    protected readonly HashSet<object> removeRegisteredSet = new HashSet<object>();

    protected ILogOut? log;

    public EventBus() {
        addEventRegistrantFilter(EventRegistrantTypeFilter.getInstance());
        addEventRegistrantFilter(EventRegistrantExcludeAttributeFilter.getInstance());
        addEventTriggerFilter(DefaultEventTriggerFilter.getInstance());
        addEventTriggerFactory(DefaultEventTriggerFactory.getInstance());
        addEventExceptionHandle(DefaultEventExceptionHandle.getInstance());
    }

    public void put(object registered) {
        getLog()?.Info($"开始注册事件监听者 监听者:{registered},监听者类型:{registered.GetType()}");
        for (var index = eventRegistrantFilterList.Count - 1; index >= 0; index--) {
            if (eventRegistrantFilterList[index].isFilter(this, registered)) {
                getLog()?.Info("事件监听被过滤掉了.");
                return;
            }
        }
        allRegistered.Add(registered);
        List<MethodInfo> methodInfos = new List<MethodInfo>();
        switch (registered) {
            case Type staticRegistered:
                methodInfos.AddRange(staticRegistered.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
                break;
            default:
                methodInfos.AddRange(registered.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                break;
        }
        foreach (var methodInfo in methodInfos) {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length != 1) {
                continue;
            }
            if (!typeof(Event).IsAssignableFrom(parameterInfos[0].ParameterType)) {
                continue;
            }
            EventAttribute? eventAttribute = methodInfo.GetCustomAttribute<EventAttribute>();
            Type eventType = parameterInfos[0].ParameterType;

            for (var index = eventTriggerFilterList.Count - 1; index >= 0; index--) {
                if (eventTriggerFilterList[index].isFilter(this, registered, eventType, methodInfo, eventAttribute)) {
                    goto end;
                }
            }

            if (!eventBus.TryGetValue(eventType, out var list)) {
                list = new List<IEventTrigger>();
                eventBus.Add(eventType, list);
            }

            IEventTrigger? newEventTrigger = null;
            for (var index = eventTriggerFactoryList.Count - 1; index >= 0; index--) {
                newEventTrigger = eventTriggerFactoryList[index].create(this, registered, eventType, methodInfo, eventAttribute);
                if (newEventTrigger != null) {
                    break;
                }
            }
            if (newEventTrigger == null) {
                getLog()?.Info($"监听方法被忽视了 方法:{methodInfo}");
                continue;
            }

            bool needInsert = true;
            for (var index = 0; index < list.Count; index++) {
                IEventTrigger eventTrigger = list[index];
                if (eventTrigger.getEventPriority() > newEventTrigger.getEventPriority()) {
                    continue;
                }
                list.Insert(index, newEventTrigger);
                needInsert = false;
                break;
            }
            if (needInsert) {
                list.Add(newEventTrigger);
            }
            getLog()?.Info($"监听方法注册成功 方法:{methodInfo}");
            end: ;
        }
        getLog()?.Info($"结束注册事件监听者 监听者:{registered},监听者类型:{registered.GetType()}");
    }

    [Event(eventAttributeType = EventAttributeType.no)]
    public Event onEvent(Event @event) {
        if (removeRegisteredSet.Count > 0) {
            foreach (var registered in removeRegisteredSet) {
                removeProtected(registered);
            }
            removeRegisteredSet.Clear();
        }
        List<Type> can = @event.GetType().getParents();
        int canCount = can.Count;
        for (var i = 0; i < canCount; i++) {
            interrupt:
            Type type = can[i];
            if (eventBus.ContainsKey(type)) {
                List<IEventTrigger> arrayList = eventBus[type];
                if (arrayList.Count < 0) {
                    continue;
                }
                int runCount = arrayList.Count;
                for (int ti = 0; ti < runCount; ti++) {
                    try {
                        arrayList[ti].invoke(@event);
                    }
                    catch (Exception e) {
                        for (var ei = eventExceptionHandles.Count - 1; ei >= 0; ei--) {
                            ExceptionHandleType exceptionHandleType = eventExceptionHandles[ei].doCatch(this, arrayList[ti], @event, e);
                            switch (exceptionHandleType) {
                                case ExceptionHandleType.success:
                                    goto success;
                                case ExceptionHandleType.success_interrupt:
                                    goto interrupt;
                                case ExceptionHandleType.success_end:
                                    goto end;
                                case ExceptionHandleType.@throw:
                                    throw;
                                case ExceptionHandleType.skip:
                                    continue;
                                default:
                                    throw;
                            }
                        }
                    }
                    success:
                    if (!@event.isContinue()) {
                        break;
                    }
                }
            }
        }
        end:
        return @event;
    }

    protected void removeProtected(object registered) {
        allRegistered.Remove(registered);
        foreach (var keyValuePair in eventBus) {
            List<IEventTrigger> eventTriggers = keyValuePair.Value;
            for (var index = 0; index < eventTriggers.Count; index++) {
                if (!registered.Equals(eventTriggers[index].getEventRegistrant())) {
                    continue;
                }
                eventTriggers.RemoveAt(index);
                index--;
            }
        }
    }

    public void remove(object registered) {
        if (!allRegistered.Contains(registered)) {
            return;
        }
        removeRegisteredSet.Add(registered);
    }

    public ISet<object> getAllRegistered() => allRegistered;

    public void addEventRegistrantFilter(IEventRegistrantFilter eventRegistrantFilter) {
        eventRegistrantFilterList.Add(eventRegistrantFilter);
    }

    public void addEventTriggerFilter(IEventTriggerFilter eventTriggerFilter) {
        eventTriggerFilterList.Add(eventTriggerFilter);
    }

    public void addEventTriggerFactory(IEventTriggerFactory eventTriggerFactory) {
        eventTriggerFactoryList.Add(eventTriggerFactory);
    }

    public void addEventExceptionHandle(IEventExceptionHandle eventExceptionHandle) {
        eventExceptionHandles.Add(eventExceptionHandle);
    }

    public void setLog(ILogOut logger) {
        this.log = logger;
    }

    public ILogOut? getLog() {
        return this.log;
    }
}