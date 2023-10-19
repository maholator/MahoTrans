using MahoTrans.Environment;
using MahoTrans.Loader;
using MahoTrans.Runtime;

internal class Program
{
    public static void Main(string[] args)
    {
        var all = ClassLoader.ReadJar(
            File.OpenRead("/home/ansel/repos/j2me/nmania/deployed/DefaultColorPhone/nmania.jar"),
            false);

        var jvm = new JvmState(null!);
        EnvironmentInitializer.Init(jvm, typeof(JavaRunner).Assembly);
        jvm.AddJvmClasses(all, "nmania", "nmania");

        BenchmarkClinit(jvm);
        RunMidlet(jvm);

        GC.Collect();
    }

    private static void BenchmarkClinit(JvmState jvm)
    {
        jvm.RunInContext(() =>
        {
            for (int i = 0; i < 128; i++)
            {
                foreach (var cls in jvm.Classes.Values)
                {
                    if (cls.Methods.TryGetValue(new NameDescriptor("<clinit>", "()V"), out var init))
                    {
                        if (init.IsNative)
                            init.NativeBody.Invoke(null, Array.Empty<object?>());
                        else
                            JavaThread.CreateSyntheticStaticAction(init, jvm).Execute(jvm);
                    }
                }
            }
        });
    }

    private static void RunMidlet(JvmState jvm)
    {
        var midlet = jvm.Heap.AllocateObject(jvm.Classes["nmania/Nmania"]);
        jvm.RunInContext(() =>
        {
            JavaThread.CreateSyntheticVirtualAction("<init>", midlet, jvm).Execute(jvm);
            JavaThread.CreateSyntheticVirtualAction("startApp", midlet, jvm).Execute(jvm);
        });
    }
}
/*
var all = ClassLoader.ReadJar(File.OpenRead("/home/ansel/repos/j2me/nmania/deployed/DefaultColorPhone/nmania.jar"),
    false);

int i = 0;

var jvm = new JvmState();
EnvironmentInitializer.Init(jvm, typeof(JavaObject).Assembly);
jvm.AddJvmClasses(all, "nmania","nmania");

Console.WriteLine("Loaded.");

var app = JavaThread.CreateSynthetic(all.First(x => x.Name == "nmania/Nmania").Methods.First(x => x.Name == "startApp"),
    jvm.Heap.AllocateObject("nmania/Nmania"), Array.Empty<object>(), jvm);

while (!app.Dead)
    JavaRunner.Step(app, jvm);
    
    */