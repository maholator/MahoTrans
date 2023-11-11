namespace MahoTrans.Builder;

public readonly struct JavaLoop : IDisposable
{
    public readonly int Number;
    public readonly JavaLabel LoopBegin;
    public readonly JavaLabel ConditionBegin;
    public readonly JavaOpcode Condition;
    private readonly JavaMethodBuilder _builder;

    public JavaLoop(JavaMethodBuilder builder, int number, JavaLabel loopBegin, JavaLabel conditionBegin,
        JavaOpcode condition)
    {
        LoopBegin = loopBegin;
        ConditionBegin = conditionBegin;
        Condition = condition;
        _builder = builder;
        Number = number;
    }

    /// <summary>
    /// Calls <see cref="JavaMethodBuilder.BeginLoopCondition"/>. This looks better with using-styled usage.
    /// </summary>
    public void ConditionSection() => _builder.BeginLoopCondition(this);

    public void Dispose()
    {
        _builder.EndLoop(this);
    }
}