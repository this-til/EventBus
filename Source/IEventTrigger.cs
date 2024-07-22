using System;
using System.Reflection;

namespace Til.EventBus {
    /// <summary>
    /// 事件触发器
    /// 用来调用事件回调
    /// </summary>
    public interface IEventTrigger {
        /// <summary>
        /// 事件被调用
        /// </summary>
        object? invoke(Event @event);

        /// <summary>
        /// 获取事件的注册者
        /// </summary>
        object getEventRegistrant();

        /// <summary>
        /// 获取监听的方法
        /// </summary>
        MethodInfo getMethodInfo();

        /// <summary>
        /// 获取事件类型
        /// </summary>
        Type getEventType();

        /// <summary>
        /// 获取事件调用的优先级
        /// </summary>
        int getEventPriority();
    }

    /// <summary>
    /// 声明一个抽象类EventTrigger，继承自IEventTrigger接口
    /// </summary>
    public abstract class EventTrigger : IEventTrigger {
        /// <summary>
        /// 声明一个只读属性methodInfo，表示方法信息
        /// </summary>
        protected readonly MethodInfo methodInfo;

        /// <summary>
        /// 声明一个只读属性registrant，表示注册者
        /// </summary>
        protected readonly object registrant;

        /// <summary>
        /// 声明一个只读属性eventAttribute，表示事件属性
        /// </summary>
        protected readonly EventAttribute? eventAttribute;

        /// <summary>
        /// 声明一个只读属性type，表示事件类型
        /// </summary>
        protected readonly Type type;

        /// <summary>
        /// 构造函数，传入方法信息、注册者、事件属性、事件类型
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="registrant"></param>
        /// <param name="eventAttribute"></param>
        /// <param name="eventType"></param>
        protected EventTrigger(MethodInfo methodInfo, object registrant, EventAttribute? eventAttribute, Type eventType) {
            this.methodInfo = methodInfo;
            this.registrant = registrant;
            this.eventAttribute = eventAttribute;
            this.type = eventType;
        }

        /// <summary>
        /// 获取注册者
        /// </summary>
        /// <returns></returns>
        public object getEventRegistrant() {
            return registrant;
        }

        /// <summary>
        /// 获取事件优先级
        /// </summary>
        /// <returns></returns>
        public int getEventPriority() {
            return eventAttribute?.priority ?? 0;
        }

        /// <summary>
        /// 调用事件
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public abstract object? invoke(Event @event);

        /// <summary>
        /// 获取事件类型
        /// </summary>
        /// <returns></returns>
        public Type getEventType() => type;

        /// <summary>
        /// 获取方法信息
        /// </summary>
        /// <returns></returns>
        public MethodInfo getMethodInfo() => methodInfo;

        /// <summary>
        /// 静态方法，创建EventTrigger实例
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="eventType"></param>
        /// <param name="methodInfoReturnType"></param>
        /// <param name="use"></param>
        /// <param name="eventAttribute"></param>
        /// <returns></returns>
        public static EventTrigger? create(MethodInfo methodInfo, Type eventType, Type methodInfoReturnType, object use, EventAttribute? eventAttribute) {
            if (methodInfoReturnType == typeof(void)) {
                return Activator.CreateInstance(typeof(EventTrigger<>).MakeGenericType(eventType), methodInfo, use, eventAttribute) as EventTrigger;
            }
            return Activator.CreateInstance(typeof(EventTrigger<,>).MakeGenericType(eventType, methodInfoReturnType), methodInfo, use, eventAttribute) as EventTrigger;
        }
    }

    /// <summary>
    /// 定义一个泛型类EventTrigger，继承自EventTrigger，泛型参数为T，T需要是一个事件类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EventTrigger<T> : EventTrigger where T : Event {
        /// <summary>
        /// 定义一个委托类型eventDelegate，泛型参数为T，T需要是一个事件类
        /// </summary>
        public delegate void eventDelegate(T @event);

        /// <summary>
        /// 保护的属性， readonly，存储一个委托，该委托指向一个方法，该方法接受一个T类型的参数
        /// </summary>
        protected readonly eventDelegate @delegate;

        /// <summary>
        /// 构造函数，传入MethodInfo，object，EventAttribute？类型的参数，类型为T
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="use"></param>
        /// <param name="eventAttribute"></param>
        public EventTrigger(MethodInfo methodInfo, object use, EventAttribute? eventAttribute) : base(methodInfo, use, eventAttribute, typeof(T)) {
            // 将传入的方法信息创建为一个委托，类型为eventDelegate，泛型参数为T
            this.@delegate = (eventDelegate)methodInfo.CreateDelegate(typeof(eventDelegate), methodInfo.IsStatic ? null : use);
        }

        /// <summary>
        /// 重载invoke方法，传入一个Event类型的参数@event
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public override object? invoke(Event @event) {
            // 调用委托，传入一个T类型的参数@event
            @delegate((T)@event);
            // 返回null
            return null;
        }
    }

    /// <summary>
    /// 定义一个泛型类，EventTrigger，其中T为泛型参数，R为泛型参数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="R"></typeparam>
    public class EventTrigger<T, R> : EventTrigger where T : Event {
        /// <summary>
        /// 定义一个泛型委托，eventDelegate，其中T为泛型参数，R为泛型参数
        /// </summary>
        public delegate R eventDelegate(T @event);

        /// <summary>
        /// 保护字段， readonly，指向泛型委托
        /// </summary>
        protected readonly eventDelegate @delegate;

        /// <summary>
        /// 构造函数，传入MethodInfo，object，EventAttribute？
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="use"></param>
        /// <param name="eventAttribute"></param>
        public EventTrigger(MethodInfo methodInfo, object use, EventAttribute? eventAttribute) : base(methodInfo, use, eventAttribute, typeof(T)) {
            // 将MethodInfo转换为泛型委托
            this.@delegate = (eventDelegate)methodInfo.CreateDelegate(typeof(eventDelegate), methodInfo.IsStatic ? null : use);
        }

        /// <summary>
        /// 重写invoke方法，传入Event，返回泛型委托的调用结果
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        public override object? invoke(Event @event) => @delegate((T)@event);
    }
}