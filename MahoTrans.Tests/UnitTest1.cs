using MahoTrans.Dummy;
using MahoTrans.Environment;
using MahoTrans.Loader;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace MahoTrans.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
        Object.DetachHeap();
    }

    [Test]
    public void TestBasics()
    {
        var all = ClassLoader.ReadJar(
            File.OpenRead("/home/ansel/repos/j2me/MaholatorTests/deployed/DefaultColorPhone/MaholatorTests.jar"),
            false);

        var jvm = new JvmState(new DummyToolkit());
        EnvironmentInitializer.Init(jvm, typeof(JavaRunner).Assembly);
        jvm.AddJvmClasses(all, "test", "test");
    }

    [Test]
    public void TestNmaniaClinit()
    {
        var all = ClassLoader.ReadJar(
            File.OpenRead("/home/ansel/repos/j2me/nmania/deployed/DefaultColorPhone/nmania.jar"),
            false);

        var jvm = new JvmState(new DummyToolkit());
        EnvironmentInitializer.Init(jvm, typeof(JavaRunner).Assembly);
        jvm.AddJvmClasses(all, "nmania", "nmania");
        jvm.LogOpcodeStats();

    }

    [Test]
    public void TestMahomapsClinit()
    {
        var all = ClassLoader.ReadJar(
            File.OpenRead("/home/ansel/repos/j2me/mm-v1/deployed/DefaultColorPhone/mm-v1.jar"),
            false);

        var jvm = new JvmState(new DummyToolkit());
        EnvironmentInitializer.Init(jvm, typeof(JavaRunner).Assembly);
        jvm.AddJvmClasses(all, "mahomaps", "mahomaps");
        jvm.LogOpcodeStats();

    }

    [Test]
    public void TestDoodleJumpClinit()
    {
        var all = ClassLoader.ReadJar(
            File.OpenRead("/home/ansel/Downloads/Doodle Jump 240x320.jar"),
            false);

        var jvm = new JvmState(new DummyToolkit());
        EnvironmentInitializer.Init(jvm, typeof(JavaRunner).Assembly);
        jvm.AddJvmClasses(all, "dj", "dj");
        jvm.LogOpcodeStats();
    }

    [Test]
    public void TestDoodleJumpMidletRun()
    {
        var all = ClassLoader.ReadJar(
            File.OpenRead("/home/ansel/Downloads/Doodle Jump 240x320.jar"),
            false);

        var jvm = new JvmState(new DummyToolkit());
        EnvironmentInitializer.Init(jvm, typeof(JavaRunner).Assembly);
        jvm.AddJvmClasses(all, "dj", "dj");

        var midlet = jvm.Heap.AllocateObject(jvm.Classes["S"]);
        jvm.RunInContext(() =>
        {
            JavaThread.CreateSyntheticVirtualAction("<init>", midlet, jvm).Execute(jvm);
            JavaThread.CreateSyntheticVirtualAction("startApp", midlet, jvm).Execute(jvm);
        });
        //jvm.Execute();
    }

    [Test]
    public void TestNmaniaMidletRun()
    {
        var all = ClassLoader.ReadJar(
            File.OpenRead("/home/ansel/repos/j2me/nmania/deployed/DefaultColorPhone/nmania.jar"),
            false);

        var jvm = new JvmState(new DummyToolkit());
        EnvironmentInitializer.Init(jvm, typeof(JavaRunner).Assembly);
        jvm.AddJvmClasses(all, "nmania", "nmania");

        var midlet = jvm.Heap.AllocateObject(jvm.Classes["nmania/Nmania"]);
        jvm.RunInContext(() =>
        {
            JavaThread.CreateSyntheticVirtualAction("<init>", midlet, jvm).Execute(jvm);
            JavaThread.CreateSyntheticVirtualAction("startApp", midlet, jvm).Execute(jvm);
        });
        //jvm.Execute();
    }
}