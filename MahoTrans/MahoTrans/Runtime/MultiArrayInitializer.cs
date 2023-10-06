namespace MahoTrans.Runtime;

public class MultiArrayInitializer
{
    public int dimensions;
    public string type;

    public MultiArrayInitializer(int dimensions, string type)
    {
        this.dimensions = dimensions;
        this.type = type;
    }
}