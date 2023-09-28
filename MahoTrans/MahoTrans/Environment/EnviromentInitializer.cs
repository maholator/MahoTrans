using System.Reflection;
using MahoTrans.Runtime;

namespace MahoTrans.Environment;

public static class EnvironmentInitializer
{
    public static void Init(JvmState state, Assembly source) => state.AddClrClasses(source);

    
}