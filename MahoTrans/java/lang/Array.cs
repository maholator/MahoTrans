using MahoTrans.Native;
using ClrArray = System.Array;

namespace java.lang;

[JavaIgnore]
public class Array<T> : Array where T : struct
{
    [JavaIgnore] public T[] Value;

    public override ClrArray BaseValue => Value;
}

public abstract class Array : Object
{
    public abstract ClrArray BaseValue { get; }
}