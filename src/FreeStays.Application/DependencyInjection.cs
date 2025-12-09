using FluentValidation;
using FreeStays.Application.Common.Behaviors;
using FreeStays.Application.Common.Mappings;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;

namespace FreeStays.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        services.AddAutoMapper(typeof(MappingProfile));
        
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        
        services.AddValidatorsFromAssembly(assembly);
        
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        
        return services;
    }
}
