using System.Diagnostics.CodeAnalysis;

namespace FiapSrvPayment.Infrastructure.Mappings;

[ExcludeFromCodeCoverage]
public static class MongoMappings
{
    public static void ConfigureMappings() 
    {
        UserMapping.Configure();
        GameMapping.Configure();
    }
}