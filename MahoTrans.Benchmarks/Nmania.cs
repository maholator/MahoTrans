using BenchmarkDotNet.Attributes;
using MahoTrans.Dummy;
using MahoTrans.Environment;
using MahoTrans.Loader;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;

namespace MahoTrans.Benchmarks;

[MemoryDiagnoser]
public class Nmania
{
    private JvmState jvm;
    private JavaClass[] classes;
    private volatile uint x;
    private volatile float f;

    [GlobalSetup]
    public void Setup()
    {
        var all = ClassLoader.ReadJar(
            File.OpenRead("/home/ansel/repos/j2me/nmania/deployed/DefaultColorPhone/nmania.jar"),
            false);

        jvm = new JvmState(new DummyToolkit());
        EnvironmentInitializer.Init(jvm, typeof(JavaRunner).Assembly);
        jvm.AddJvmClasses(all, "nmania", "nmania");
        classes = all.Item1;
        x = 23;
    }

    [Benchmark()]
    public void Clinit()
    {
        jvm.RunInContext(() =>
        {
            foreach (var cls in classes)
            {
                if (cls.Methods.TryGetValue(new NameDescriptor("<clinit>", "()V"), out var init))
                {
                    if (init.IsNative)
                        init.NativeBody.Invoke(null, Array.Empty<object?>());
                    else
                        JavaThread.CreateSyntheticStaticAction(init, jvm).Execute(jvm);
                }
            }
        });
    }

    /*
    [Benchmark()]
    public void Managed()
    {
        f = BitConverter.UInt32BitsToSingle(x);
        f = BitConverter.UInt32BitsToSingle(x);
        f = BitConverter.UInt32BitsToSingle(x);
        f = BitConverter.UInt32BitsToSingle(x);
        f = BitConverter.UInt32BitsToSingle(x);
        f = BitConverter.UInt32BitsToSingle(x);
        f = BitConverter.UInt32BitsToSingle(x);
    }

    [Benchmark()]
    public unsafe void Unmanaged()
    {
        uint xx = x;
        f = *(float*)&xx;
        f = *(float*)&xx;
        f = *(float*)&xx;
        f = *(float*)&xx;
        f = *(float*)&xx;
        f = *(float*)&xx;
        f = *(float*)&xx;
        f = *(float*)&xx;
    }*/
}