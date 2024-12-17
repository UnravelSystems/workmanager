using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkManager.Configuration.Interfaces;

namespace WorkManager.Configuration;

/// <summary>
/// Extension for registering services
/// </summary>
public static class RegisterServiceFromConfigurationExtension
{
    private static readonly Dictionary<string, Dictionary<string, Type>> _serviceMap = new();

    /// <summary>
    /// Static constructor to load up services
    /// </summary>
    static RegisterServiceFromConfigurationExtension()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        foreach (Type type in GetExternalServiceConfigurations(assembly))
        {
            ServiceConfigurationAttribute attribute =
                (ServiceConfigurationAttribute)type.GetCustomAttribute(typeof(ServiceConfigurationAttribute))!;

            if (attribute.ServiceName != null)
            {
                if(!_serviceMap.ContainsKey(attribute.ServiceName))
                {
                    _serviceMap.Add(attribute.ServiceName, new Dictionary<string, Type>());
                }
                _serviceMap[attribute.ServiceName].Add(attribute.ServiceType, type);
            }
        }
    }

    private static IEnumerable<Type> GetServiceTypes(Type type)
    {
        foreach (Type @interface in GetAllInheritedTypes(type))
        {
            yield return @interface;
        }
        
        ServiceConfigurationAttribute attribute =
            (ServiceConfigurationAttribute)type.GetCustomAttribute(typeof(ServiceConfigurationAttribute))!;

        if (attribute.ServiceTypes != null)
        {
            foreach (Type serviceType in attribute.ServiceTypes)
            {
                yield return serviceType;
            }
        }
    }
    
    private static IEnumerable<Type> GetExternalServiceConfigurations(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.GetCustomAttributes(typeof(ServiceConfigurationAttribute), true).Length > 0)
            {
                yield return type;
            }
        }
    }

    private static ServiceLifetime GetServiceScope(Type serviceType)
    {
        ServiceConfigurationAttribute attribute =
            (ServiceConfigurationAttribute)serviceType.GetCustomAttribute(typeof(ServiceConfigurationAttribute))!;
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
                    RegisterOptionsFromConfigurationExtension.AddOptionsWithValidateOnStart(serviceCollection,
                        optionsSection, $"{serviceName}.{serviceTypeName}");
                }

                // More complex type of service builder, call the appropriate method for configuring the services
                if (typeof(IExternalServiceBuilder).IsAssignableFrom(serviceType))
                {
                    IExternalServiceBuilder instance = (IExternalServiceBuilder)Activator.CreateInstance(serviceType)!;
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
        foreach (Type @interface in GetServiceTypes(serviceType))
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

    private static IEnumerable<Type> GetAllInheritedTypes(Type serviceType)
    {
        HashSet<Type> yieldedTypes = new();
        Type curType = serviceType;
        do
        {
            if (!yieldedTypes.Contains(curType))
            {
                yield return curType;
                yieldedTypes.Add(curType);
            }
            foreach (Type interfaceType in curType.GetInterfaces())
            {
                if (!yieldedTypes.Contains(interfaceType))
                {
                    yield return interfaceType;
                    yieldedTypes.Add(interfaceType);
                }
            }
            curType = curType.BaseType;
        } while (curType != null && curType != typeof(object));
    }
    private static IEnumerable<Type> GetInterfaceCombinations(Type serviceType)
    {
        foreach (Type interfaceType in GetAllInheritedTypes(serviceType))
        {
            if (interfaceType.IsInterface)
            {
                Type[] genericArguments = interfaceType.GetGenericArguments();
                if (genericArguments.Length == 1)
                {
                    Type genericType = interfaceType.GetGenericTypeDefinition();
                    
                    foreach (Type subInterface in GetAllInheritedTypes(genericArguments[0]))
                    {
                        yield return genericType.MakeGenericType(subInterface);
                    }
                    yield return genericType.MakeGenericType(genericArguments[0]);
                }
                else
                {
                    yield return interfaceType;
                }
            }
            else
            {
                yield return interfaceType;
            }
        }
    }

    private static Type ValidateAndGetServiceType(string serviceName, string serviceTypeName)
    {
        if (!_serviceMap.TryGetValue(serviceName, out Dictionary<string, Type>? serviceTypeMap))
        {
            throw new InvalidOperationException($"No service class found for '{serviceName}'.");
        }

        if (!serviceTypeMap.TryGetValue(serviceTypeName, out Type? serviceType))
        {
            throw new InvalidOperationException($"No service type found for '{serviceName}/{serviceTypeName}'.");
        }

        return serviceType;
    }

    private static void ValidateDependsOn(IConfiguration serviceConfiguration, HashSet<string> seenServices,
        string serviceName)
    {
        IConfigurationSection dependsOn = serviceConfiguration.GetSection("dependsOn");
        if (dependsOn.Exists())
        {
            foreach (IConfigurationSection dependsOnSection in dependsOn.GetChildren())
            {
                string? dependsOnServiceName = dependsOnSection.Value;
                if (!string.IsNullOrEmpty(dependsOnServiceName) &&
                    !seenServices.Contains(dependsOnServiceName.ToLower()))
                {
                    throw new ConfigurationException(
                        $"Missing Dependency: Service '{serviceName}' depends on '{dependsOnServiceName}'.");
                }
            }
        }
    }
}