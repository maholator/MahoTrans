using java.lang;
using java.util;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace javax.microedition.rms;

public class RecordStore : Object
{
    private Reference storeName;
    private int openCount;
    private Reference listeners;
    private int version;
    private long modifiedAt;

    [JavaIgnore] private static Dictionary<string, Reference> openedStores = new();

    void addRecord()
    {
    }

    public void addRecordListener([JavaType(typeof(RecordListener))] Reference listener)
    {
        Vector vector;
        if (listeners.IsNull)
        {
            vector = Heap.AllocateObject<Vector>();
            vector.Init();
            listeners = vector.This;
        }
        else
        {
            vector = Heap.Resolve<Vector>(listeners);
        }

        if (!vector.contains(listener))
        {
            vector.addElement(listener);
        }
    }

    public void closeRecordStore()
    {
        CheckNotClosed();
        openCount--;
        Toolkit.RecordStore.CloseStore(Heap.ResolveString(storeName));
        if (openCount == 0)
        {
            listeners = Reference.Null;
        }
    }

    void deleteRecord()
    {
    }

    void deleteRecordStore()
    {
    }

    void enumerateRecords()
    {
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
        return Toolkit.RecordStore.GetNextId(Heap.ResolveString(storeName));
    }

    public int getNumRecords()
    {
        CheckNotClosed();
        return Toolkit.RecordStore.GetCount(Heap.ResolveString(storeName));
    }

    [return: JavaType("[B")]
    public Reference getRecord(int recordId)
    {
        CheckNotClosed();
        var buf = Toolkit.RecordStore.GetRecord(Heap.ResolveString(storeName), recordId);
        if (buf == null)
            Heap.Throw<InvalidRecordIDException>();
        return Heap.AllocateArray(buf);
    }

    public int getRecord(int recordId, [JavaType("[B")] Reference buf, int offset)
    {
        CheckNotClosed();
        var arr = Heap.ResolveArray<sbyte>(buf);
        var r = Toolkit.RecordStore.GetRecord(Heap.ResolveString(storeName), recordId);
        if (r == null)
            Heap.Throw<InvalidRecordIDException>();
        if (r.Length + offset > arr.Length)
            Heap.Throw<ArrayIndexOutOfBoundsException>();
        r.CopyTo(arr, offset);
        return r.Length;
    }

    public int getRecordSize(int recordId)
    {
        CheckNotClosed();
        var size = Toolkit.RecordStore.GetSize(Heap.ResolveString(storeName), recordId);
        if (size.HasValue)
            return size.Value;
        Heap.Throw<InvalidRecordIDException>();
        return 0;
    }

    public int getSize()
    {
        CheckNotClosed();
        return Toolkit.RecordStore.GetSize(Heap.ResolveString(storeName));
    }

    public int getSizeAvailable()
    {
        throw new NotImplementedException();
    }

    public int getVersion() => version;

    [return: JavaType("[Ljava/lang/String;")]
    public static Reference listRecordStores()
    {
        var stores = Toolkit.RecordStore.ListStores();
        if (stores.Length == 0)
            return Reference.Null;
        return stores.ToHeap(Heap);
    }

    [return: JavaType(typeof(RecordStore))]
    public static Reference openRecordStore([String] Reference name, bool create)
    {
        return openRecordStore(name, create, 0, false);
    }

    [return: JavaType(typeof(RecordStore))]
    public static Reference openRecordStore([String] Reference name, bool create, int authMode, bool writable)
    {
        var nameStr = Heap.ResolveString(name);
        if (openedStores.TryGetValue(nameStr, out var opened))
        {
            var store = Heap.Resolve<RecordStore>(opened);
            store.openCount++;
            return opened;
        }

        if (Toolkit.RecordStore.OpenStore(nameStr, create))
        {
            var store = Heap.AllocateObject<RecordStore>();
            store.openCount = 1;
            store.storeName = name;
            openedStores.Add(nameStr, store.This);
            return store.This;
        }

        Heap.Throw<RecordStoreNotFoundException>();
        return Reference.Null;
    }

    [return: JavaType(typeof(RecordStore))]
    public static Reference openRecordStore([String] Reference name, [String] Reference vendorName,
        [String] Reference midletName)
    {
        //TODO
        Heap.Throw<RecordStoreException>();
        return Reference.Null;
    }

    public void removeRecordListener([JavaType(typeof(RecordListener))] Reference listener)
    {
        if (listeners.IsNull)
            return;
        var vector = Heap.Resolve<Vector>(listener);
        vector.removeElement(listener);
    }

    void setMode()
    {
    }

    void setRecord()
    {
    }

    #region Utils

    private void CheckNotClosed()
    {
        if (openCount == 0)
            Heap.Throw<RecordStoreNotOpenException>();
    }

    #endregion
}