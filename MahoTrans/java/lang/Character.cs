using MahoTrans.Native;

namespace java.lang;

public class Character : Object
{
    public char Value;

    [InitMethod]
    public void Init(char c) => Value = c;

    public char charValue() => Value;
}