namespace CodexLibMediator.Extensions;

public sealed class CodexMediatorOptions
{
    internal List<Type> BehaviorTypes { get; } = [];

    public CodexMediatorOptions AddBehavior<TBehavior>() where TBehavior : class
    {
        BehaviorTypes.Add(typeof(TBehavior));
        return this;
    }
}