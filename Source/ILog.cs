using System;


namespace Til.EventBus {
    /// <summary>
    ///  定义一个ILog接口，用于记录日志
    /// </summary>
    public interface ILog {
        /// <summary>
        /// 记录一条信息日志
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        void Info(string message, Exception? exception = null);
        /// <summary>
        /// 记录一条警告日志
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        void Warn(string message, Exception? exception = null);
        /// <summary>
        /// 记录一条错误日志
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        void Error(string message, Exception? exception = null);
    }
}