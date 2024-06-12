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
    public static readonly Dictionary<Type, List<Type>> types = new Dictionary<Type, List<Type>>();

    /// <summary>
    /// 定义一个方法，用于获取指定类型的父类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static List<Type> getParents(this Type type) {
        // 如果字典中存在该类型的父类型，则直接返回
        if (types.TryGetValue(type, out List<Type>? list)) {
            return list;
        }
        // 否则，递归获取该类型的父类型，并将结果存储在字典中
        list = type._getParents(new List<Type>(), false);
        types.Add(type, list);
        return list;
    }

    /// <summary>
    /// 定义一个私有方法，用于递归获取指定类型的父类型
    /// </summary>
    /// <param name="type"></param>
    /// <param name="list"></param>
    /// <param name="hasInterfaces"></param>
    /// <returns></returns>
    public static List<Type> _getParents(this Type type, List<Type> list, bool hasInterfaces = false) {
        // 将指定类型添加到列表中
        list.Add(type);
        // 如果指定类型实现了接口，则将接口添加到列表中
        if (hasInterfaces) {
            foreach (var @interface in type.GetInterfaces()) {
                // 如果列表中已经存在该接口，则跳过
                if (list.Contains(@interface)) {
                    continue;
                }
                // 将接口添加到列表中
                list.Add(@interface);
            }
        }
        // 获取指定类型的基类型
        Type? baseType = type.BaseType;
        // 如果基类型为空或者基类型为object，则返回列表
        if (baseType == null || baseType == typeof(object)) {
            return list;
        }
        // 否则，递归获取基类型的父类型，并将结果添加到列表中
        return _getParents(baseType, list, hasInterfaces);
    }
}

/// <summary>
/// 定义一个类，用于实现单例模式
/// </summary>
/// <typeparam name="T"></typeparam>
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