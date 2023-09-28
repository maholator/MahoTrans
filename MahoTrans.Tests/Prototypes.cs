using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;

namespace MahoTrans.Tests;

public class Prototypes
{
    [Test]
    public void TryBuildGlue()
    {
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("test"), AssemblyBuilderAccess.RunAndCollect);
        var module = asm.DefineDynamicModule("test");
        var type = module.DefineType("type");
        var method = type.DefineMethod("glue", MethodAttributes.Static | MethodAttributes.Public,
            CallingConventions.Standard, null,
            new[] { typeof(Frame) });
        method.DefineParameter(1, ParameterAttributes.None, "javaFrame");
        var il = method.GetILGenerator();
        Frame f = new Frame(new JavaMethodBody(3, 3)
        {
            Method = new Method(new NameDescriptor(), 0, new JavaClass
            {
                Constants = Array.Empty<object>(),
            })
        });

        GenerateIL(il);

        var con = type.CreateType();

        var info = con!.GetMethod("glue");

        var del = info!.CreateDelegate<Action<Frame>>();

        f.PushInt(727);
        f.PushInt(292);
        del.Invoke(f);

        Assert.That(f.PopInt(), Is.EqualTo(292));
    }

    private void GenerateIL(ILGenerator il)
    {
        // for push
        il.Emit(OpCodes.Ldarg_0);


        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, 2);
        il.Emit(OpCodes.Call, typeof(Frame).GetMethod(nameof(Frame.SetFrom))!);

        il.Emit(OpCodes.Call, typeof(Prototypes).GetMethod(nameof(GetTest))!);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(Frame).GetMethod(nameof(Frame.PopIntFrom))!);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(Frame).GetMethod(nameof(Frame.PopIntFrom))!);


        il.Emit(OpCodes.Call, typeof(Test).GetMethod(nameof(Test.Min))!);

        il.Emit(OpCodes.Call, typeof(Frame).GetMethod(nameof(Frame.PushInt))!);
        il.Emit(OpCodes.Ret);
    }

    public static Test GetTest() => new Test();

    public class Test
    {
#pragma warning disable CA1822
        public int Min(int a, int b) => Math.Min(a, b);
#pragma warning restore CA1822
    }
}