using System.Reflection;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkManager.Configuration.Interfaces;

namespace WorkManager.Configuration;

public static class RegisterWorkersFromConfigurationExtension
{
    private static Dictionary<string, Type> _workerMap = new Dictionary<string, Type>();

    static RegisterWorkersFromConfigurationExtension()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in GetExternalServiceConfigurations(assembly))
            {
                WorkerAttribute attribute = (WorkerAttribute)type.GetCustomAttribute(typeof(WorkerAttribute));
                _workerMap[attribute.WorkerName] = type;
            }
        }
    }
    
    static IEnumerable<Type> GetExternalServiceConfigurations(Assembly assembly) {
        foreach(Type type in assembly.GetTypes()) {
            if (type.GetCustomAttributes(typeof(WorkerAttribute), true).Length > 0) {
                yield return type;
            }
        }
    }
    
    public static IServiceCollection RegisterWorkers(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        IConfigurationSection workersSection = configuration.GetSection("workers");
        foreach (IConfigurationSection workerSection in workersSection.GetChildren())
        {
            string workerName = workerSection.GetValue<string>("workerName")!;
            Type serviceType = ValidateAndGetWorkerType(workerName);

            try
            {
                IConfigurationSection optionsSection = workerSection.GetSection("options");
                if (optionsSection.Exists())
                {
                    RegisterOptionsFromConfigurationExtension.AddOptionsWithValidateOnStart(serviceCollection, optionsSection, $"{workerName}");
                }

                Type curType = serviceType;
                do
                {
                    foreach (Type interfaceType in serviceType.GetInterfaces())
                    {
                        serviceCollection.AddTransient(interfaceType, serviceType);
                    }
                    serviceCollection.AddTransient(curType, serviceType);
                    curType = serviceType.BaseType;
                } while (curType != null && curType != typeof(Object));
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to register service: '{workerName}'", e);
            }
        }

        return serviceCollection;
    }

    private static Type ValidateAndGetWorkerType(string workerName)
    {
        if (!_workerMap.TryGetValue(workerName, out Type workerType))
        {
            throw new InvalidOperationException($"No service class found for '{workerName}'.");
        }
            
        return workerType;
    }
}