using MahoTrans.Runtime.Types;
using Thread = java.lang.Thread;

namespace MahoTrans.Runtime;

/// <summary>
/// JVM thread. Create it using static methods. Then run it using <see cref="Execute"/> or add to jvm pool using <see cref="JvmState.RegisterThread"/>.
/// </summary>
public class JavaThread
{
    private static int _roller = 1;

    /// <summary>
    /// Frames stack on this thread. Do not touch it, use <see cref="Push"/> and <see cref="Pop"/> instead.
    /// </summary>
    public Frame?[] CallStack = new Frame?[16];

    /// <summary>
    /// Index at which <see cref="ActiveFrame"/> is in <see cref="CallStack"/>. To push a frame, increase this and write new frame at the index.
    /// </summary>
    public int ActiveFrameIndex = -1;

    /// <summary>
    /// This field stores running frame. Do not touch it, use <see cref="Push"/> and <see cref="Pop"/> instead. This may be null if thread is dead.
    /// </summary>
    public Frame? ActiveFrame;

    public readonly int ThreadId;

    public JavaThread()
    {
        ThreadId = _roller;
        _roller++;
    }

    public JavaThread(Frame root, JvmState jvm) : this()
    {
        root.Method.EnsureBytecodeLinked(jvm);
        ActiveFrameIndex = 0;
        ActiveFrame = root;
        CallStack[0] = root;
    }

    public Frame Push(JavaMethodBody method)
    {
        ActiveFrameIndex++;
        if (ActiveFrameIndex == CallStack.Length)
        {
            var a = new Frame[CallStack.Length * 2];
            Array.Copy(CallStack, a, CallStack.Length);
            CallStack = a;
        }

        var f = CallStack[ActiveFrameIndex];
        if (f == null)
        {
            f = new Frame(method);
            CallStack[ActiveFrameIndex] = f;
        }
        else
        {
            f.Reinitialize(method);
        }

        ActiveFrame = f;

        return f;
    }

    public void Pop()
    {
        ActiveFrameIndex--;
        if (ActiveFrameIndex >= 0)
            ActiveFrame = CallStack[ActiveFrameIndex];
        else
            ActiveFrame = null;
    }

    /// <summary>
    /// Spins thread until it ends. It is not guaranteed that this method will ever return.
    /// </summary>
    /// <param name="state">JVM to run in.</param>
    public void Execute(JvmState state)
    {
        while (ActiveFrame != null)
        {
            JavaRunner.Step(this, state);
        }
    }

    /// <summary>
    /// Creates a new thread which runs specific java method.
    /// </summary>
    /// <param name="launcher">Method to run.</param>
    /// <param name="target">Object to call method on. Pass <c>null</c> if method is static. Passing null reference (with zero pointer) will call method with null <c>this</c>.</param>
    /// <param name="args">List of args to pass.</param>
    /// <param name="state">JVM to operate on.</param>
    /// <returns>Thread with ready to run frame.</returns>
    public static JavaThread CreateSynthetic(Method launcher, Reference target, long[] args, JvmState state)
    {
        var f = new Frame(launcher.JavaBody);
        if (launcher.IsStatic)
        {
            if (!target.IsNull)
                throw new JavaRuntimeError($"Attempt to invoke static method {launcher} on object.");
            args.CopyTo(f.LocalVariables, 0);
        }
        else
        {
            if (target.IsNull)
                throw new JavaRuntimeError($"Attempt to call instance method {launcher} without object.");
            f.LocalVariables[0] = target;
            args.CopyTo(f.LocalVariables, 1);
        }

        var javaThread = new JavaThread(f, state);
        if (launcher.Class.PendingInitializer)
            launcher.Class.Initialize(javaThread, state);
        return javaThread;
    }

    public static JavaThread CreateSyntheticVirtualAction(string name, Reference target, JvmState state) =>
        CreateSyntheticVirtual(new NameDescriptor(name, "()V"), target, Array.Empty<long>(), state);

    public static JavaThread CreateSyntheticVirtual(NameDescriptor name, Reference target, long[] args,
        JvmState state) =>
        CreateSynthetic(state.GetVirtualMethod(state.GetVirtualPointer(name), target), target, args, state);

    public static JavaThread CreateSyntheticStaticAction(JavaClass @class, string name, JvmState state)
    {
        if (!@class.Methods.TryGetValue(new NameDescriptor(name, "()V"), out var method))
            throw new JavaRuntimeError($"Static action {name} was not found on {@class}.");
        return CreateSynthetic(method, default, Array.Empty<long>(), state);
    }

    public static JavaThread CreateSyntheticStaticAction(Method method, JvmState state)
    {
        return CreateSynthetic(method, default, Array.Empty<long>(), state);
    }

    public static JavaThread CreateSyntheticStatic(NameDescriptorClass name, long[] args, JvmState state) =>
        CreateSynthetic(state.GetMethod(name, true), default, args, state);


    public static JavaThread CreateReal(Thread thread, JvmState state)
    {
        if (thread.This.IsNull)
            throw new JavaRuntimeError("Attempt to launch thread which is created not in java heap.");

        var method = state.GetVirtualMethod(state.GetVirtualPointer(new NameDescriptor("run", "()V")), thread.This);

        var f = new Frame(method.JavaBody);
        f.LocalVariables[0] = thread.This;

        var javaThread = new JavaThread(f, state);
        if (thread.JavaClass.PendingInitializer)
            thread.JavaClass.Initialize(javaThread, state);
        return javaThread;
    }
}