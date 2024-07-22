using System;

namespace Til.EventBus {
    public interface IEventExceptionHandle {
        /// <summary>
        /// 进行抛出异常的处理
        /// 返回true表示类型已经被
        /// </summary>
        ExceptionHandleType doCatch(IEventBus iEventBus, IEventTrigger eventTrigger, Event @event, Exception exception);
    }

    /// <summary>
    /// 异常处理结果类型
    /// </summary>
    public enum ExceptionHandleType {
        /// <summary>
        /// 表示异常被处理
        /// </summary>
        success,

        /// <summary>
        /// 表示异常被处理，但是需要中断事件
        /// </summary>
        success_interrupt,

        /// <summary>
        /// 表示异常被处理，但是直接结束事件
        /// </summary>
        success_end,

        /// <summary>
        /// 向外部抛出异常
        /// </summary>
        @throw,

        /// <summary>
        /// 跳过该异常处理
        /// </summary>
        skip
    }

    /// <summary>
    /// 定义一个单例类，继承自SingletonPatternClass，实现IEventExceptionHandle接口
    /// </summary>
    public class DefaultEventExceptionHandle : SingletonPatternClass<DefaultEventExceptionHandle>, IEventExceptionHandle {
        /// <summary>
        /// 重写doCatch方法
        /// </summary>
        public ExceptionHandleType doCatch(IEventBus iEventBus, IEventTrigger eventTrigger, Event @event, Exception exception) {
            // 返回跳过处理
            return ExceptionHandleType.skip;
        }
    }
}