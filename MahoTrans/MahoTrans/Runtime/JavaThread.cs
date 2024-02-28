// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Runtime.Errors;
using Array = System.Array;
using Object = java.lang.Object;
using Thread = java.lang.Thread;

namespace MahoTrans.Runtime;

/// <summary>
///     JVM thread. Create it via static methods.
///     Then run it using <see cref="Execute" /> or add to jvm pool using <see cref="JvmState.RegisterThread" />.
/// </summary>
public class JavaThread
{
    private static int _roller = 1;

    /// <summary>
    ///     Frames stack on this thread. Do not touch it, use <see cref="Push" /> and <see cref="Pop" /> instead.
    /// </summary>
    public Frame?[] CallStack = new Frame?[16];

    /// <summary>
    ///     Index at which <see cref="ActiveFrame" /> is in <see cref="CallStack" />. To push a frame, increase this and write
    ///     new frame at the index.
    /// </summary>
    public int ActiveFrameIndex = -1;

    /// <summary>
    ///     This field stores running frame. Do not touch it, use <see cref="Push" /> and <see cref="Pop" /> instead.
    ///     This may be null if thread is dead.
    ///     There must not be a situation when this is null and thread is running, use <see cref="JvmState.Kill" /> before
    ///     clearing.
    /// </summary>
    public Frame? ActiveFrame;

    public readonly int ThreadId;

    public readonly Reference Model;

    /// <summary>
    ///     List of thread's IDs, waiting at <see cref="java.lang.Thread.join" />.
    /// </summary>
    public List<int> WaitingForKill = new();

    public JavaThread(Reference model)
    {
        Model = model;
        ThreadId = _roller;
        _roller++;
    }

    public JavaThread(Reference model, int id)
    {
        Model = model;
        ThreadId = id;
    }

    public JavaThread(Frame root, Reference model)
        : this(model)
    {
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

    /// <summary>
    ///     Pops thread's frame. If thread is finishing its work, this must be called from JVM context.
    /// </summary>
    public void Pop()
    {
        ActiveFrameIndex--;
        if (ActiveFrameIndex >= 0)
            ActiveFrame = CallStack[ActiveFrameIndex];
        else
        {
            ActiveFrame = null;
            Object.Jvm.Kill(this);
        }
    }

    /// <summary>
    ///     Spins thread until it ends. It is not guaranteed that this method will ever return. This must be called inside jvm
    ///     context.
    /// </summary>
    public void Execute()
    {
        var jvm = Object.Jvm;

        while (ActiveFrame != null)
        {
            JavaRunner.Step(this, jvm);
        }
    }

    /// <summary>
    ///     Creates synthetic thread, i.e. thread that executes specified method. Uses <see cref="AnyCallBridge" />.
    ///     Only creates java-side model object and sets it up. Call <see cref="Thread.start" /> on it to continue.
    /// </summary>
    /// <param name="nd">Method's descriptor.</param>
    /// <param name="target">Object to call the method on.</param>
    /// <param name="state">Jvm.</param>
    /// <returns></returns>
    public static Thread CreateSynthetic(NameDescriptor nd, Reference target, JvmState state)
    {
        var bridge = state.Allocate<AnyCallBridge>();
        bridge.Init(target, state.AllocateString(nd.Name), state.AllocateString(nd.Descriptor));

        var model = state.Allocate<Thread>();
        model.InitTargeted(bridge.This);

        return model;
    }

    /// <summary>
    ///     Creates thread for passed java model. Does nothing with the created thread. This must be called inside JVM context.
    /// </summary>
    /// <param name="thread">Thread to start.</param>
    /// <returns>MT thread object.</returns>
    /// <remarks>
    ///     If you want to start a java thread from native code, this is not for you. Allocate a <see cref="Thread" /> via
    ///     <see cref="JvmState.Allocate{T}" />, initialize it with a runnable via <see cref="Thread.InitTargeted" /> and
    ///     call <see cref="Thread" />.<see cref="Thread.start" />.
    /// </remarks>
    public static unsafe JavaThread CreateReal(Thread thread)
    {
        if (thread.This.IsNull)
            throw new JavaRuntimeError("Attempt to launch thread which is created not in java heap.");

        var jvm = Object.Jvm;
        var method = jvm.Classes["java/lang/Thread"].Methods[new NameDescriptor(nameof(Thread.runInternal), "()V")];

        var f = new Frame(method.JavaBody!);
        f.LocalVariables[0] = thread.This;

        var javaThread = new JavaThread(f, thread.This);
        if (thread.JavaClass.PendingInitializer)
            thread.JavaClass.Initialize(javaThread);
        return javaThread;
    }
}