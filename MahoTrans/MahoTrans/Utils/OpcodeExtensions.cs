// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Utils;

public static class OpcodeExtensions
{
    /// <summary>
    /// Returns true if execution of this opcode will always perform jump.
    /// </summary>
    /// <param name="opcode">Opcode to check.</param>
    public static bool IsJumpOpcode(this JavaOpcode opcode)
    {
        switch (opcode)
        {
            case JavaOpcode.@goto:
            case JavaOpcode.jsr:
            case JavaOpcode.ret:
            case JavaOpcode.tableswitch:
            case JavaOpcode.lookupswitch:
            case JavaOpcode.ireturn:
            case JavaOpcode.lreturn:
            case JavaOpcode.freturn:
            case JavaOpcode.dreturn:
            case JavaOpcode.areturn:
            case JavaOpcode.@return:
            case JavaOpcode.athrow:
            case JavaOpcode.goto_w:
            case JavaOpcode.jsr_w:
            case JavaOpcode._inplacereturn:
                return true;

            default:
                return false;
        }
    }
}