// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using System.Reflection.Emit;
using MahoTrans.Runtime;

namespace MahoTrans.Compiler;

public partial class CrossRoutineCompilerPass
{
    public static void CrossCompileMethod(JavaMethodBody method, ModuleBuilder module)
    {
        var ranges = method.GetPossiblyCompilableRanges();
        if (ranges.Count == 0)
            return;

        var host = module.DefineType($"CrossHost_{method.Method.Class.Name}_{method.Method.Descriptor}",
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);

        List<int> applicationPoints = new();

        foreach (var range in ranges)
        {
            var state = new CrossRoutineCompilerPass(method, range, host, $"Routine_{applicationPoints.Count}");
            state.Compile();
            applicationPoints.Add(range.Start);
        }

        var built = host.CreateType()!;

        for (int i = 0; i < applicationPoints.Count; i++)
        {
            var bridge = built.GetMethod($"Routine_{i}")!.CreateDelegate<Action<Frame>>();
            method.LinkedCode[applicationPoints[i]] = new LinkedInstruction(MTOpcode.bridge, 0, 0, bridge);
        }
    }
}