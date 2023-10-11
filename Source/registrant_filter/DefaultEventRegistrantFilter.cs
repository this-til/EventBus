
namespace EventBus; 

public class DefaultEventRegistrantFilter : SingletonPatternClass<DefaultEventRegistrantFilter>, IEventRegistrantFilter {

    public bool isFilter(IEventBus eventBus, object registrant) {
        if (registrant.GetType().IsPrimitive) {
            eventBus.getLog().Warn($"注册项[{registrant}]是基础数据类型，他不能被注册");
            return true;
        }
        if (registrant.GetType().IsValueType) {
            eventBus.getLog().Warn($"注册项[{registrant}]是结构体，他不能被注册");
            return true;
        }
        if (registrant.GetType().IsEnum) {
            eventBus.getLog().Warn($"注册项[{registrant}]是枚举，他不能被注册");
            return true;
        }
        return false;
    }
}