using System.Reflection;
using CodexLibMediator.Handlers;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddCodexMediator(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);
        
        services.AddScoped<IMediator, Mediator>();

        RegisterHandlers(services, assembly);
        RegisterBehaviors(services, assembly, null);
        return services;
    }
    
    public static IServiceCollection AddCodexMediator(this IServiceCollection services,
        Assembly assembly, Action<CodexMediatorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(configure);
        
        services.AddScoped<IMediator, Mediator>();
        RegisterHandlers(services, assembly);

        var options = new CodexMediatorOptions();
        configure(options);

        RegisterBehaviors(services, assembly, options);
        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes().Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                type.GetInterfaces().Any(IsHandlerInterface))
            .ToArray();

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces().Where(IsHandlerInterface);
            foreach (var handlerInterface in interfaces)
            {
                services.AddTransient(handlerInterface, handlerType);

                if (!handlerInterface.IsGenericType)
                    continue;

                var definition = handlerInterface.GetGenericTypeDefinition();
                var arguments = handlerInterface.GetGenericArguments();

                var requestType = arguments[0];
                if (definition == typeof(IStreamRequestHandler<,>))
                {
                    var resultType1 = arguments[1];

                    var invokerType = typeof(StreamRequestHandlerInvoker<,>)
                        .MakeGenericType(requestType, resultType1);

                    services.AddTransient(invokerType);
                    continue;
                }

                if (definition == typeof(INotificationHandler<>))
                {
                    var invokerType = typeof(NotificationHandlerInvoker<>).MakeGenericType(requestType);
                    services.AddTransient(invokerType);
                    continue;
                }

                if (definition == typeof(ICommandHandler<>) || definition == typeof(IRequestHandler<>))
                {
                    var invokerType = typeof(PipelineHandlerInvoker<>).MakeGenericType(requestType);
                    services.AddTransient(invokerType);
                    continue;
                }

                if (definition != typeof(IQueryHandler<,>) &&
                    definition != typeof(ICommandHandler<,>) &&
                    definition != typeof(IRequestHandler<,>))
                    continue;

                var resultType2 = arguments[1];
                var pipelineInvokerType = typeof(PipelineHandlerInvoker<,>).MakeGenericType(requestType, resultType2);
                services.AddTransient(pipelineInvokerType);
            }
        }
    }

    private static void RegisterBehaviors(IServiceCollection services, Assembly assembly, CodexMediatorOptions? options)
    {
        if (options is { BehaviorTypes.Count: > 0 })
        {
            foreach (var behaviorType in options.BehaviorTypes)
                RegisterBehavior(services, behaviorType);

            return;
        }

        var behaviorTypes = assembly.GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                type.GetInterfaces().Any(IsPipelineBehaviorInterface))
            .OrderBy(type => type.FullName)
            .ToArray();

        foreach (var behaviorType in behaviorTypes)
            RegisterBehavior(services, behaviorType);
    }
    
    private static void RegisterBehavior(IServiceCollection services, Type behaviorType)
    {
        var behaviorInterfaces = behaviorType
            .GetInterfaces().Where(IsPipelineBehaviorInterface);

        foreach (var behaviorInterface in behaviorInterfaces)
            services.AddTransient(behaviorInterface, behaviorType);
    }

    private static bool IsHandlerInterface(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() is var definition &&
               (
                   definition == typeof(IQueryHandler<,>) ||
                   definition == typeof(ICommandHandler<>) ||
                   definition == typeof(ICommandHandler<,>) ||
                   definition == typeof(IRequestHandler<>) ||
                   definition == typeof(IRequestHandler<,>) ||
                   definition == typeof(INotificationHandler<>) ||
                   definition == typeof(IStreamRequestHandler<,>)
               );
    }

    private static bool IsPipelineBehaviorInterface(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() is var definition &&
               (
                   definition == typeof(IPipelineBehavior<>) ||
                   definition == typeof(IPipelineBehavior<,>) || 
                   definition == typeof(IStreamPipelineBehavior<,>)
               );
    }
}