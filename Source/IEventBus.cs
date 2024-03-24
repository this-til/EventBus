using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;

namespace EventBus {
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

        ILog getLog();
    }

    [EventSupplierExclude]
    public class EventBus : IEventBus {
        protected readonly HashSet<object> allRegistered = new HashSet<object>();
        protected readonly HashSet<object> removeRegisteredSet = new HashSet<object>();

        protected readonly Dictionary<Type, List<IEventTrigger>> eventBus = new Dictionary<Type, List<IEventTrigger>>();
        protected readonly Dictionary<Type, List<List<IEventTrigger>>> runTimeEventBus = new Dictionary<Type, List<List<IEventTrigger>>>();

        protected readonly Dictionary<Type, List<IEventTrigger>> eventBus_coroutine = new Dictionary<Type, List<IEventTrigger>>();
        protected readonly Dictionary<Type, List<List<IEventTrigger>>> runTimeEventBus_coroutine = new Dictionary<Type, List<List<IEventTrigger>>>();

        protected ILog? log;
        protected IEventBusRule eventBusRule = EventBusRule.defaultEventBusRule;

        protected List<IEventTrigger> remove_cache = new List<IEventTrigger>();
        protected List<IEventTrigger> remove_coroutine_cache = new List<IEventTrigger>();

        protected List<IEventTrigger> add_cache = new List<IEventTrigger>();
        protected List<IEventTrigger> add_coroutine_cache = new List<IEventTrigger>();

        public EventBus() {
            log = LogManager.GetLogger(GetType());
        }

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

                bool isCoroutine = methodInfoReturnType == typeof(IEnumerable) || methodInfoReturnType == typeof(IEnumerator);
                if (!isCoroutine && methodInfoReturnType != typeof(void)) {
                    log?.Warn($"{registered}中{methodInfo}返回值不是{typeof(void)},{typeof(IEnumerable)},{typeof(IEnumerator)},但他不会被拍抛弃");
                }

                Dictionary<Type, List<IEventTrigger>> _eventBus = isCoroutine ? eventBus_coroutine : eventBus;
                if (!_eventBus.TryGetValue(eventType, out var list)) {
                    list = new List<IEventTrigger>();
                    _eventBus.Add(eventType, list);
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

                (isCoroutine ? add_coroutine_cache : add_cache).Add(newEventTrigger);

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
                end: ;
            }
        }

        [Event(eventAttributeType = EventAttributeType.no)]
        public Event onEvent(Event @event) {
            if (removeRegisteredSet.Count > 0) {
                foreach (var registered in removeRegisteredSet) {
                    removeProtected(registered);
                }
                removeRegisteredSet.Clear();
            }
            if (!runTimeEventBus.TryGetValue(@event.GetType(), out var runTimeBus)) {
                List<Type> can = @event.GetType().getParents();
                runTimeBus = new List<List<IEventTrigger>>(can.Count);
                foreach (var type in can) {
                    if (!eventBus.TryGetValue(type, out var bus)) {
                        bus = new List<IEventTrigger>();
                        eventBus.Add(type, bus);
                    }
                    runTimeBus.Add(bus);
                }
                runTimeEventBus.Add(@event.GetType(), runTimeBus);
            }

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
            return @event;
        }

        [Event(eventAttributeType = EventAttributeType.no)]
        public IEnumerable onEvent_coroutine(Event @event) {
            if (removeRegisteredSet.Count > 0) {
                foreach (var registered in removeRegisteredSet) {
                    removeProtected(registered);
                }
                removeRegisteredSet.Clear();
            }

            onEvent(@event);

            if (!runTimeEventBus_coroutine.TryGetValue(@event.GetType(), out var runTimeBus)) {
                List<Type> can = @event.GetType().getParents();
                runTimeBus = new List<List<IEventTrigger>>(can.Count);
                foreach (var type in can) {
                    if (!eventBus_coroutine.TryGetValue(type, out var bus)) {
                        bus = new List<IEventTrigger>();
                        eventBus_coroutine.Add(type, bus);
                    }
                    runTimeBus.Add(bus);
                }
                runTimeEventBus_coroutine.Add(@event.GetType(), runTimeBus);
            }

            foreach (var bus in runTimeBus) {
                interrupt:
                if (bus.Count == 0) {
                    continue;
                }
                foreach (var eventTrigger in bus) {
                    object invoke = null;
                    IEnumerator enumerator = null;
                    while (invoke == null || enumerator != null) {
                        try {
                            if (invoke == null) {
                                invoke = eventTrigger.invoke(@event);
                                switch (invoke) {
                                    case IEnumerable enumerable:
                                        enumerator = enumerable.GetEnumerator();
                                        break;
                                    case IEnumerator _enumerator:
                                        enumerator = _enumerator;
                                        break;
                                }
                            }
                            else {
                                if (!enumerator.MoveNext()) {
                                    (enumerator as IDisposable).Dispose();
                                    enumerator = null;
                                    break;
                                }
                            }
                        }
                        catch (Exception e) {
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

                        yield return enumerator.Current;
                    }

                    success:
                    if (!@event.isContinue()) {
                        break;
                    }
                }
            }
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

            foreach (var keyValuePair in eventBus_coroutine) {
                List<IEventTrigger> eventTriggers = keyValuePair.Value;
                for (var index = 0; index < eventTriggers.Count; index++) {
                    IEventTrigger eventTrigger = eventTriggers[index];
                    if (!registered.Equals(eventTrigger.getEventRegistrant())) {
                        continue;
                    }

                    eventTriggers.RemoveAt(index);
                    index--;

                    remove_coroutine_cache.Add(eventTrigger);
                }
            }

            if (remove_cache.Count > 0 || remove_coroutine_cache.Count > 0) {
                log?.Info($"从{registered}中删除监听:{string.Join(',', remove_cache.Select(trigger => trigger.getMethodInfo().ToString()))} 删除携程监听{string.Join(',', remove_coroutine_cache.Select(trigger => trigger.getMethodInfo().ToString()))}");
            }

            remove_cache.Clear();
            remove_coroutine_cache.Clear();
        }

        public void remove(object registered) {
            if (!allRegistered.Contains(registered)) {
                return;
            }
            removeRegisteredSet.Add(registered);
        }

        public ISet<object> getAllRegistered() => allRegistered;

        public void setRule(IEventBusRule _eventBusRule) {
            eventBusRule = _eventBusRule;
        }

        public IEventBusRule getRule() => eventBusRule;

        public ILog getLog() => log;
    }
}