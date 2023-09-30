using java.io;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;

namespace java.lang;

public class Class : Object
{
    /// <summary>
    /// JVM-side version of this object.
    /// </summary>
    [JavaIgnore] public JavaClass InternalClass = null!;

    [return: JavaType(typeof(Class))]
    public static Reference forName([String] Reference r)
    {
        var name = Heap.ResolveString(r);
        if (!Heap.State.Classes.TryGetValue(name.Replace('.', '/'), out var jc))
            Heap.Throw<ClassNotFoundException>();
        var cls = Heap.AllocateObject<Class>();
        cls.InternalClass = jc!;
        return cls.This;
    }

    [return: String]
    public Reference getName()
    {
        var name = InternalClass.Name.Replace('/', '.');
        return Heap.AllocateString(name);
    }

    [return: JavaType(typeof(InputStream))]
    public Reference getResourceAsStream([String] Reference name)
    {
        var data = Heap.State.GetResource(Heap.ResolveString(name));
        if (data == null)
            return Reference.Null;

        var stream = Heap.AllocateObject<ByteArrayInputStream>();
        var buf = Heap.AllocateArray(data);
        stream.Init(buf);
        return stream.This;
    }
}