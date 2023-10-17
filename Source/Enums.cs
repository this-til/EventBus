﻿namespace EventBus; 


[Flags]
public enum EventAttributeType {
    /// <summary>
    /// 方法不作为事件
    /// </summary>
    no = 1 << 0,

    /// <summary>
    /// 是测试事件
    /// </summary>
    test = 1 << 1,
}
