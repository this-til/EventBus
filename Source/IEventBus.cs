using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Til.EventBus;

/// <summary>
/// 事件总线-接口
/// </summary>
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
    /// 以携程的方式开启一个事件
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    [Event(eventAttributeType = EventAttributeType.no)]
    IEnumerable onEvent_coroutine(Event @event);

    /// <summary>
    /// 删除一个注册者
    /// </summary>
    /// <param name="registered">事件的注册者</param>
    void remove(object registered);

    /// <summary>
    /// 设置事件总线的规则
    /// </summary>
    void setRule(IEventBusRule eventBusRule);

    /// <summary>
    /// 得到事件总线的规则
    /// </summary>
    IEventBusRule getRule();

    /// <summary>
    /// 获取日志对象
    /// </summary>
    /// <returns></returns>
    ILog? getLog();

    /// <summary>
    /// 设置日志对象
    /// </summary>
    /// <param name="log"></param>
    void setLog(ILog log);
}

/// <summary>
/// 事件总线
/// </summary>
[EventSupplierExclude]
public class EventBus : IEventBus {
    protected readonly HashSet<object> allRegistered = new HashSet<object>();
    protected readonly HashSet<object> removeRegisteredSet = new HashSet<object>();

    protected readonly Dictionary<Type, List<IEventTrigger>> eventBus = new Dictionary<Type, List<IEventTrigger>>();
    protected readonly Dictionary<Type, List<List<IEventTrigger>>> runTimeEventBus = new Dictionary<Type, List<List<IEventTrigger>>>();

    protected ILog? log;
    protected IEventBusRule eventBusRule = EventBusRule.defaultEventBusRule;

    protected Stack<Event> eventStack = new Stack<Event>();

    protected List<IEventTrigger> remove_cache = new List<IEventTrigger>();

    public void put(object registered) {
        if (allRegistered.Contains(registered)) {
            log?.Info($"{registered}重复的注册");
        }
        foreach (var eventRegistrantFilter in eventBusRule.forEventRegistrantFilter()) {
            if (eventRegistrantFilter.isFilter(this, registered)) {
                log?.Info($"{registered}被{eventRegistrantFilter}过滤掉了");
                return;
            }
        }

        allRegistered.Add(registered);
        BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
        bindingFlags |= registered is Type ? BindingFlags.Static : BindingFlags.Instance;
        Type registeredType = registered is Type ? (Type)registered : registered.GetType();
        foreach (var methodInfo in registeredType.GetMethods(bindingFlags)) {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length != 1) {
                continue;
            }
            if (!typeof(Event).IsAssignableFrom(parameterInfos[0].ParameterType)) {
                continue;
            }
            EventAttribute? eventAttribute = methodInfo.GetCustomAttribute<EventAttribute>();
            Type eventType = parameterInfos[0].ParameterType;

            foreach (var eventTriggerFilter in eventBusRule.forEventTriggerFilter()) {
                if (eventTriggerFilter.isFilter(this, registered, registeredType, eventType, methodInfo, eventAttribute)) {
                    log?.Info($"{registered}中{methodInfo}被{eventTriggerFilter}过滤掉了");
                    goto end;
                }
            }

            Type methodInfoReturnType = methodInfo.ReturnType;

            /*bool isCoroutine = methodInfoReturnType == typeof(IEnumerable) || methodInfoReturnType == typeof(IEnumerator);
            if (!isCoroutine && methodInfoReturnType != typeof(void)) {
                log?.Warn($"{registered}中{methodInfo}返回值不是{typeof(void)},{typeof(IEnumerable)},{typeof(IEnumerator)},但他不会被拍抛弃");
            }*/

            if (!eventBus.TryGetValue(eventType, out var list)) {
                list = new List<IEventTrigger>();
                eventBus.Add(eventType, list);
            }

            IEventTrigger? newEventTrigger = null;
            foreach (var eventTriggerFactory in eventBusRule.forEventTriggerFactory()) {
                newEventTrigger = eventTriggerFactory.create(this, registered, eventType, methodInfoReturnType, methodInfo, eventAttribute);
                if (newEventTrigger != null) {
                    break;
                }
            }
            if (newEventTrigger == null) {
                log?.Info($"{registered}中{methodInfo}并没有被创建{typeof(IEventTrigger)}");
                continue;
            }

            bool needInsert = true;
            for (var index = 0; index < list.Count; index++) {
                IEventTrigger eventTrigger = list[index];
                if (eventTrigger.getEventPriority() >= newEventTrigger.getEventPriority()) {
                    continue;
                }
                list.Insert(index, newEventTrigger);
                needInsert = false;
                break;
            }
            if (needInsert) {
                list.Add(newEventTrigger);
            }
            end: ;
        }
    }

    protected void deletionDetection() {
        if (removeRegisteredSet.Count <= 0) {
            return;
        }
        foreach (var registered in removeRegisteredSet) {
            removeProtected(registered);
        }
        removeRegisteredSet.Clear();
    }

    protected List<List<IEventTrigger>> getRunTimeEventTrigger(Type eventType) {
        if (!runTimeEventBus.TryGetValue(eventType, out var runTimeBus)) {
            IList<Type> can = eventType.getParents();
            runTimeBus = new List<List<IEventTrigger>>(can.Count);
            foreach (var type in can) {
                if (!eventBus.TryGetValue(type, out var bus)) {
                    bus = new List<IEventTrigger>();
                    eventBus.Add(type, bus);
                }
                runTimeBus.Add(bus);
            }
            runTimeEventBus.Add(eventType, runTimeBus);
        }
        return runTimeBus;
    }

    [Event(eventAttributeType = EventAttributeType.no)]
    public Event onEvent(Event @event) {
        deletionDetection();
        List<List<IEventTrigger>> runTimeBus = getRunTimeEventTrigger(@event.GetType());
        eventStack.Push(@event);

        foreach (var bus in runTimeBus) {
            interrupt:
            if (bus.Count == 0) {
                continue;
            }
            foreach (var eventTrigger in bus) {
                try {
                    eventTrigger.invoke(@event);
                }
                catch (Exception e) {
                    getLog()?.Info($"处理事件{@event}时出现异常:", e);
                    foreach (var eventExceptionHandle in eventBusRule.forEventExceptionHandle()) {
                        ExceptionHandleType exceptionHandleType = eventExceptionHandle.doCatch(this, eventTrigger, @event, e);
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
        end:
        deletionDetection();

        eventStack.Pop();
        return @event;
    }

    [Event(eventAttributeType = EventAttributeType.no)]
    public IEnumerable onEvent_coroutine(Event @event) {
        deletionDetection();
        List<List<IEventTrigger>> runTimeBus = getRunTimeEventTrigger(@event.GetType());
        foreach (var bus in runTimeBus) {
            interrupt:
            if (bus.Count == 0) {
                continue;
            }
            foreach (var eventTrigger in bus) {
                object? invoke = null;
                try {
                    invoke = eventTrigger.invoke(@event);
                }
                catch (Exception e) {
                    getLog()?.Info($"处理事件{@event}时出现异常:", e);
                    foreach (var eventExceptionHandle in eventBusRule.forEventExceptionHandle()) {
                        ExceptionHandleType exceptionHandleType = eventExceptionHandle.doCatch(this, eventTrigger, @event, e);
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

                IEnumerator? enumerator = null;
                switch (invoke) {
                    case IEnumerable enumerable:
                        enumerator = enumerable.GetEnumerator();
                        break;
                    case IEnumerator _enumerator:
                        enumerator = _enumerator;
                        break;
                }

                if (enumerator != null) {
                    while (enumerator.MoveNext()) {
                        yield return enumerator.Current;
                    }
                    (enumerator as IDisposable)?.Dispose();
                }

                success:
                if (!@event.isContinue()) {
                    break;
                }
            }
        }
        deletionDetection();
        end: ;
    }

    protected void removeProtected(object registered) {
        allRegistered.Remove(registered);
        foreach (var keyValuePair in eventBus) {
            List<IEventTrigger> eventTriggers = keyValuePair.Value;
            for (var index = 0; index < eventTriggers.Count; index++) {
                IEventTrigger eventTrigger = eventTriggers[index];
                if (!registered.Equals(eventTrigger.getEventRegistrant())) {
                    continue;
                }
                eventTriggers.RemoveAt(index);
                index--;
                remove_cache.Add(eventTrigger);
            }
        }
        if (remove_cache.Count > 0) {
            log?.Info($"从{registered}中删除监听:{string.Join(",", remove_cache.Select(trigger => trigger.getMethodInfo().ToString()))}");
        }
        remove_cache.Clear();
    }

    public void remove(object registered) {
        if (!allRegistered.Contains(registered)) {
            return;
        }
        if (eventStack.Count == 0) {
            removeProtected(registered);
            return;
        }
        removeRegisteredSet.Add(registered);
    }

    public ISet<object> getAllRegistered() => allRegistered;

    public void setRule(IEventBusRule _eventBusRule) {
        eventBusRule = _eventBusRule;
    }

    public IEventBusRule getRule() => eventBusRule;

    public ILog? getLog() => log;

    public void setLog(ILog _log) => log = _log;
}