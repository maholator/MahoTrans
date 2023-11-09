namespace MahoTrans.Builder;

public readonly struct JavaLoop : IDisposable
{
    public readonly int Number;
    public readonly JavaLabel LoopBegin;
    public readonly JavaLabel ConditionBegin;
    public readonly JavaOpcode Condition;
    public readonly JavaMethodBuilder Builder;

    public JavaLoop(JavaMethodBuilder builder, int number, JavaLabel loopBegin, JavaLabel conditionBegin, JavaOpcode condition)
    {
        LoopBegin = loopBegin;
        ConditionBegin = conditionBegin;
        Condition = condition;
        Builder = builder;
        Number = number;
    }

    public void ConditionSection() => Builder.BeginLoopCondition(this);

    public void Dispose()
    {
        Builder.EndLoop(this);
    }
}