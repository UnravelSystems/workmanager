namespace WorkManager.Configuration;

[AttributeUsage(AttributeTargets.Class)]
public class WorkerAttribute : Attribute
{
    public string WorkerName { get; set; }
    public List<Type> ServiceTypes { get; set; } = new List<Type>();
}