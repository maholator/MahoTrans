using java.lang;
using java.util;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Array = System.Array;
using Object = java.lang.Object;

namespace javax.microedition.rms;

public class RecordStore : Object
{
    [String] private Reference storeName;
    private int openCount;

    [JavaType(typeof(Vector))] public Reference listeners;

    private int version;
    private long modifiedAt;

    [JavaIgnore] private static Dictionary<string, Reference> openedStores = new();

    [StaticFieldsAnnouncer]
    public static void Statics(List<Reference> list) => list.AddRange(openedStores.Values);


    [JavaDescriptor("([BII)I")]
    public JavaMethodBody addRecord(JavaClass cls)
    {
        var impl = cls.PushConstant(new NameDescriptorClass(nameof(AddRecordInternal), "([BII)I",
            typeof(RecordStore).ToJavaName()));
        var code = new Instruction[]
        {
            // calling actual method
            new Instruction(JavaOpcode.aload_0),
            new Instruction(JavaOpcode.aload_1),
            new Instruction(JavaOpcode.iload_2),
            new Instruction(JavaOpcode.iload_3),
            new Instruction(JavaOpcode.invokespecial, impl.Split()),
            // ID will be here at the stack root
            // it didn't fail? Now events:
            new Instruction(JavaOpcode.iload_1),
            new Instruction(JavaOpcode.istore, new byte[] { 5 }),
        };
        return new JavaMethodBody(5, 9)
        {
            RawCode = code
                .Concat(GenerateListenersCalls(cls, "recordAdded"))
                .Append(new Instruction(JavaOpcode.ireturn))
                .ToArray()
        };
    }

    public int AddRecordInternal([JavaType("[B")] Reference data, int offset, int count)
    {
        CheckNotClosed();
        var name = Jvm.ResolveString(storeName);

        if (count == 0)
        {
            return Toolkit.RecordStore.AddRecord(name, Array.Empty<sbyte>(), 0, 0);
        }

        var arr = Jvm.ResolveArray<sbyte>(data);
        return Toolkit.RecordStore.AddRecord(name, arr, offset, count);
    }

    public void addRecordListener([JavaType(typeof(RecordListener))] Reference listener)
    {
        Vector vector = Jvm.Resolve<Vector>(listeners);

        if (!vector.contains(listener))
        {
            vector.addElement(listener);
        }
    }

    public void closeRecordStore()
    {
        CheckNotClosed();
        openCount--;
        Toolkit.RecordStore.CloseStore(Jvm.ResolveString(storeName));

        if (openCount == 0)
        {
            Jvm.Resolve<Vector>(listeners).removeAllElements();
        }
    }

    void deleteRecord()
    {
    }

    public static void deleteRecordStore([String] Reference str)
    {
        var name = Jvm.ResolveString(str);

        if (openedStores.TryGetValue(name, out var storeRef))
        {
            var store = Jvm.Resolve<RecordStore>(storeRef);
            if (store.openCount > 0)
                Jvm.Throw<RecordStoreException>();
        }

        if (!Toolkit.RecordStore.DeleteStore(name))
            Jvm.Throw<RecordStoreNotFoundException>();
    }

    public void enumerateRecords()
    {
        throw new NotImplementedException();
    }

    public long getLastModified()
    {
        CheckNotClosed();
        return modifiedAt;
    }

    [return: String]
    public Reference getName()
    {
        CheckNotClosed();
        return storeName;
    }

    public int getNextRecordID()
    {
        CheckNotClosed();
        return Toolkit.RecordStore.GetNextId(Jvm.ResolveString(storeName));
    }

    public int getNumRecords()
    {
        CheckNotClosed();
        return Toolkit.RecordStore.GetCount(Jvm.ResolveString(storeName));
    }

    [return: JavaType("[B")]
    public Reference getRecord(int recordId)
    {
        CheckNotClosed();
        var buf = Toolkit.RecordStore.GetRecord(Jvm.ResolveString(storeName), recordId);
        if (buf == null)
            Jvm.Throw<InvalidRecordIDException>();
        return Jvm.AllocateArray(buf, "[B");
    }

    public int getRecord(int recordId, [JavaType("[B")] Reference buf, int offset)
    {
        CheckNotClosed();
        var arr = Jvm.ResolveArray<sbyte>(buf);
        var r = Toolkit.RecordStore.GetRecord(Jvm.ResolveString(storeName), recordId);
        if (r == null)
            Jvm.Throw<InvalidRecordIDException>();
        if (r.Length + offset > arr.Length)
            Jvm.Throw<ArrayIndexOutOfBoundsException>();
        r.CopyTo(arr, offset);
        return r.Length;
    }

    public int getRecordSize(int recordId)
    {
        CheckNotClosed();
        var size = Toolkit.RecordStore.GetSize(Jvm.ResolveString(storeName), recordId);
        if (size.HasValue)
            return size.Value;
        Jvm.Throw<InvalidRecordIDException>();
        return 0;
    }

    public int getSize()
    {
        CheckNotClosed();
        return Toolkit.RecordStore.GetSize(Jvm.ResolveString(storeName));
    }

    public int getSizeAvailable()
    {
        //TODO
        return 798 * 1024 + 590;
    }

    public int getVersion() => version;

    [return: JavaType("[Ljava/lang/String;")]
    public static Reference listRecordStores()
    {
        var stores = Toolkit.RecordStore.ListStores();
        if (stores.Length == 0)
            return Reference.Null;
        return stores.ToHeap(Jvm);
    }

    [return: JavaType(typeof(RecordStore))]
    public static Reference openRecordStore([String] Reference name, bool create)
    {
        return openRecordStore(name, create, 0, false);
    }

    [return: JavaType(typeof(RecordStore))]
    public static Reference openRecordStore([String] Reference name, bool create, int authMode, bool writable)
    {
        var nameStr = Jvm.ResolveString(name);

        if (!Toolkit.RecordStore.OpenStore(nameStr, create))
        {
            Jvm.Throw<RecordStoreNotFoundException>();
            return Reference.Null;
        }

        if (openedStores.TryGetValue(nameStr, out var opened))
        {
            var s = Jvm.Resolve<RecordStore>(opened);
            s.openCount++;
            return opened;
        }

        var store = Jvm.AllocateObject<RecordStore>();
        store.openCount = 1;
        store.storeName = name;
        var vec = Jvm.AllocateObject<Vector>();
        vec.Init();
        store.listeners = vec.This;
        openedStores.Add(nameStr, store.This);
        return store.This;
    }

    [return: JavaType(typeof(RecordStore))]
    public static Reference openRecordStore([String] Reference name, [String] Reference vendorName,
        [String] Reference midletName)
    {
        //TODO
        Jvm.Throw<RecordStoreException>();
        return Reference.Null;
    }

    public void removeRecordListener([JavaType(typeof(RecordListener))] Reference listener)
    {
        if (listeners.IsNull)
            return;
        var vector = Jvm.Resolve<Vector>(listener);
        vector.removeElement(listener);
    }

    [JavaDescriptor(
        "(Ljavax/microedition/rms/RecordFilter;Ljavax/microedition/rms/RecordComparator;Z)Ljavax/microedition/rms/RecordEnumeration;")]
    public Reference enumerateRecords(Reference filter, Reference comp, bool z)
    {
        //TODO
        Jvm.Throw<RecordStoreNotOpenException>();
        return Reference.Null;
    }

    public void setMode(int authmode, bool writeable)
    {
        //TODO
    }

    [JavaDescriptor("(I[BII)V")]
    public JavaMethodBody setRecord(JavaClass cls)
    {
        var impl = cls.PushConstant(new NameDescriptorClass(nameof(SetRecordInternal), "(I[BII)V",
            typeof(RecordStore).ToJavaName()));
        var code = new Instruction[]
        {
            // calling actual method
            new Instruction(JavaOpcode.aload_0),
            new Instruction(JavaOpcode.iload_1),
            new Instruction(JavaOpcode.aload_2),
            new Instruction(JavaOpcode.iload_3),
            new Instruction(JavaOpcode.iload, new byte[] { 4 }),
            new Instruction(JavaOpcode.invokespecial, impl.Split()),
            // it didn't fail? Now events:
            new Instruction(JavaOpcode.iload_1),
            new Instruction(JavaOpcode.istore, new byte[] { 5 }),
        };
        return new JavaMethodBody(5, 9)
        {
            RawCode = code
                .Concat(GenerateListenersCalls(cls, "recordChanged"))
                .Append(new Instruction(JavaOpcode.@return))
                .ToArray()
        };
    }

    public void SetRecordInternal(int recordId, [JavaType("[B")] Reference newData, int offset, int count)
    {
        CheckNotClosed();
        var arr = Jvm.ResolveArray<sbyte>(newData);
        Toolkit.RecordStore.SetRecord(Jvm.ResolveString(storeName), recordId, arr, offset, count);
    }

    #region Utils

    private void CheckNotClosed()
    {
        if (openCount == 0)
            Jvm.Throw<RecordStoreNotOpenException>();
    }

    /// <summary>
    /// Generates code for calling listeners. Embed this directly to record operation.
    /// This expects store object at slot 0 and record index at slot 5.
    /// Slot 6, 7 and 8 will be used - do not touch them.
    /// </summary>
    /// <param name="cls">Class from bytecode generator.</param>
    /// <param name="eventName">Event to invoke.</param>
    /// <returns>Code fragment.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <listheader>Locals:</listheader>
    /// <item>0 - this</item>
    /// <item>1 - free</item>
    /// <item>2 - free</item>
    /// <item>3 - free</item>
    /// <item>4 - free</item>
    /// <item>5 - record ID</item>
    /// <item>6 - used (vector)</item>
    /// <item>7 - used (vector size)</item>
    /// <item>8 - used (index)</item>
    /// </list>
    /// This must enter with <b>empty</b> stack. This fragment does not return. Stack will be left empty.
    /// </remarks>
    private Instruction[] GenerateListenersCalls(JavaClass cls, string eventName)
    {
        var lf = cls.PushConstant(new NameDescriptorClass(nameof(listeners), typeof(Vector), typeof(RecordStore)));
        var vs = cls.PushConstant(new NameDescriptor(nameof(Vector.size), "()I"));
        var vg = cls.PushConstant(new NameDescriptor(nameof(Vector.elementAt), "(I)Ljava/lang/Object;"));
        var ev = cls.PushConstant(new NameDescriptor(eventName, $"({typeof(RecordStore).ToJavaDescriptor()}I)V"));
        return new Instruction[]
        {
            // stack is empty
            new Instruction(JavaOpcode.aload_0),
            new Instruction(JavaOpcode.getfield, lf.Split()),
            new Instruction(JavaOpcode.dup),
            // vector > vector
            new Instruction(JavaOpcode.invokevirtual, vs.Split()),
            new Instruction(JavaOpcode.istore, new byte[] { 7 }), // size = vector.size()
            new Instruction(JavaOpcode.astore, new byte[] { 6 }), // vector = vector
            new Instruction(JavaOpcode.iconst_0),
            new Instruction(JavaOpcode.astore, new byte[] { 8 }), // i = 0
            // stack is empty
            new Instruction(JavaOpcode.@goto, 19.Split()), // while (i<size)
            // loop begin
            new Instruction(JavaOpcode.aload, new byte[] { 6 }),
            new Instruction(JavaOpcode.iload, new byte[] { 8 }),
            // vector > index
            new Instruction(JavaOpcode.invokevirtual, vg.Split()), // vector.elemAt(i)
            new Instruction(JavaOpcode.aload_0),
            new Instruction(JavaOpcode.iload, new byte[] { 5 }),
            // listener > store > id
            new Instruction(JavaOpcode.invokevirtual, ev.Split()), // event call
            new Instruction(JavaOpcode.iinc, new byte[] { 8, 1 }), // i++
            // loop condition
            new Instruction(JavaOpcode.iload, new byte[] { 8 }),
            new Instruction(JavaOpcode.iload, new byte[] { 7 }),
            // i > size
            new Instruction(JavaOpcode.if_icmplt, (-20).Split()),
            // loop end
        };
    }

    #endregion
}