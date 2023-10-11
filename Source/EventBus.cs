using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using log4net.Core;

namespace EventBus;

public class EventBus : IEventBus {
    protected readonly HashSet<object> allRegistered = new HashSet<object>();
    protected readonly Dictionary<Type, List<IEventTrigger>> eventBus = new Dictionary<Type, List<IEventTrigger>>();
    protected readonly List<IEventRegistrantFilter> eventRegistrantFilterList = new List<IEventRegistrantFilter>();
    protected readonly List<IEventTriggerFilter> eventTriggerFilterList = new List<IEventTriggerFilter>();
    protected readonly List<IEventTriggerFactory> eventTriggerFactoryList = new List<IEventTriggerFactory>();
    protected readonly List<IEventExceptionHandle> eventExceptionHandles = new List<IEventExceptionHandle>();
    protected readonly HashSet<object> removeRegisteredSet = new HashSet<object>();

    protected ILog log = LogManager.GetLogger(typeof(EventBus));

    public EventBus() {
        addEventRegistrantFilter(DefaultEventRegistrantFilter.getInstance());
        addEventTriggerFilter(DefaultEventTriggerFilter.getInstance());
        addEventTriggerFactory(DefaultEventTriggerFactory.getInstance());
        addEventExceptionHandle(DefaultEventExceptionHandle.getInstance());
    }

    public void put(object registered) {
        log.Info($"开始注册事件监听者，监听者:{registered}，监听者类型：{registered.GetType()}");
        for (var index = eventRegistrantFilterList.Count - 1; index >= 0; index--) {
            if (eventRegistrantFilterList[index].isFilter(this, registered)) {
                log.Info("事件监听被过滤掉了。");
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
                log.Info($"监听方法被忽视了，方法:{methodInfo}");
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
            log.Info($"监听方法注册成功，方法:{methodInfo}");
            end: ;
        }
        log.Info($"结束注册事件监听者，监听者:{registered}，监听者类型：{registered.GetType()}");
    }

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

    public void setLog(ILog logger) {
        this.log = logger;
    }

    public ILog getLog() {
        return this.log;
    }
}