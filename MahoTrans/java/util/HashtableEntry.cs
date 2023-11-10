using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace java.util;

public class HashtableEntry : Object
{
    public Reference Key;
    public Reference Value;
    [JavaType(typeof(HashtableEntry))] public Reference Next;

    [InitMethod]
    public void Init(Reference key, Reference value)
    {
        Key = key;
        Value = value;
    }
}