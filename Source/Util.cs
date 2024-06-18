using System;
using System.Collections.Generic;

namespace Til.EventBus;

/// <summary>
/// 定义一个类，用于获取类型之间的关系
/// </summary>
public static class EventTypeTool {
    /// <summary>
    /// 定义一个字典，用于存储类型之间的关系
    /// </summary>
    public static readonly Dictionary<Type, IList<Type>> types = new Dictionary<Type, IList<Type>>();

    private static readonly IList<Type> emptyList = new List<Type>();

    /// <summary>
    /// 定义一个方法，用于获取指定类型的所有父类型 [this,object)
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IList<Type> getParents(this Type? type) {
        if (type is null) {
            return emptyList;
        }
        if (types.TryGetValue(type, out IList<Type>? list)) {
            return list;
        }

        Type? baseType = type;
        list = new List<Type>();
        while (baseType is not null && baseType != typeof(object)) {
            list.Add(baseType);
            baseType = baseType.BaseType;
        }
        types.Add(type, list);
        return list;
    }
}

/// <summary>
/// 定义一个类，用于实现单例模式
/// </summary>
public class SingletonPatternClass<T> where T : new() {
    /// <summary>
    ///  定义一个静态属性，用于存储实例
    /// </summary>
    protected static T? instance;

    /// <summary>
    /// 定义一个静态方法，用于获取实例
    /// </summary>
    /// <returns></returns>
    public static T getInstance() => instance ??= new T();

    /// <summary>
    /// 定义一个构造方法，用于防止实例被实例化
    /// </summary>
    protected SingletonPatternClass() {
    }
}