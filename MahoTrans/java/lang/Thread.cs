using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace java.lang;

public class Thread : Object, Runnable
{
    [JavaIgnore] protected JavaThread JavaThread = null!;

    [JavaIgnore] [ThreadStatic] public static JavaThread? CurrentThread;

    [JavaType(typeof(Runnable))] public Reference _target;
    [String] public Reference _name;
    public bool started;

    [InitMethod]
    public void InitEmpty()
    {
    }

    [InitMethod]
    public void InitNamed([String] Reference str)
    {
        _name = str;
    }

    [InitMethod]
    public void InitTargeted([JavaType(typeof(Runnable))] Reference t)
    {
        _target = t;
    }

    [InitMethod]
    public void InitFull([JavaType(typeof(Runnable))] Reference t, [String] Reference str)
    {
        _target = t;
        _name = str;
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody run(JavaClass @class)
    {
        return new JavaMethodBody
        {
            LocalsCount = 1,
            StackSize = 2,
            Code = new Instruction[]
            {
                new Instruction(0, JavaOpcode.aload_0),
                new Instruction(1, JavaOpcode.getfield,
                    @class.PushConstant(new NameDescriptorClass("_target", "Ljava/lang/Runnable;", "java/lang/Thread"))
                        .Split()),
                new Instruction(4, JavaOpcode.dup),
                new Instruction(5, JavaOpcode.ifnull, new byte[] { 0, 6 }),
                new Instruction(8, JavaOpcode.invokevirtual,
                    @class.PushConstant(new NameDescriptorClass("run", "()V", "java/lang/Runnable")).Split()),
                new Instruction(11, JavaOpcode.@return),
            }
        };
    }

    public static void sleep(long time)
    {
        //return;
        if (CurrentThread != null)
            Jvm.Detach(CurrentThread, time);
    }

    public static void yield()
    {
    }

    public bool isAlive() => JavaThread.ActiveFrame != null;

    public void start()
    {
        if (started)
            Jvm.Throw<IllegalThreadStateException>();
        started = true;
        var javaThread = JavaThread.CreateReal(this, Jvm);
        Jvm.RegisterThread(javaThread);
        JavaThread = javaThread;
    }
}