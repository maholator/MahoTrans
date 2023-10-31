using System.Reflection;
using MahoTrans.Runtime;

namespace MahoTrans.Environment;

[Obsolete("This must not exist. Frontend must load MahoTrans dll via direct call.")]
public static class EnvironmentInitializer
{
    public static void Init(JvmState state, Assembly source) => state.AddClrClasses(source);
}