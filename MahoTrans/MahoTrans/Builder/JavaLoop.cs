namespace MahoTrans.Builder;

public readonly struct JavaLoop
{
    public readonly JavaLabel LoopBegin;
    public readonly JavaLabel ConditionBegin;
    public readonly JavaOpcode Condition;

    public JavaLoop(JavaLabel loopBegin, JavaLabel conditionBegin, JavaOpcode condition)
    {
        LoopBegin = loopBegin;
        ConditionBegin = conditionBegin;
        Condition = condition;
    }
}