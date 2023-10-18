using MahoTrans.Native;
using MahoTrans.Runtime;
using ClrArray = System.Array;

namespace java.lang;

[JavaIgnore]
public class Array<T> : Array where T : struct
{
    [JavaIgnore] public T[] Value = null!;

    public override ClrArray BaseValue => Value;

    public override IEnumerable<Reference> EnumerableReferences()
    {
        if (typeof(T) == typeof(Reference))
        {
            return (Reference[])(object)Value;
        }

        return base.EnumerableReferences();
    }
}

public abstract class Array : Object
{
    public abstract ClrArray BaseValue { get; }
}