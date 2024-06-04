using System;

namespace Til.EventBus {
    public interface ILog {
        void Info(string message, Exception? exception = null);
        void Warn(string message, Exception? exception = null);
        void Error(string message, Exception? exception = null);
    }
}
