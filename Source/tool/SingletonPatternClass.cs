namespace EventBus; 

public class SingletonPatternClass<T> where T : new() {
    protected static T? instance;

    public static T getInstance() => instance ??= new T();

    protected SingletonPatternClass() {
    }
}