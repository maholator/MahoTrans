using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Short : Object
{
    [JavaIgnore] public short Value;

    [InitMethod]
    public void Init(short v)
    {
        Value = v;
    }

    public short shortValue() => Value;

    public static short parseShort([String] Reference str)
    {
        if (!int.TryParse(Jvm.ResolveString(str), out var i))
            Jvm.Throw<NumberFormatException>();

        if (i < short.MinValue || i > short.MaxValue)
            Jvm.Throw<NumberFormatException>();

        return (short)i;
    }

    public bool equals(Reference r)
    {
        if (r.IsNull)
            return false;

        var obj = Jvm.ResolveObject(r);

        if (obj is Short s)
        {
            return s.Value == Value;
        }

        return false;
    }
}