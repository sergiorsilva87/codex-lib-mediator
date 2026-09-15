using System.Reflection;
using CodexLibMediator.Handlers;
using CodexLibMediator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CodexLibMediator.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddCodexMediator(this IServiceCollection services, Assembly assembly)
    {
        services.AddScoped<IMediator, Mediator>();

        RegisterHandlers(services, assembly);
        RegisterBehaviors(services, assembly);
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
            var interfaces = handlerType
                .GetInterfaces().Where(IsHandlerInterface);

            foreach (var handlerInterface in interfaces)
            {
                services.AddTransient(handlerInterface, handlerType);

                if (!handlerInterface.IsGenericType)
                    continue;

                var definition = handlerInterface.GetGenericTypeDefinition();
                var arguments = handlerInterface.GetGenericArguments();

                var requestType = arguments[0];
                if (definition == typeof(ICommandHandler<>))
                {
                    var commandInvokerType = typeof(RequestHandlerInvoker<>).MakeGenericType(requestType);
                    services.AddTransient(commandInvokerType);
                    continue;
                }

                if (definition != typeof(IQueryHandler<,>) &&
                    definition != typeof(ICommandHandler<,>) &&
                    definition != typeof(IRequestHandler<,>))
                    continue;

                var resultType = arguments[1];
                var pipelineInvokerType = typeof(PipelineHandlerInvoker<,>).MakeGenericType(requestType, resultType);
                services.AddTransient(pipelineInvokerType);
            }
        }
    }

    private static void RegisterBehaviors(IServiceCollection services, Assembly assembly)
    {
        var behaviorTypes = assembly.GetTypes().Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                type.GetInterfaces().Any(IsPipelineBehaviorInterface))
            .ToArray();

        foreach (var behaviorType in behaviorTypes)
        {
            var behaviorInterfaces = behaviorType
                .GetInterfaces().Where(IsPipelineBehaviorInterface);

            foreach (var behaviorInterface in behaviorInterfaces)
                services.AddTransient(behaviorInterface, behaviorType);
        }
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
                   definition == typeof(IRequestHandler<,>)
               );
    }

    private static bool IsPipelineBehaviorInterface(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() is var definition &&
               (
                   definition == typeof(IPipelineBehavior<>) ||
                   definition == typeof(IPipelineBehavior<,>)
               );
    }
}