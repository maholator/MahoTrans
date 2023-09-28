namespace MahoTrans;

[AttributeUsage(AttributeTargets.Field)]
public class OpcodeArgsCountAttribute : Attribute
{
    public int ArgsCount;

    public OpcodeArgsCountAttribute(int argsCount)
    {
        ArgsCount = argsCount;
    }
}