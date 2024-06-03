using System.Collections.Generic;

namespace EventBus {
    /// <summary>
    /// 事件的规则
    /// </summary>
    public interface IEventBusRule {
        //void addEventRegistrantFilter(IEventRegistrantFilter eventRegistrantFilter);
        //void addEventTriggerFilter(IEventTriggerFilter eventTriggerFilter);
        //void addEventTriggerFactory(IEventTriggerFactory eventTriggerFactory);
        //void addEventExceptionHandle(IEventExceptionHandle eventExceptionHandle);

        IEnumerable<IEventRegistrantFilter> forEventRegistrantFilter();
        IEnumerable<IEventTriggerFilter> forEventTriggerFilter();
        IEnumerable<IEventTriggerFactory> forEventTriggerFactory();
        IEnumerable<IEventExceptionHandle> forEventExceptionHandle();
    }

    public class EventBusRule : IEventBusRule {
        public static readonly EventBusRule defaultEventBusRule = new EventBusRule();

        protected readonly List<IEventRegistrantFilter> eventRegistrantFilterList = new List<IEventRegistrantFilter>();
        protected readonly List<IEventTriggerFilter> eventTriggerFilterList = new List<IEventTriggerFilter>();
        protected readonly List<IEventTriggerFactory> eventTriggerFactoryList = new List<IEventTriggerFactory>();
        protected readonly List<IEventExceptionHandle> eventExceptionHandles = new List<IEventExceptionHandle>();
        

        public EventBusRule() {
            addEventRegistrantFilter(EventRegistrantTypeFilter.getInstance());
            addEventRegistrantFilter(EventRegistrantExcludeAttributeFilter.getInstance());
            addEventTriggerFilter(DefaultEventTriggerFilter.getInstance());
            addEventTriggerFactory(DefaultEventTriggerFactory.getInstance());
            addEventExceptionHandle(DefaultEventExceptionHandle.getInstance());
        }

        public void addEventRegistrantFilter(IEventRegistrantFilter eventRegistrantFilter) {
            eventRegistrantFilterList.Insert(0, eventRegistrantFilter);
        }

        public void addEventTriggerFilter(IEventTriggerFilter eventTriggerFilter) {
            eventTriggerFilterList.Insert(0, eventTriggerFilter);
        }

        public void addEventTriggerFactory(IEventTriggerFactory eventTriggerFactory) {
            eventTriggerFactoryList.Insert(0, eventTriggerFactory);
        }

        public void addEventExceptionHandle(IEventExceptionHandle eventExceptionHandle) {
            eventExceptionHandles.Insert(0, eventExceptionHandle);
        }
        

        public IEnumerable<IEventRegistrantFilter> forEventRegistrantFilter() => eventRegistrantFilterList;

        public IEnumerable<IEventTriggerFilter> forEventTriggerFilter() => eventTriggerFilterList;

        public IEnumerable<IEventTriggerFactory> forEventTriggerFactory() => eventTriggerFactoryList;

        public IEnumerable<IEventExceptionHandle> forEventExceptionHandle() => eventExceptionHandles;
    }
}