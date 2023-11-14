using MahoTrans.Native;

namespace java.lang;

public class Character : Object
{
    public char Value;

    [InitMethod]
    public void Init(char c) => Value = c;

    public char charValue() => Value;

    public static int digit(char ch, int radix)
    {
        var val = ch - '0';
        if (val < 0) return -1;
        if (radix <= 10)
        {
            if (val < radix)
                return val;
            return -1;
        }

        if (val < 10)
            return val;
        val = char.ToUpper(ch) - 'A';
        if (val < 0)
            return -1;
        val += 10;
        if (val < radix)
            return val;
        return -1;
    }
}