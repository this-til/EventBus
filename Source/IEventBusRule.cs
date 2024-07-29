using System.Collections.Generic;

namespace Til.EventBus {
    /// <summary>
    /// 事件的规则
    /// </summary>
    public interface IEventBusRule {
        //void addEventRegistrantFilter(IEventRegistrantFilter eventRegistrantFilter);
        //void addEventTriggerFilter(IEventTriggerFilter eventTriggerFilter);
        //void addEventTriggerFactory(IEventTriggerFactory eventTriggerFactory);
        //void addEventExceptionHandle(IEventExceptionHandle eventExceptionHandle);

        /// <summary>
        /// 用于事件注册人过滤
        /// </summary>
        /// <returns></returns>
        IEnumerable<IEventRegistrantFilter> forEventRegistrantFilter();

        /// <summary>
        /// 用于事件触发器过滤
        /// </summary>
        /// <returns></returns>
        IEnumerable<IEventTriggerFilter> forEventTriggerFilter();

        /// <summary>
        /// 用于生成事件触发器
        /// </summary>
        /// <returns></returns>
        IEnumerable<IEventTriggerFactory> forEventTriggerFactory();

        /// <summary>
        /// 用于事件异常处理
        /// </summary>
        IEnumerable<IEventExceptionHandle> forEventExceptionHandle();
    }

    /// <summary>
    /// 默认的EventBus规则
    /// </summary>
    public class EventBusRule : IEventBusRule {
        /// <summary>
        /// 默认的EventBus规则
        /// </summary>
        public static readonly EventBusRule defaultEventBusRule = new EventBusRule();

        /// <summary>
        /// 事件注册者过滤器列表
        /// </summary>
        protected readonly List<IEventRegistrantFilter> eventRegistrantFilterList = new List<IEventRegistrantFilter>();

        /// <summary>
        /// 事件触发过滤器列表
        /// </summary>
        protected readonly List<IEventTriggerFilter> eventTriggerFilterList = new List<IEventTriggerFilter>();

        /// <summary>
        /// 事件触发工厂列表
        /// </summary>
        protected readonly List<IEventTriggerFactory> eventTriggerFactoryList = new List<IEventTriggerFactory>();

        /// <summary>
        /// 事件异常处理列表
        /// </summary>
        protected readonly List<IEventExceptionHandle> eventExceptionHandles = new List<IEventExceptionHandle>();

        /// <summary>
        /// 后缀
        /// </summary>
        public EventBusRule() {
            // 添加事件注册者过滤器
            addEventRegistrantFilter(EventRegistrantTypeFilter.getInstance());
            addEventRegistrantFilter(EventRegistrantExcludeAttributeFilter.getInstance());
            // 添加事件触发过滤器
            addEventTriggerFilter(DefaultEventTriggerFilter.getInstance());
            // 添加事件触发工厂
            addEventTriggerFactory(DefaultEventTriggerFactory.getInstance());
        }

        /// <summary>
        ///  添加事件注册者过滤器
        /// </summary>
        /// <param name="eventRegistrantFilter"></param>
        public void addEventRegistrantFilter(IEventRegistrantFilter eventRegistrantFilter) {
            eventRegistrantFilterList.Insert(0, eventRegistrantFilter);
        }

        /// <summary>
        /// 添加事件触发过滤器
        /// </summary>
        /// <param name="eventTriggerFilter"></param>
        public void addEventTriggerFilter(IEventTriggerFilter eventTriggerFilter) {
            eventTriggerFilterList.Insert(0, eventTriggerFilter);
        }

        /// <summary>
        /// 添加事件触发工厂
        /// </summary>
        /// <param name="eventTriggerFactory"></param>
        public void addEventTriggerFactory(IEventTriggerFactory eventTriggerFactory) {
            eventTriggerFactoryList.Insert(0, eventTriggerFactory);
        }

        /// <summary>
        /// 添加事件异常处理
        /// </summary>
        /// <param name="eventExceptionHandle"></param>
        public void addEventExceptionHandle(IEventExceptionHandle eventExceptionHandle) {
            eventExceptionHandles.Insert(0, eventExceptionHandle);
        }

        /// <summary>
        /// 获取事件注册者过滤器
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEventRegistrantFilter> forEventRegistrantFilter() => eventRegistrantFilterList;

        /// <summary>
        /// 获取事件触发过滤器
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEventTriggerFilter> forEventTriggerFilter() => eventTriggerFilterList;

        /// <summary>
        /// 获取事件触发工厂
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEventTriggerFactory> forEventTriggerFactory() => eventTriggerFactoryList;

        /// <summary>
        /// 获取事件异常处理
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEventExceptionHandle> forEventExceptionHandle() => eventExceptionHandles;
    }
}