using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WorkManager.Configuration;

public static class RegisterOptionsFromConfigurationExtension
{
    private static readonly Dictionary<string, Type> _optionsMapping = new();

    static RegisterOptionsFromConfigurationExtension()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        foreach (Type type in GetExternalServiceConfigurations(assembly))
        {
            OptionsConfigurationAttribute? attribute =
                (OptionsConfigurationAttribute)type.GetCustomAttribute(typeof(OptionsConfigurationAttribute));
            _optionsMapping.Add(attribute.ServiceName, type);
        }
    }

    private static IEnumerable<Type> GetExternalServiceConfigurations(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
        {
            if (type.GetCustomAttributes(typeof(OptionsConfigurationAttribute), true).Length > 0)
            {
                yield return type;
            }
        }
    }

    public static IServiceCollection RegisterOptionsFromConfiguration(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        IConfigurationSection optionsSection = configuration.GetSection("options");

        foreach (IConfigurationSection optionSection in optionsSection.GetChildren())
        {
            AddOptionsWithValidateOnStart(serviceCollection, optionSection, optionSection.Key);
        }

        return serviceCollection;
    }

    public static void AddOptionsWithValidateOnStart(IServiceCollection serviceCollection,
        IConfigurationSection configuration, string optionName)
    {
        if (string.IsNullOrEmpty(optionName))
        {
            throw new ArgumentNullException(nameof(optionName));
        }

        if (!_optionsMapping.TryGetValue(optionName, out Type? optionType))
        {
            throw new InvalidOperationException($"The service '{optionName}' is not registered.");
        }

        try
        {
            object? optionsBuilder = typeof(OptionsServiceCollectionExtensions)
                .GetMethod("AddOptionsWithValidateOnStart", 1, [typeof(IServiceCollection), typeof(string)])
                ?.MakeGenericMethod(optionType)
                .Invoke(serviceCollection, [serviceCollection, null]);

            optionsBuilder = typeof(OptionsBuilderConfigurationExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(x => x.Name == "Bind" && x.GetParameters().Length == 2)
                .MakeGenericMethod(optionType)
                .Invoke(optionsBuilder, [optionsBuilder, configuration]);

            _ = typeof(OptionsBuilderDataAnnotationsExtensions)
                .GetMethod("ValidateDataAnnotations")
                ?.MakeGenericMethod(optionType)
                .Invoke(optionsBuilder, [optionsBuilder]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"The service '{optionName}' is not registered.", ex);
        }
    }
}