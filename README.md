![logo](MahoTrans.Abstractions/icon.png)

# MahoTrans

JVM interpreter&recompiler written on C# as core for mobile apps emulators

## This project is:

- Implementation of Java Virtual Machine, compatible with JRE <1.3
- JNI-like system of embedding IL code into java runtime
- Implementation of CLDC, MIDP and some related JSRs
- Set of utils to build JVM classes and code with bytecode

## This project is not:

- Accurate port of any mobile device runtime environment: use emulators in SDKs instead.
- "Desktop" JVM: MT implements CLDC and MIDP and not compatible with SE/EE apps.
- Bi-directional bridge between JVM and CLR: use IKVM.NET instead.
- Java source code compiler: use `javac` instead. All java pieces of this are written on java bytecode.
- Something that you can run: MahoTrans is only one of the parts of an arbitrary emulator/RE. It can do nothing by
  itself.

## Basic concepts

- MahoTrans does nothing by itself: it can only run the code and provide basic libraries to it. Frontend implementation
  must manually initialize JVM pieces, load and inject JAR and auxiliary libraries and control the execution.
- All interaction with the "outside world" is done via "toolkits": services that provide certain sets of APIs. For
  example, there is a "clock toolkit": it's a service that provide time information to running application. It can be
  implemented in a naive way by querying OS time, converting it to java format and return as is, but also may contain
  complex logic, allowing time rewinding or something like that. Different toolkit implementations may be combined with
  each other to achieve certain goals.
- Common MahoTrans setup consists of MahoTrans itself, toolkit implementations and a frontend (app that controls
  MahoTrans and do toolkits' tasks).
- All JVM classes are dynamically compiled into CLR types, so, if there is a "Class1" class with "coordX" integer field
  in your app, a real CLR type "Class1" with "int coordX" field will be compiled.
- Heap is just a CLR array of references to instances of such CLR classes. Each object has a unique number - its
  address/index/number in heap "array".
- Methods are not compiled. MahoTrans executes java code in single-thread interpreter. This is a nightmare from
  performance perspective but allows to do some cool things. Only one java instruction may be executed in the same time.
- Any java code at any point, due to native call or precompiled block, may leave the interpreter and go to CLR code.
  Native calls are atomic, any native method execution takes one cycle from interpreter perspective.
- Transition between JVM and CLR is seamless: any virtual/special/static call may create one more java frame in a thread
  or go to native code. To make the transition as seamless as possible, MahoTrans dynamically compiles "bridges" - small
  utility routines those take "java" frame and "convert" it to CLR frame (by pushing values onto the stack or to
  locals). Then "bridge" calls CLR method and execution goes as there was no java code underneath it at all.
  Unfortunately, due to how things work, native code can't receive real objects and usually operate with MT java
  primitives. Marshalling is work in progress.
- Transition between CLR and JVM is not possible at all - native call is atomic, you can't return to interpreter during
  its execution.

## Key components

| Class               | Description                                                                                     |
|---------------------|-------------------------------------------------------------------------------------------------|
| `JvmState`          | MahoTrans "JVM". Heap, toolkits, classes, threads - here is everything.                         |
| `JvmContext`        | Native code may want to interact with JVM. This is the place where `JvmState` object is stored. |
| `ClassLoader`       | Thing that reads a JAR package from a `Stream` - classes, resources, etc.                       |
| `JarPackage`        | Object that you get from `ClassLoader`. Here is everything it managed to read.                  |
| `ClassCompiler`     | Converts JVM types to CLR types.                                                                |
| `NativeLinker`      | Converts CLR types to JVM types.                                                                |
| `BridgeCompiler`    | Builds bridges between JVM and CLR.                                                             |
| `JavaMethodBuilder` | Bytecode generator.                                                                             |
| `BytecodeLinker`    | Converts raw java code read by `ClassLoader` to something that can be executed.                 |
| `JavaRunner`        | Interpreter.                                                                                    |
| `Reference`         | Wrapper over `int`, index of an object in the heap.                                             |
| `ToolkitCollection` | Set of toolkits that MahoTrans will use.                                                        |
| `JavaThread`        | Interpreted thread.                                                                             |
| `JavaThrowable`     | Allows to throw JVM exceptions as CLR exceptions.                                               |

## Toolkits

| Class                                                                 | Description                                                                          |
|-----------------------------------------------------------------------|--------------------------------------------------------------------------------------|
| [ISystem](MahoTrans.Abstractions/Abstractions/ISystem.cs)             | Receives data from stdout/stderr. Provides properties and timezone.                  |
| [Clock](MahoTrans.Abstractions/Abstractions/Clock.cs)                 | Provides time and controls execution speed.                                          |
| [IImageManager](MahoTrans.Abstractions/Abstractions/IImageManager.cs) | Backend for LCDUI `Image`/`Graphics` classes.                                        |
| [IFontManager](MahoTrans.Abstractions/Abstractions/IFontManager.cs)   | Backend for LCDUI `Font` class.                                                      |
| [IDisplay](MahoTrans.Abstractions/Abstractions/IDisplay.cs)           | Backend for LCDUI `Display`/`Displayable` classes.                                   |
| [IAmsCallbacks](MahoTrans.Abstractions/Abstractions/IAmsCallbacks.cs) | Receives pause/resume/exit signals from MIDlet. Allows to perform platform requests. |
| [IRecordStore](MahoTrans.Abstractions/Abstractions/IRecordStore.cs)   | Backend for `RecordStore` class.                                                     |
| [IMedia](MahoTrans.Abstractions/Abstractions/IMedia.cs)               | Backend for MMAPI.                                                                   |
| [ILoadLogger](MahoTrans.Abstractions/Abstractions/ILoadLogger.cs)     | Receives messages from loader, linker and compiler.                                  |
| [ILogger](MahoTrans.Abstractions/Abstractions/ILogger.cs)             | Receives exception and runtime events.                                               |

Some stub implementations are provided out of the box. Others must be implemented by frontend.

MT manages only its own lifetime. Toolkits must be initialized/disposed/snapshoted/restored themselves.

Some toolkits do not need access to MT to work (for example, graphics, clock, record store). They should
reference [package](https://www.nuget.org/packages/ru.nnproject.MahoTrans.Abstractions/) directly and not depend on MT
itself.

## Example startup code

```csharp
private ToolkitCollection CreateToolkit() {
    // initialize your toolkit here
}

private JvmState Setup() {
    var loader = new ClassLoader(/* your logger */);
    using var s = /* get a stream somewhere, for example, */ File.Open();
    var jarPackage = loader.ReadJarFile(s, false);
    
    var j = new JvmState(CreateToolkit(), ExecutionManner.Unlocked);
    j.AddMahoTransLibrary(); // MT libs
    // load here your CLR libs, JAR libs and polyfills.
    j.AddJvmClasses(jarPackage, "name-of-your-app"); // your JAR
    return j;
}

static void Main(string midletClass) {
    var j = Setup(); // setup jvm
    using (new JvmContext(j)) // enter context
    {
        j.InitQueue(); // init ams queue
        // StartupThread is for example here: you may want to bootstrap your app in another way.
        var runner = j.AllocateObject<StartupThread>(); // alloc startup object
        runner.Manifest = new(); // we start with empty manifest
        runner.MidletClassName = midletClass; // assign class name
        runner.start(); // start a thread
        // MT does nothing by itself! Launch your threads you want it to execute.
        j.ExecuteLoop(); // run jvm
    }
}
```

## Docs

- [`Reference`, heap and serializer](docs/heap.md)
- [Marshalling and `JavaType` attribute: creating CLR library methods](docs/marshalling.md)
- [Builders and descriptors: creating JVM library methods](docs/javaemit.md)