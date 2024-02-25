# Building "native" (for JVM) methods

According to the basic principles of operation, there is no way to return to interpreter once bridge was entered.

But what if you want to invoke a callback or overriden client code? There is a quick example - `equals()` method.

## Workarounds

There are several crude ways to work around that.

### Running nested loop

i.e. altering stacks and calling `ExecuteLoop()` again. **Never do this.**

### Running nested JVM

This *may be acceptable* if you need to do isolated calculations, for example, execute code from a plugin or something
like that.
If you need this you probably want to implement this at frontend level and integrate with your toolkits.

## Proper way: just do not enter CLR method

Don't leave interpreter. Do your stuff using java bytecode. This may look like this:

```
(some glue code)
call to CLR method that will prepare the data
(some glue code)
virtual call to your callbacks
(some glue code)
call to CLR method that will process output
```

First, you need to declare a special "builder" method.
"Builder" method in a CLR class is method that won't be invoked via bridge, but will be executed and "method" that it
will construct will be used by interpreter.

Builder method can be both static and instance. It must return `JavaMethodBody`. Its only argument must be `JavaClass`.

As there are no marshalled arguments, linker won't know them.
So, you must attach `JavaDescriptor` attribute and specify full descriptor there.

The next step is to construct your MethodBody using class you got via args. There are 2 ways to do this.

### Using builder util

Example from [StartupThread](../MahoTrans/javax/microedition/ams/lifecycle/StartupThread.cs):

```
var b = new JavaMethodBuilder(cls);
b.AppendThis();
b.AppendVirtcall(nameof(AllocMidlet), typeof(Reference));
b.Append(JavaOpcode.dup);
b.AppendVirtcall("<init>", typeof(void));
b.AppendVirtcall("startApp", typeof(void));
b.AppendReturn();
return b.Build(2, 1);
```

You create builder object and pass the class there. Builder does all the stuff to maintain offsets/constants. It
contains set of shortcut for emitting calls/jumps/fields. If you need primitive things, do `Append(opcode)`. `Build()`
call argument are stack and locals count. Keep in mind that `this` is the zero variable, so non-static method always has
at least one variable.

This example also shows usage of auxiliary routines - `AllocMidlet` is CLR method that prepares the object, then the
method does two virtual calls.

[Hashtable](../MahoTrans/java/util/Hashtable.cs) and [InputStream](../MahoTrans/java/io/InputStream.cs) are good
examples of building complex loops with branches and try-catches.

**This is the "youngest" and recommended tool.**

### Direct construction of method

Example from [Thread](../MahoTrans/java/lang/Thread.cs).run():

```
return new JavaMethodBody
{
    LocalsCount = 1,
    StackSize = 2,
    Code = new[]
    {
        new Instruction(0, JavaOpcode.aload_0),
        new Instruction(1, JavaOpcode.getfield,
            cls.PushConstant(new NameDescriptorClass("_target", "Ljava/lang/Runnable;", "java/lang/Thread"))
                .Split()),
        new Instruction(4, JavaOpcode.dup),
        new Instruction(5, JavaOpcode.ifnull, new byte[] { 0, 7 }),
        new Instruction(8, JavaOpcode.invokevirtual,
            cls.PushConstant(new NameDescriptorClass("run", "()V", "java/lang/Runnable")).Split()),
        new Instruction(11, JavaOpcode.@return),
        new Instruction(12, JavaOpcode.@return),
    }
};
```

You create `JMB` object, set your stack&locals and instructions list. You may use `RawCode` property to make offsets
calculated automatically. Use `PushConstant` util to save your descriptors/numbers/texts to class and get int16 pointer.
Keep in mind that branch offsets are relative from called. Instruction/opcode arguments are raw, as they are stored
in `.class` file. Check [JVM specifications](https://docs.oracle.com/javase/specs/jvms/se7/html/jvms-6.html) to recall
opcodes and their data.

Generally, you should not use this method when writing new code. It is keep around because it works and there are no
reasons to rewrite it.

## Q/A

### What if I specify wrong stack/locals size?
Linker analyzes this at boot time and will give you bunch of errors.

### What if I emit broken bytecode?
Linker processes "builder" methods like bytecode loaded from JAR. You will face bunch of errors and you will need to fix them.

### How to call CLR method hidden from JVM?
Emitted bytecode will be executed on JVM so all fields/methods must be accessible to it.

### Are there ways to embed `.class` files directly to libraries?
No. This is by design. Frontend may solve this by allowing to load set of "polyfills" before loading guest application.

### Are there ways to "merge" clr type and such polyfill?
No. Declare full class in your polyfill.

### Are there ways to compile java source code on fly?
No. Precompile it and make your frontend load such libs after MT and before guest application.

