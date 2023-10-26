namespace EventBus;

public interface ILogOut {
    void Info(object message);
    void Warn(object message);
    void Error(object message);
}