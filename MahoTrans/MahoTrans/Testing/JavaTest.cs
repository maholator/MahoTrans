using MahoTrans.Loader;
using MahoTrans.Runtime;
using MahoTrans.ToolkitImpls.Ams;
using MahoTrans.ToolkitImpls.Clocks;
using MahoTrans.ToolkitImpls.Loggers;
using MahoTrans.ToolkitImpls.Rms;
using MahoTrans.Toolkits;

namespace MahoTrans.Testing;

public class JavaTest
{
    private readonly JvmState _jvm;

    public JavaTest(Stream? jarFile, string className, string methodName)
    {
        if (jarFile == null)
            throw new FileNotFoundException();

        Toolkit tk = new Toolkit(
            null!,
            new RealTimeClock(),
            null!,
            null!,
            null!,
            new AmsEventHub(),
            new InMemoryRms(),
            new ConsoleLogger(),
            new ConsoleLogger());
        _jvm = new JvmState(tk);
        _jvm.AddClrClasses(typeof(JavaRunner).Assembly);
        var cl = new ClassLoader(new ConsoleLogger());
        var cls = cl.ReadJarFile(jarFile, true);
        _jvm.AddJvmClasses(cls, "jar", "jar");
        var obj = _jvm.AllocateObject(_jvm.GetClass(className));


        var thread = JavaThread.CreateSynthetic(new NameDescriptor(methodName, "()V"), obj, _jvm);
        _jvm.RunInContext(() => thread.start());
        _jvm.BetweenBunches += _ =>
        {
            if (!thread.isAlive())
                _jvm.Stop();
        };
    }

    public JavaTest Run()
    {
        _jvm.ExecuteLoop();

        return this;
    }
}