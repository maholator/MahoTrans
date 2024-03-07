// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Runtime;

namespace MahoTrans.Compiler;

public partial class CrossRoutineCompilerPass
{
    private static int _hostAsmCounter = 1;

    public static void CrossCompileMethod(JavaMethodBody method, ModuleBuilder module)
    {
        var ranges = method.GetPossiblyCompilableRanges();
        if (ranges.Count == 0)
            return;

        var host = module.DefineType($"CrossHost_{method.Method.Class.Name}_{method.Method.Descriptor}",
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];
            var state = new CrossRoutineCompilerPass(method, range, host, $"Routine_{i}");
            state.Compile();
        }

        var built = host.CreateType()!;

        for (int i = 0; i < ranges.Count; i++)
        {
            var bridge = built.GetMethod($"Routine_{i}")!.CreateDelegate<Action<Frame>>();
            var poppedCount = ranges[i].StackOnEnter.HasValue ? 1 : 0;
            method.LinkedCode[ranges[i].Start] =
                new LinkedInstruction(MTOpcode.bridge, 0, poppedCount, bridge);
        }
    }

    public static void CrossCompileAll(JvmState jvm)
    {
        var name = new AssemblyName($"{JvmState.CROSS_ROUTINES_DLL_PREFIX}{_hostAsmCounter}");

        var builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
        var module = builder.DefineDynamicModule($"{JvmState.CROSS_ROUTINES_DLL_PREFIX}{_hostAsmCounter}");
        _hostAsmCounter++;

        foreach (var cls in jvm.Classes.Values)
        {
            foreach (var method in cls.Methods.Values)
            {
                if (method.JavaBody != null)
                    CrossCompileMethod(method.JavaBody, module);
            }
        }
    }
}
