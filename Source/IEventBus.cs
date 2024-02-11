using System;
using System.Collections.Generic;
using System.Reflection;

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
        /// 设置日志输出接口
        /// </summary>
        void setLog(ILogOut logger);

        /// <summary>
        /// 获取事件系统的一个日志输出接口
        /// </summary>
        ILogOut? getLog();
    }

    [EventSupplierExclude]
    public class EventBus : IEventBus {
        protected readonly HashSet<object> allRegistered = new HashSet<object>();
        protected readonly Dictionary<Type, List<IEventTrigger>> eventBus = new Dictionary<Type, List<IEventTrigger>>();
        protected readonly Dictionary<Type, List<List<IEventTrigger>>> runTimeEventBus = new Dictionary<Type, List<List<IEventTrigger>>>();
        protected readonly HashSet<object> removeRegisteredSet = new HashSet<object>();

        protected ILogOut? log;
        protected IEventBusRule eventBusRule = EventBusRule.defaultEventBusRule;

        public void put(object registered) {
            getLog()?.Info($"EventBus开始注册事件监听者 监听者:{registered},监听者类型:{registered.GetType()}");

            foreach (var eventRegistrantFilter in eventBusRule.forEventRegistrantFilter()) {
                if (eventRegistrantFilter.isFilter(this, registered)) {
                    getLog()?.Info("EventBus将事件监听被过滤掉了.");
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
                        goto end;
                    }
                }

                if (!eventBus.TryGetValue(eventType, out var list)) {
                    list = new List<IEventTrigger>();
                    eventBus.Add(eventType, list);
                }

                IEventTrigger? newEventTrigger = null;
                foreach (var eventTriggerFactory in eventBusRule.forEventTriggerFactory()) {
                    newEventTrigger = eventTriggerFactory.create(this, registered, eventType, methodInfo, eventAttribute);
                    if (newEventTrigger != null) {
                        break;
                    }
                }
                if (newEventTrigger == null) {
                    getLog()?.Info($"EventBus忽视了方法:{methodInfo}");
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
                getLog()?.Info($"EventBus监听方法注册成功 方法:{methodInfo}");
                end: ;
            }
            getLog()?.Info($"EventBus结束注册事件监听者 监听者:{registered},监听者类型:{registered.GetType()}");
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

        public void setLog(ILogOut logger) {
            this.log = logger;
        }

        public ILogOut? getLog() => this.log;

        public void setRule(IEventBusRule _eventBusRule) {
            eventBusRule = _eventBusRule;
        }

        public IEventBusRule getRule() => eventBusRule;
    }
}