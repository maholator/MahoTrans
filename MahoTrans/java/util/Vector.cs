using java.lang;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace java.util;

public class Vector : Object
{
    [JavaIgnore] public List<Reference> List = null!;

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        if (List == null!)
            return;
        foreach (var r in List) queue.Enqueue(r);
    }

    [InitMethod]
    public new void Init()
    {
        List = new List<Reference>();
    }

    [InitMethod]
    public void Init(int capacity)
    {
        List = new List<Reference>(capacity);
    }

    [InitMethod]
    public void Init(int capacity, int incr)
    {
        List = new List<Reference>(capacity);
    }

    public void addElement(Reference r) => List.Add(r);

    public int capacity() => List.Capacity;

    public bool contains(Reference r) => List.Contains(r);

    public void copyInto([JavaType("[Ljava/lang/Object;")] Reference arr)
    {
        var a = Jvm.ResolveArray<Reference>(arr);
        List.CopyTo(a);
    }

    public Reference elementAt(int i) => List[i];

    [return: JavaType(typeof(Enumeration))]
    public Reference elements()
    {
        var e = Jvm.AllocateObject<ArrayEnumerator>();
        e.Value = List.ToArray();
        return e.This;
    }

    public void ensureCapacity(int min)
    {
    }

    public Reference firstElement()
    {
        if (List.Count == 0)
            Jvm.Throw<NoSuchElementException>();
        return List[0];
    }

    public Reference lastElement()
    {
        if (List.Count == 0)
            Jvm.Throw<NoSuchElementException>();
        return List[^1];
    }

    public int size() => List.Count;

    public bool isEmpty() => List.Count == 0;

    //TODO equals()?
    public bool removeElement(Reference obj) => List.Remove(obj);

    public void removeAllElements() => List.Clear();

    public void removeElementAt(int index) => List.RemoveAt(index);

    public void setElementAt(Reference obj, int index)
    {
        if (index < 0 || index >= size())
            Jvm.Throw<ArrayIndexOutOfBoundsException>();
        List[index] = obj;
    }

    public void insertElementAt(Reference obj, int index)
    {
        if (index < 0 || index > size())
            Jvm.Throw<ArrayIndexOutOfBoundsException>();
        List.Insert(index, obj);
    }

    [JavaDescriptor("(Ljava/lang/Object;)I")]
    public JavaMethodBody indexOf(JavaClass cls)
    {
        var els = cls.PushConstant(new NameDescriptorClass(nameof(elements), "()Ljava/util/Enumeration;",
            typeof(Vector))).Split();
        var hasMore = cls.PushConstant(new NameDescriptorClass(nameof(ArrayEnumerator.hasMoreElements), "()Z",
            typeof(Enumeration))).Split();
        var nextEl = cls.PushConstant(new NameDescriptor(nameof(ArrayEnumerator.nextElement), "()Ljava/lang/Object;"))
            .Split();
        return new JavaMethodBody(2, 4)
        {
            // locals: this > target > enum > index
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.invokevirtual, els),
                new(JavaOpcode.astore_2),
                new(JavaOpcode.iconst_0),
                new(JavaOpcode.istore_3),
                new(JavaOpcode.@goto, new byte[] { 0, 20 }),

                // loop
                new(JavaOpcode.aload_2),
                new(JavaOpcode.invokevirtual, nextEl),
                new Instruction(JavaOpcode.aload_1),
                new Instruction(JavaOpcode.swap),
                // target > el
                new(JavaOpcode.invokevirtual,
                    cls.PushConstant(new NameDescriptor(nameof(equals), "(Ljava/lang/Object;)Z")).Split()),
                // areEquals
                new(JavaOpcode.ifne, new byte[] { 0, 5 }),
                new(JavaOpcode.iload_3),
                new(JavaOpcode.ireturn),

                new(JavaOpcode.iinc, new byte[] { 3, 1 }),

                // condition
                new(JavaOpcode.aload_2),
                new(JavaOpcode.invokevirtual,
                    hasMore),
                new(JavaOpcode.ifne, (-21).Split()),

                // return -1 if nothing found
                new(JavaOpcode.iconst_m1),
                new(JavaOpcode.ireturn),
            }
        };
    }

    public void trimToSize()
    {
    }
}