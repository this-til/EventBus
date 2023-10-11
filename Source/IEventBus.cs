using log4net;
using log4net.Core;

namespace EventBus;

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
    /// 设置日志输出接口
    /// </summary>
    void setLog(ILog logger);
    
    /// <summary>
    /// 获取事件系统的一个日志输出接口
    /// </summary>
    ILog getLog();

    void addEventRegistrantFilter(IEventRegistrantFilter eventRegistrantFilter);

    void addEventTriggerFilter(IEventTriggerFilter eventTriggerFilter);

    void addEventTriggerFactory(IEventTriggerFactory eventTriggerFactory);

    void addEventExceptionHandle(IEventExceptionHandle eventExceptionHandle);
}