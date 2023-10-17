namespace EventBus; 

public static class EventTypeTool {
    public static readonly Dictionary<Type, List<Type>> types = new Dictionary<Type, List<Type>>();

    public static List<Type> getParents(this Type type) {
        if (types.ContainsKey(type)) {
            return types[type];
        }
        List<Type> list = type._getParents(new List<Type>());
        types.Add(type, list);
        return list;
    }

    public static List<Type> _getParents(this Type type, List<Type> list, bool hasInterfaces = false) {
        list.Add(type);
        if (hasInterfaces) {
            foreach (var @interface in type.GetInterfaces()) {
                if (list.Contains(@interface)) {
                    continue;
                }
                list.Add(@interface);
            }
        }
        Type? baseType = type.BaseType;
        if (baseType == null || baseType == typeof(object)) {
            return list;
        }
        return _getParents(baseType, list, hasInterfaces);
    }
}

public class SingletonPatternClass<T> where T : new() {
    protected static T? instance;

    public static T getInstance() => instance ??= new T();

    protected SingletonPatternClass() {
    }
}