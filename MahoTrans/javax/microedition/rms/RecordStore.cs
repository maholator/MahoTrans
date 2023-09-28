using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Toolkit;
using Object = java.lang.Object;

namespace javax.microedition.rms;

public class RecordStore : Object
{
    [JavaIgnore] private IRecordStoreEntry _entry;

    [return: JavaType(typeof(RecordStore))]
    public static Reference openRecordStore([String] Reference name, bool create)
    {
        var item = Toolkit.RecordStore.Open(Heap.ResolveString(name), create);
        if (item == null)
            Heap.Throw<RecordStoreNotFoundException>();
        var record = Heap.AllocateObject<RecordStore>();
        record._entry = item;
        return record.This;
    }
}