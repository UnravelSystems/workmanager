using System.Reflection;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkManager.Configuration.Interfaces;

namespace WorkManager.Configuration;

public static class RegisterServiceFromConfigurationExtension
{
    private static Dictionary<string, Dictionary<string, Type>> _serviceMap = new Dictionary<string, Dictionary<string, Type>>();

    static RegisterServiceFromConfigurationExtension()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in GetExternalServiceConfigurations(assembly))
            {
                ServiceConfigurationAttribute attribute = (ServiceConfigurationAttribute)type.GetCustomAttribute(typeof(ServiceConfigurationAttribute));
                if (!_serviceMap.ContainsKey(attribute.ServiceName))
                {
                    _serviceMap.Add(attribute.ServiceName, new Dictionary<string, Type>());
                }
                
                _serviceMap[attribute.ServiceName].Add(attribute.ServiceType, type);
            }
        }
    }
    
    static IEnumerable<Type> GetExternalServiceConfigurations(Assembly assembly) {
        foreach(Type type in assembly.GetTypes()) {
            if (type.GetCustomAttributes(typeof(ServiceConfigurationAttribute), true).Length > 0) {
                yield return type;
            }
        }
    }

    static ServiceLifetime GetServiceScope(Type serviceType)
    {
        ServiceConfigurationAttribute attribute = (ServiceConfigurationAttribute)serviceType.GetCustomAttribute(typeof(ServiceConfigurationAttribute))!;
        return attribute.Scope;
    }
    
    public static IServiceCollection RegisterServicesFromConfiguration(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        IConfigurationSection servicesSection = configuration.GetSection("services");
        HashSet<string> seenServices = new();
        foreach (IConfigurationSection serviceSection in servicesSection.GetChildren())
        {
            string serviceName = serviceSection.GetValue<string>("serviceName")!;
            string serviceTypeName = serviceSection.GetValue<string>("serviceType", "default")!;
            Type serviceType = ValidateAndGetServiceType(serviceName, serviceTypeName);
            
            ValidateDependsOn(serviceSection, seenServices, serviceName);

            try
            {
                IConfigurationSection optionsSection = serviceSection.GetSection("options");
                if (optionsSection.Exists())
                {
                    RegisterOptionsFromConfigurationExtension.AddOptionsWithValidateOnStart(serviceCollection, optionsSection, $"{serviceName}.{serviceTypeName}");
                }
                
                // More complex type of service builder, call the appropriate method for configuring the services
                if (typeof(ExternalServiceBuilder).IsAssignableFrom(serviceType))
                {
                    ExternalServiceBuilder instance = (ExternalServiceBuilder)Activator.CreateInstance(serviceType)!;
                    instance.ConfigureServices(serviceCollection, serviceSection);
                }
                else
                {
                    // Simple service
                    RegisterService(serviceCollection, serviceType, GetServiceScope(serviceType));
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Failed to register service: '{serviceName}'", e);
            }
            
            seenServices.Add(serviceName.ToLower());
        }

        return serviceCollection;
    }

    private static void RegisterService(IServiceCollection serviceCollection, Type serviceType, ServiceLifetime scope)
    {
        Type[] interfaces = serviceType.GetInterfaces();

        foreach (Type @interface in interfaces)
        {
            switch (scope)
            {
                case ServiceLifetime.Singleton:
                    serviceCollection.AddSingleton(@interface, serviceType);
                    break;
                case ServiceLifetime.Scoped:
                    serviceCollection.AddScoped(@interface, serviceType);
                    break;
                case ServiceLifetime.Transient:
                    serviceCollection.AddTransient(@interface, serviceType);
                    break;
                default:
                    throw new InvalidOperationException($"The service-scope '{scope}' is not supported.");
            }
        }
    }

    private static Type ValidateAndGetServiceType(string serviceName, string serviceTypeName)
    {
        if (!_serviceMap.TryGetValue(serviceName, out Dictionary<string, Type> serviceTypeMap))
        {
            throw new InvalidOperationException($"No service class found for '{serviceName}'.");
        }
            
        if (!serviceTypeMap.TryGetValue(serviceTypeName, out Type serviceType))
        {
            throw new InvalidOperationException($"No service type found for '{serviceName}/{serviceTypeName}'.");
        }

        return serviceType;
    }

    private static void ValidateDependsOn(IConfiguration serviceConfiguration, HashSet<string> seenServices, String serviceName)
    {
            
        IConfigurationSection dependsOn = serviceConfiguration.GetSection("dependsOn");
        if (dependsOn.Exists())
        {
            foreach (IConfigurationSection dependsOnSection in dependsOn.GetChildren())
            {
                string? dependsOnServiceName = dependsOnSection.Value;
                if (!string.IsNullOrEmpty(dependsOnServiceName) && !seenServices.Contains(dependsOnServiceName.ToLower()))
                {
                    throw new ConfigurationException($"Missing Dependency: Service '{serviceName}' depends on '{dependsOnServiceName}'.");
                }
            }
        }
    }
}