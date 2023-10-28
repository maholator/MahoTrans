namespace MahoTrans.Utils;

public class SafeAssembly
{
    public string? FullName;
    public SafeAssembly ( System.Reflection.Assembly ass )
    {
        this.FullName = ass.FullName;
    }
}