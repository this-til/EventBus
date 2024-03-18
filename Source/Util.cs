using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using log4net.Core;

namespace EventBus {
    public static class EventTypeTool {
        /*public static Dictionary<Type, int> typeMapping = new Dictionary<Type, int>();
        public static List<Type> typeList = new List<Type>();
        public static List<int[]?> typeParentList = new List<int[]?>();

        public static int getTypeId(Type type) {
            if (typeMapping.ContainsKey(type)) {
                return typeMapping[type];
            }
            int typeListCount = typeList.Count;
            typeMapping.Add(type, typeListCount);
            typeList.Add(type);
            return typeListCount;
        }

        public static Type? getTypeAtId(int id) {
            if (id < 0 || id > typeList.Count) {
                return null;
            }
            return typeList[id];
        }

        public static int[]? getTypeParentIds(int id) {
            if (id < 0 || id >= typeList.Count) {
                return null;
            }
            int[]? array = id < typeParentList.Count ? typeParentList[id] : null;
            if (array is null) {
                Type type = getTypeAtId(id) ?? throw new Exception();
                Type? basicsType = type;
                List<int> types = new List<int>();
                while (basicsType is not null && basicsType == typeof(object)) {
                    types.Add(getTypeId(basicsType));
                    basicsType = basicsType.BaseType;
                }
                while (typeParentList.Count < id) {
                    typeParentList.Add(null);
                }
                typeParentList.Insert(id, types.ToArray());
            }
            return array;
        }

        public static int[] getTypeParentIds(Type type) => getTypeParentIds(getTypeId(type))!;*/

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

    public static class LogUtil {
    }
    
}