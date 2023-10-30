namespace EventBus {
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class EventAttribute : System.Attribute {
        public EventAttributeType eventAttributeType;

        /// <summary>
        /// 优先级
        /// </summary>
        public int priority;
    }

    /// <summary>
    /// 排除供应商
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class EventSupplierExcludeAttribute : Attribute {
        public bool excludeState = true;
        public bool excludeStateInstance = true;
    }
}