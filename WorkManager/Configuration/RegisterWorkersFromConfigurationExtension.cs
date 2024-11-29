using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorkManager.Configuration;

/// <summary>
///     Extension for registering works from a configuration file
/// </summary>
public static class RegisterWorkersFromConfigurationExtension
{
    private static readonly Dictionary<string, Type> _workerMap = new();

    /// <summary>
    ///     Static constructor which finds workers based on an attribute on the class
    /// </summary>
    static RegisterWorkersFromConfigurationExtension()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        foreach (Type type in GetExternalServiceConfigurations(assembly))
        {
            WorkerAttribute? attribute = (WorkerAttribute)type.GetCustomAttribute(typeof(WorkerAttribute));
            _workerMap[attribute.WorkerName] = type;
        }
    }

    /// <summary>
    /// Gets all types that have the WorkerAttribute from a specific assembly
    /// </summary>
    /// <param name="assembly">The assembly we are searching through</param>
    /// <returns>Yields a Type object</returns>
    private static IEnumerable<Type> GetExternalServiceConfigurations(Assembly assembly)
    {
        return assembly.GetTypes().Where(type => type.GetCustomAttributes(typeof(WorkerAttribute), true).Length > 0);
    }

    /// <summary>
    /// Registers all workers that are set in the configuration being loaded
    /// </summary>
    /// <param name="serviceCollection">The app service collection</param>
    /// <param name="configuration">The app configuration</param>
    /// <returns>The service collection that was passed in</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IServiceCollection RegisterWorkers(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        IConfigurationSection workersSection = configuration.GetSection("workers");
        
        // Loop through all workers
        foreach (IConfigurationSection workerSection in workersSection.GetChildren())
        {
            string workerName = workerSection.GetValue<string>("workerName")!;
            
            // Get the worker object
            Type serviceType = ValidateAndGetWorkerType(workerName);
            try
            {
                // Look for any options for this specific worker and register it
                IConfigurationSection optionsSection = workerSection.GetSection("options");
                if (optionsSection.Exists())
                {
                    RegisterOptionsFromConfigurationExtension.AddOptionsWithValidateOnStart(serviceCollection,
                        optionsSection, $"{workerName}");
                }

                // Loop through all interfaces for this worker object and register this object to each interface
                Type? curType = serviceType;
                do
                {
                    foreach (Type interfaceType in serviceType.GetInterfaces())
                    {
                        serviceCollection.AddTransient(interfaceType, serviceType);
                    }

                    serviceCollection.AddTransient(curType, serviceType);
                    curType = serviceType.BaseType;
                } while (curType != null && curType != typeof(object));
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to register service: '{workerName}'", e);
            }
        }

        return serviceCollection;
    }

    /// <summary>
    /// Retrieves the Type and validates that it actually exists
    /// </summary>
    /// <param name="workerName">The name of the worker being configured</param>
    /// <returns>The worker type</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static Type ValidateAndGetWorkerType(string workerName)
    {
        if (!_workerMap.TryGetValue(workerName, out Type? workerType))
        {
            throw new InvalidOperationException($"No service class found for '{workerName}'.");
        }

        return workerType;
    }
}