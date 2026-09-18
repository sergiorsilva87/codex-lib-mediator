# CodexLibMediator

Uma biblioteca de Mediator para .NET 10, com suporte a Requests, Commands, Queries, Notifications, Streams e Pipeline Behaviors.

## Características

- Requests com e sem retorno
- Commands com e sem retorno
- Queries
- Notifications com múltiplos handlers
- Streaming com `IAsyncEnumerable<T>`
- Pipeline Behaviors
- Stream Pipeline Behaviors
- Registro automático via Assembly Scanning
- Registro explícito de Pipeline Behaviors
- Propagação de `CancellationToken`
- Suporte à injeção de dependência do .NET
- Execução determinística dos handlers e behaviors registrados automaticamente

## Requisitos

- .NET 10

## Instalação

O projeto pode ser referenciado diretamente pela aplicação:

```xml
<ProjectReference Include="../CodexLibMediator/CodexLibMediator.csproj" />
```

Depois, importe os namespaces necessários:

```csharp
using CodexLibMediator.Extensions;
using CodexLibMediator.Interfaces;
```

## Configuração

Registre o Mediator informando o Assembly onde estão os handlers:

```csharp
services.AddCodexMediator(typeof(MyRequestHandler).Assembly);
```

O registro automático encontra:

- `IRequestHandler<TRequest>`
- `IRequestHandler<TRequest, TResult>`
- `ICommandHandler<TRequest>`
- `ICommandHandler<TRequest, TResult>`
- `IQueryHandler<TRequest, TResult>`
- `INotificationHandler<TNotification>`
- `IStreamRequestHandler<TRequest, TResult>`

O Mediator é registrado como `Scoped` e os handlers/behaviors como `Transient`.

## Requests

### Request sem retorno

Defina o request:

```csharp
public sealed record CreateUserCommand(string Name) : ICommand;
```

Implemente o handler:

```csharp
public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public Task HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Usuário: {command.Name}");

        return Task.CompletedTask;
    }
}
```

Envie através do Mediator:

```csharp
await mediator.SendAsync(
    new CreateUserCommand("Sérgio"),
    cancellationToken);
```

### Request com retorno

```csharp
public sealed record GetUserQuery(Guid Id) : IQuery<string>;
```

Handler:

```csharp
public sealed class GetUserQueryHandler : IQueryHandler<GetUserQuery, string>
{
    public Task<string> HandleAsync(
        GetUserQuery query,
        CancellationToken cancellationToken)
    {
        return Task.FromResult($"User: {query.Id}");
    }
}
```

Uso:

```csharp
var result = await mediator.SendAsync(
    new GetUserQuery(userId),
    cancellationToken);
```

## Notifications

Notifications podem ter vários handlers.

Defina a notification:

```csharp
public sealed record UserCreatedNotification(Guid UserId) : INotification;
```

Handlers:

```csharp
public sealed class SendEmailHandler
    : INotificationHandler<UserCreatedNotification>
{
    public Task HandleAsync(
        UserCreatedNotification notification,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Enviando e-mail...");

        return Task.CompletedTask;
    }
}

public sealed class AuditHandler
    : INotificationHandler<UserCreatedNotification>
{
    public Task HandleAsync(
        UserCreatedNotification notification,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Registrando auditoria...");

        return Task.CompletedTask;
    }
}
```

Publique:

```csharp
await mediator.PublishAsync(
    new UserCreatedNotification(userId),
    cancellationToken);
```

Todos os handlers registrados para a notification são executados sequencialmente.

Quando o registro é feito por Assembly Scanning, os handlers são ordenados pelo nome completo do tipo para tornar a ordem determinística.

Se um handler lançar uma exceção, a exceção é propagada e os handlers seguintes não são executados.

## Pipeline Behaviors

Um Pipeline Behavior permite executar lógica antes e/ou depois do handler.

Exemplo de logging:

```csharp
public sealed class LoggingBehavior<TRequest, TResult>
    : IPipelineBehavior<TRequest, TResult>
{
    public async Task<TResult> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResult> next)
    {
        Console.WriteLine("Before");

        var result = await next();

        Console.WriteLine("After");

        return result;
    }
}
```

Para requests sem retorno:

```csharp
public sealed class LoggingBehavior<TRequest>
    : IPipelineBehavior<TRequest>
{
    public async Task HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate next)
    {
        Console.WriteLine("Before");

        await next();

        Console.WriteLine("After");
    }
}
```

### Registro automático

Por padrão, behaviors encontrados no Assembly Scanning são registrados automaticamente.

A ordem automática é determinística, baseada no nome completo do tipo.

### Registro explícito

Também é possível informar exatamente quais behaviors devem ser utilizados:

```csharp
services.AddCodexMediator(
    typeof(MyRequestHandler).Assembly,
    options =>
    {
        options.AddBehavior<ValidationBehavior>();
        options.AddBehavior<LoggingBehavior>();
    });
```

Nesse caso, os behaviors são executados na ordem em que foram adicionados:

```text
ValidationBehavior
    ↓
LoggingBehavior
    ↓
Handler
    ↑
LoggingBehavior
    ↑
ValidationBehavior
```

Quando existe pelo menos um behavior explicitamente configurado, o Assembly Scanning automático de behaviors não é utilizado.

## Stream Requests

Streams utilizam `IAsyncEnumerable<T>`.

Request:

```csharp
public sealed class GetNumbersRequest : IStreamRequest<int>
{
}
```

Handler:

```csharp
public sealed class GetNumbersHandler
    : IStreamRequestHandler<GetNumbersRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(
        GetNumbersRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 1; i <= 5; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return i;

            await Task.Yield();
        }
    }
}
```

Consumo:

```csharp
await foreach (var number in mediator.StreamAsync(
    new GetNumbersRequest(),
    cancellationToken))
{
    Console.WriteLine(number);
}
```

## Stream Pipeline Behaviors

Streams também suportam behaviors:

```csharp
public sealed class MultiplyBehavior
    : IStreamPipelineBehavior<GetNumbersRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(
        GetNumbersRequest request,
        CancellationToken cancellationToken,
        StreamHandlerDelegate<int> next)
    {
        await foreach (var item in next(cancellationToken))
        {
            yield return item * 10;
        }
    }
}
```

O behavior pode transformar os valores produzidos pelo handler, além de executar lógica antes ou depois do processamento do stream.

## CancellationToken

O `CancellationToken` recebido pelo Mediator é propagado para:

- Pipeline Behaviors
- Handlers
- Notification Handlers
- Stream Handlers
- Stream Pipeline Behaviors

Exemplo:

```csharp
await mediator.SendAsync(
    request,
    cancellationToken);
```

## Tratamento de handlers não registrados

Se um request ou stream request não possuir o handler/invoker necessário registrado no container de DI, o Mediator lança `InvalidOperationException`.

## Estrutura básica

Uma aplicação pode organizar os componentes desta forma:

```text
MyApplication/
├── Commands/
│   ├── CreateUserCommand.cs
│   └── CreateUserCommandHandler.cs
├── Queries/
│   ├── GetUserQuery.cs
│   └── GetUserQueryHandler.cs
├── Notifications/
│   ├── UserCreatedNotification.cs
│   └── UserCreatedHandler.cs
└── Behaviors/
    └── LoggingBehavior.cs
```

Basta registrar o Assembly correspondente:

```csharp
services.AddCodexMediator(typeof(CreateUserCommandHandler).Assembly);
```

## Testes

O projeto possui uma suíte de testes cobrindo:

- Requests, Commands e Queries
- Pipeline Behaviors
- Ordem de execução
- Notifications e múltiplos handlers
- Streams
- CancellationToken
- Propagação de exceções
- Registro via Dependency Injection
- Assembly Scanning
- Configuração explícita de behaviors
- Casos de registro duplicado e tipos inválidos
- Tratamento de argumentos nulos

Execute:

```bash
dotnet test
```

## Licença

A licença do projeto ainda não está definida.
