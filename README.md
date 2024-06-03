# EventBus

一个高效的事件总线

## 如何使用

    < C#
    using System;
    using System.Collections;
    using til.EventBus;
    
    public class Demo {
    public static void Main() {
            IEventBus eventBus = new EventBus();
            
            //自动注入带唯一Event形参的方法
            //Type 注入静态方法
            eventBus.put(typeof(Demo));
            eventBus.put(new Demo());
    
            eventBus.onEvent(new Event());
            eventBus.onEvent(new DemoEvent("aabb"));
    
            //发布一个携程事件 最初是给unity的携程使用的
            foreach (var o in eventBus.onEvent_coroutine(new CoroutineEvent())) {
            }
        }
    
        /// <summary>
        /// 静态监听吗，通过  eventBus.put(typeof(Demo)); 自动注入
        /// eventBus.onEvent(new DemoEvent()); 时也会被调用因为Event是DemoEvent超类
        /// </summary>
        public static void onEventStatic(Event e) {
        }
    
        /// <summary>
        /// 通过EventAttribute注解排除监听
        /// </summary>
        /// <param name="e"></param>
        [Event(eventAttributeType = EventAttributeType.no)]
        public static void excludedEvent(Event e) {
        }
    
        public static IEnumerable onCoroutineEvent(CoroutineEvent @event) {
            yield return null;
        }
    
        /// <summary>
        /// 实例中的监听通过 eventBus.put(new Demo()); 自动注入
        /// </summary>
        /// <param name="e"></param>
        public void onEvent(Event e) {
        }
    
        public class DemoEvent : Event {
            /// <summary>
            /// 通过包装传递数据
            /// </summary>
            public string data;
    
            public DemoEvent(string data) {
                this.data = data ?? throw new ArgumentNullException(nameof(data));
            }
        }
    
        public class CoroutineEvent : Event {
        }
    }    


    

    
    
