// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection.Emit;
using MahoTrans.Runtime;

namespace MahoTrans.Compiler;

public partial class CrossRoutineCompilerPass
{
    /// <summary>
    ///     Wrap push to CLR stack into usage of this struct.
    /// </summary>
    private struct MarshallerWrapper : IDisposable
    {
        private CrossRoutineCompilerPass _pass;
        private PrimitiveType _primitive;
        private StackValuePurpose _purp;
        private Index _stackPos;

        public MarshallerWrapper(CrossRoutineCompilerPass pass, PrimitiveType primitive, StackValuePurpose purp,
            Index stackPos)
        {
            _pass = pass;
            _primitive = primitive;
            _purp = purp;
            _stackPos = stackPos;
            before();
        }

        public MarshallerWrapper(CrossRoutineCompilerPass pass, Index stackPos)
        {
            _pass = pass;
            var currInstr = _pass._instrIndex;
            // looking into next instruction
            _purp = _pass.StackPurposes[currInstr + 1][stackPos];
            _primitive = _pass.StackTypes[currInstr + 1][stackPos];
            _stackPos = stackPos;
            before();
        }

        public void Dispose() => after();

        private void before()
        {
            switch (_purp)
            {
                case StackValuePurpose.ToLocal:
                case StackValuePurpose.ReturnToStack:
                    // to return to frame, we need a frame.
                    _pass._il.Emit(OpCodes.Ldarg_0);
                    // others need nothing.
                    break;
            }
        }

        private void after()
        {
            switch (_purp)
            {
                case StackValuePurpose.Consume:
                case StackValuePurpose.ToLocal:
                    // we need raw value.
                    break;
                case StackValuePurpose.ReturnToStack:
                    _pass._il.Emit(OpCodes.Call, CompilerUtils.StackPushMethods[_primitive.ToType()]);
                    break;
                case StackValuePurpose.Target:
                    // resolve object
                    _pass._il.Emit(OpCodes.Call, CompilerUtils.ResolveAnyObject);
                    break;
                default:
                    // this is an array. Resolve an array.
                    _pass._il.Emit(OpCodes.Call, CompilerUtils.ResolveArrEx.MakeGenericMethod(_purp.ToArrayType()));
                    break;
                case StackValuePurpose.MethodArg:
                case StackValuePurpose.FieldValue:
                    // we need EXACT value. Applying marshaller.
                    var real = _pass.GetExactType(
                        _stackPos.GetOffset(_pass.StackPurposes[_pass._instrIndex + 1].Length));
                    var poppable = CompilerUtils.GetStackTypeFor(real);
                    var marshaller = MarshalUtils.GetMarshallerFor(poppable, false, real, false);
                    if (marshaller != null)
                        _pass._il.Emit(OpCodes.Call, marshaller);
                    break;
            }
        }
    }
}