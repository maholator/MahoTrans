using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Toolkit;
using Object = java.lang.Object;

namespace javax.microedition.rms;

public class RecordStore : Object
{
    [JavaIgnore] private string storeName;
    [JavaIgnore] private static Dictionary<string, Reference> openedStores = new();

    void addRecord()
    {
    }

    void addRecordListener()
    {
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

    void getVersion()
    {
    }

    static void listRecordStores()
    {
    }

    static void openRecordStore()
    {
    }

    void removeRecordListener()
    {
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