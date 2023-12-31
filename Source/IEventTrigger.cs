﻿using System;
using System.Reflection;

namespace EventBus {
    /// <summary>
    /// 事件触发器
    /// 用来调用事件回调
    /// </summary>
    public interface IEventTrigger {
        /// <summary>
        /// 事件被调用
        /// </summary>
        void invoke(Event @event);

        /// <summary>
        /// 获取事件的注册者
        /// </summary>
        object getEventRegistrant();

        /// <summary>
        /// 获取事件类型
        /// </summary>
        Type getEventType();

        /// <summary>
        /// 获取事件调用的优先级
        /// </summary>
        int getEventPriority();
    }

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
}