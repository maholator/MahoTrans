// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Loader;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Config;
using MahoTrans.ToolkitImpls.Ams;
using MahoTrans.ToolkitImpls.Clocks;
using MahoTrans.ToolkitImpls.Loggers;
using MahoTrans.ToolkitImpls.Rms;
using MahoTrans.ToolkitImpls.Systems;

namespace MahoTrans.Testing;

public class JavaTest
{
    private readonly JvmState _jvm;

    public JavaTest(Stream? jarFile, string className, string methodName)
    {
        if (jarFile == null)
            throw new FileNotFoundException();

        ToolkitCollection tk = new ToolkitCollection(
            new DummySystem(),
            new RealTimeClock(),
            null!,
            null!,
            null!,
            new AmsEventHub(),
            new VirtualRms(), null!)
        {
            Logger = new ConsoleLogger(),
            LoadLogger = new ConsoleLogger()
        };
        _jvm = new JvmState(tk, ExecutionManner.Unlocked);
        _jvm.AddClrClasses(typeof(JavaRunner).Assembly);
        var cl = new ClassLoader(new ConsoleLogger());
        var cls = cl.ReadJarFile(jarFile, true);
        _jvm.AddJvmClasses(cls, "jar", "jar");
        var obj = _jvm.AllocateObject(_jvm.GetClass(className));


        var thread = JavaThread.CreateSynthetic(new NameDescriptor(methodName, "()V"), obj, _jvm);
        using (new JvmContext(_jvm))
            thread.start();
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