using java.util;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Toolkit;
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

    void getLastModified()
    {
    }

    void getName()
    {
    }

    void getNextRecordID()
    {
    }

    void getNumRecords()
    {
    }

    void getRecord(int id)
    {
    }

    void getRecord()
    {
    }

    void getRecordSize()
    {
    }

    void getSize()
    {
    }

    void getSizeAvailable()
    {
    }

    public int getVersion() => version;

    [return: JavaType("[Ljava/lang/String;")]
    public static Reference listRecordStores()
    {
        return Toolkit.RecordStore.ListStores().ToHeap(Heap);
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