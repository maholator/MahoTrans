using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.lang;

public class Byte : Object
{
    public sbyte Value;

    [InitMethod]
    public void Init(sbyte b) => Value = b;

    public sbyte byteValue() => Value;

    public static sbyte parseByte([String] Reference str)
    {
        if (!int.TryParse(Jvm.ResolveString(str), out var i))
            Jvm.Throw<NumberFormatException>();

        if (i < -128 || i > 127)
            Jvm.Throw<NumberFormatException>();

        return (sbyte)i;
    }

    public static sbyte parseByte([String] Reference str, int radix)
    {
        try
        {
            var i = Convert.ToInt32(Jvm.ResolveString(str), radix);
            if (i < -128 || i > 127)
                Jvm.Throw<NumberFormatException>();
            return (sbyte)i;
        }
        catch
        {
            Jvm.Throw<NumberFormatException>();
        }

        return 0;
    }

    [return: String]
    public Reference toString()
    {
        return Jvm.AllocateString(Value.ToString());
    }

    public int hashCode() => Value;

    public bool equals(Reference obj)
    {
        if (obj.IsNull)
            return false;
        var o = Jvm.ResolveObject(obj);
        if (o is not Byte ii)
            return false;
        return ii.Value == Value;
    }
}