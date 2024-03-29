// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Loader;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Config;
using MahoTrans.ToolkitImpls.Clocks;
using MahoTrans.ToolkitImpls.Loggers;
using MahoTrans.ToolkitImpls.Rms;
using MahoTrans.ToolkitImpls.Stub;

namespace MahoTrans.Testing;

public class JavaTest
{
    private readonly JvmState _jvm;

    public JavaTest(Stream? jarFile, string className, string methodName)
    {
        if (jarFile == null)
            throw new FileNotFoundException();

        ToolkitCollection tk = new ToolkitCollection(
            new StubSystem(),
            new RealTimeClock(),
            null!,
            null!,
            null!,
            new VirtualRms(), null!)
        {
            Logger = new ConsoleLogger(),
            LoadLogger = new ConsoleLogger()
        };
        _jvm = new JvmState(tk, ExecutionManner.Unlocked);
        _jvm.AddMahoTransLibrary();
        var cl = new ClassLoader(new ConsoleLogger());
        var cls = cl.ReadJarFile(jarFile, true);
        _jvm.AddJvmClasses(cls, "jar");

        _jvm.LinkAndLock();

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
