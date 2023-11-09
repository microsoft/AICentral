using System.Reflection;

namespace AICentral.Configuration;

public class AssemblyEx
{
    public static Type[] GetTypesOfType<T>(params Assembly[] additionalAssembliesToScan)
    {
        var testEndpointSelectors = additionalAssembliesToScan
            .Union(new[] { typeof(ConfigurationBasedPipelineBuilder).Assembly }).SelectMany(
                x => x.ExportedTypes
                    .Where(x1 => x1 is { IsInterface: false, IsAbstract: false })
                    .Where(x1 => x1.IsAssignableTo(typeof(T))))
            .ToArray();
        return testEndpointSelectors;
    }
}