using System;

namespace Til.EventBus {
    public interface IEventExceptionHandle {
        /// <summary>
        /// 进行抛出异常的处理
        /// 返回true表示类型已经被
        /// </summary>
        void doCatch(IEventBus iEventBus, IEventTrigger eventTrigger, Event @event, Exception exception);
    }
}