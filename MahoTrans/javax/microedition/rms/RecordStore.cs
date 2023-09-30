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

    void closeRecordStore()
    {
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

    int getVersion() => version;

    [return: JavaType("[Ljava/lang/String;")]
    public static Reference listRecordStores()
    {
        return Toolkit.RecordStore.ListStores().ToHeap(Heap);
    }

    static void openRecordStore()
    {
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

    [return: JavaType(typeof(RecordStore))]
    public static Reference openRecordStore([String] Reference name, bool create)
    {
        return Reference.Null;
        /*
                var item = Toolkit.RecordStore.Open(Heap.ResolveString(name), create);
                if (item == null)
                    Heap.Throw<RecordStoreNotFoundException>();
                var record = Heap.AllocateObject<RecordStore>();
                record._entry = item;
                return record.This;*/
    }
}