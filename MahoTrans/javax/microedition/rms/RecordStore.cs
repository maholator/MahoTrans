// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using java.util;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Newtonsoft.Json;
using Object = java.lang.Object;

namespace javax.microedition.rms;

public class RecordStore : Object
{
    [String] public Reference StoreName;
    [JsonProperty] private int _openCount;

    [JavaType(typeof(Vector))] public Reference Listeners;

    [JsonProperty] private int _version;
    [JsonProperty] private long _modifiedAt;

    [ClassInit]
    public static void ClInit()
    {
        NativeStatics.OpenedRecordStores = new Dictionary<string, Reference>();
    }

    [JavaDescriptor("([BII)I")]
    public JavaMethodBody addRecord(JavaClass cls)
    {
        var impl = cls.PushConstant(new NameDescriptorClass(nameof(AddRecordInternal), "([BII)I",
            typeof(RecordStore).ToJavaName()));
        var code = new[]
        {
            // calling actual method
            new Instruction(JavaOpcode.aload_0),
            new Instruction(JavaOpcode.aload_1),
            new Instruction(JavaOpcode.iload_2),
            new Instruction(JavaOpcode.iload_3),
            new Instruction(JavaOpcode.invokespecial, impl.Split()),
            // ID will be here at the stack root
            // save it to local 5 for events and leaving on stack for ireturn in the end
            new Instruction(JavaOpcode.dup),
            new Instruction(JavaOpcode.istore, new byte[] { 5 }),
            // all the above didn't fail? Now events:
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
        var name = Jvm.ResolveString(StoreName);

        if (count == 0)
        {
            return Toolkit.RecordStore.AddRecord(name, ReadOnlySpan<byte>.Empty);
        }

        var arr = Jvm.ResolveArray<sbyte>(data);
        return Toolkit.RecordStore.AddRecord(name, new ReadOnlySpan<byte>(arr.ToUnsigned(), offset, count));
    }

    public void addRecordListener([JavaType(typeof(RecordListener))] Reference listener)
    {
        Vector vector = Jvm.Resolve<Vector>(Listeners);

        if (!vector.contains(listener))
        {
            vector.addElement(listener);
        }
    }

    public void closeRecordStore()
    {
        CheckNotClosed();
        _openCount--;
        Toolkit.RecordStore.CloseStore(Jvm.ResolveString(StoreName));

        if (_openCount == 0)
        {
            Jvm.Resolve<Vector>(Listeners).removeAllElements();
        }
    }

    public void deleteRecord(int id)
    {
        CheckNotClosed();
        Toolkit.RecordStore.DeleteRecord(Jvm.ResolveString(StoreName), id);
    }

    public static void deleteRecordStore([String] Reference str)
    {
        var name = Jvm.ResolveString(str);

        if (NativeStatics.OpenedRecordStores.TryGetValue(name, out var storeRef))
        {
            var store = Jvm.Resolve<RecordStore>(storeRef);
            if (store._openCount > 0)
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
        return _modifiedAt;
    }

    [return: String]
    public Reference getName()
    {
        CheckNotClosed();
        return StoreName;
    }

    public int getNextRecordID()
    {
        CheckNotClosed();
        return Toolkit.RecordStore.GetNextId(Jvm.ResolveString(StoreName));
    }

    public int getNumRecords()
    {
        CheckNotClosed();
        return Toolkit.RecordStore.GetCount(Jvm.ResolveString(StoreName));
    }

    [return: JavaType("[B")]
    public Reference getRecord(int recordId)
    {
        CheckNotClosed();
        var buf = Toolkit.RecordStore.GetRecord(Jvm.ResolveString(StoreName), recordId);
        if (buf == null)
            Jvm.Throw<InvalidRecordIDException>();
        return Jvm.WrapPrimitiveArray(buf.ConvertToSigned());
    }

    public int getRecord(int recordId, [JavaType("[B")] Reference buf, int offset)
    {
        CheckNotClosed();
        var arr = Jvm.ResolveArray<sbyte>(buf);
        var r = Toolkit.RecordStore.GetRecord(Jvm.ResolveString(StoreName), recordId);
        if (r == null)
            Jvm.Throw<InvalidRecordIDException>();
        if (r.Length + offset > arr.Length)
            Jvm.Throw<ArrayIndexOutOfBoundsException>();
        r.ConvertToSigned().CopyTo(arr, offset);
        return r.Length;
    }

    public int getRecordSize(int recordId)
    {
        CheckNotClosed();
        var size = Toolkit.RecordStore.GetSize(Jvm.ResolveString(StoreName), recordId);
        if (size.HasValue)
            return size.Value;
        Jvm.Throw<InvalidRecordIDException>();
        return 0;
    }

    public int getSize()
    {
        CheckNotClosed();
        return Toolkit.RecordStore.GetSize(Jvm.ResolveString(StoreName));
    }

    public int getSizeAvailable()
    {
        return Toolkit.RecordStore.AvailableMemory;
    }

    public int getVersion() => _version;

    [return: JavaType("[Ljava/lang/String;")]
    public static Reference listRecordStores()
    {
        var stores = Toolkit.RecordStore.ListStores();
        if (stores.Length == 0)
            return Reference.Null;
        return stores.AsJavaArray();
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

        if (nameStr.Length == 0 || nameStr.Length > 32)
            Jvm.Throw<IllegalArgumentException>();

        if (!Toolkit.RecordStore.OpenStore(nameStr, create))
        {
            Jvm.Throw<RecordStoreNotFoundException>();
            return Reference.Null;
        }

        if (NativeStatics.OpenedRecordStores.TryGetValue(nameStr, out var opened))
        {
            var s = Jvm.Resolve<RecordStore>(opened);
            s._openCount++;
            return opened;
        }

        var store = Jvm.Allocate<RecordStore>();
        store._openCount = 1;
        store.StoreName = name;
        var vec = Jvm.Allocate<Vector>();
        vec.Init();
        store.Listeners = vec.This;
        NativeStatics.OpenedRecordStores.Add(nameStr, store.This);
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
        if (Listeners.IsNull)
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
        var code = new[]
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
        var res = Toolkit.RecordStore.SetRecord(Jvm.ResolveString(StoreName), recordId,
            new ReadOnlySpan<byte>(arr.ToUnsigned(), offset, count));
        if (!res)
            Jvm.Throw<InvalidRecordIDException>();
    }

    #region Utils

    private void CheckNotClosed()
    {
        if (_openCount == 0)
            Jvm.Throw<RecordStoreNotOpenException>();
    }

    /// <summary>
    ///     Generates code for calling listeners. Embed this directly to record operation.
    ///     This expects store object at slot 0 and record index at slot 5.
    ///     Slot 6, 7 and 8 will be used - do not touch them.
    /// </summary>
    /// <param name="cls">Class from bytecode generator.</param>
    /// <param name="eventName">Event to invoke.</param>
    /// <returns>Code fragment.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <listheader>Locals:</listheader>
    ///         <item>0 - this</item>
    ///         <item>1 - free</item>
    ///         <item>2 - free</item>
    ///         <item>3 - free</item>
    ///         <item>4 - free</item>
    ///         <item>5 - record ID</item>
    ///         <item>6 - used (vector)</item>
    ///         <item>7 - used (vector size)</item>
    ///         <item>8 - used (index)</item>
    ///     </list>
    ///     This must enter with <b>empty</b> stack. This fragment does not return. Stack will be left empty.
    /// </remarks>
    private Instruction[] GenerateListenersCalls(JavaClass cls, string eventName)
    {
        var lf = cls.PushConstant(new NameDescriptorClass(nameof(Listeners), typeof(Vector), typeof(RecordStore)));
        var vs = cls.PushConstant(new NameDescriptor(nameof(Vector.size), "()I"));
        var vg = cls.PushConstant(new NameDescriptor(nameof(Vector.elementAt), "(I)Ljava/lang/Object;"));
        var ev = cls.PushConstant(new NameDescriptor(eventName, $"({typeof(RecordStore).ToJavaDescriptor()}I)V"));
        return new[]
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
            new Instruction(JavaOpcode.istore, new byte[] { 8 }), // i = 0
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