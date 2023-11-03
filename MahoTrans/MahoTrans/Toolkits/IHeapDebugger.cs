using Object = java.lang.Object;

namespace MahoTrans.Toolkits;

public interface IHeapDebugger : IToolkit
{
    void ObjectCreated(Object obj);

    void ObjectDeleted(Object obj);
}