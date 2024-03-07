// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

using java.lang;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;

namespace MahoTrans;

/// <summary>
///     Converted opcodes, which are used in <see cref="LinkedInstruction" />.
/// </summary>
public enum MTOpcode : byte
{
    nop = 0,

    #region Constants

    aconst_0,
    iconst_m1,
    iconst_0,
    iconst_1,
    iconst_2,
    iconst_3,
    iconst_4,
    iconst_5,
    lconst_0,
    lconst_1,
    lconst_2,
    fconst_0,
    fconst_1,
    fconst_2,
    dconst_0,
    dconst_1,
    dconst_2,

    /// <summary>
    ///     Pushes integer from <see cref="LinkedInstruction.IntData" />.
    /// </summary>
    iconst,

    /// <summary>
    ///     Pushes float from <see cref="LinkedInstruction.IntData" />.
    /// </summary>
    fconst,

    /// <summary>
    ///     Pushes <see cref="LinkedInstruction.Data" /> as a string.
    /// </summary>
    strconst,

    /// <summary>
    ///     Pushes <see cref="LinkedInstruction.Data" /> as a long.
    /// </summary>
    lconst,

    /// <summary>
    ///     Pushes <see cref="LinkedInstruction.Data" /> as a double.
    /// </summary>
    dconst,

    #endregion

    #region Locals

    load,
    store,
    iinc,

    #endregion

    #region Arrays

    iaload,
    laload,
    faload,
    daload,
    aaload,
    baload,
    caload,
    saload,

    iastore,
    lastore,
    fastore,
    dastore,
    aastore,
    bastore,
    castore,
    sastore,

    array_length,

    #endregion

    #region Stack

    pop,
    pop2,
    swap,
    dup,
    dup2,
    dup_x1,
    dup_x2,
    dup2_x1,

    #endregion

    #region Math

    iadd,
    ladd,
    fadd,
    dadd,
    isub,
    lsub,
    fsub,
    dsub,
    imul,
    lmul,
    fmul,
    dmul,
    idiv,
    ldiv,
    fdiv,
    ddiv,
    irem,
    lrem,
    frem,
    drem,
    ineg,
    lneg,
    fneg,
    dneg,
    ishl,
    lshl,
    ishr,
    lshr,
    iushr,
    lushr,
    iand,
    land,
    ior,
    lor,
    ixor,
    lxor,

    #endregion

    #region Conversions

    i2l,
    i2f,
    i2d,
    l2i,
    l2f,
    l2d,
    f2i,
    f2l,
    f2d,
    d2i,
    d2l,
    d2f,
    i2b,
    i2c,
    i2s,

    #endregion

    #region Comparisons

    lcmp,
    fcmpl,
    fcmpg,
    dcmpl,
    dcmpg,

    #endregion

    #region Branching

    ifeq,
    ifne,
    iflt,
    ifge,
    ifgt,
    ifle,
    if_cmpeq,
    if_cmpne,
    if_cmplt,
    if_cmpge,
    if_cmpgt,
    if_cmple,
    tableswitch,
    lookupswitch,

    #endregion

    #region Jumps

    jump,
    return_value,
    return_void,
    return_void_inplace,
    athrow,

    #endregion

    #region Calls

    invoke_virtual,
    invoke_static,
    invoke_instance,
    invoke_virtual_void_no_args_bysig,

    #endregion

    #region Fileds (static)

    /// <summary>
    ///     Gets value from <see cref="JvmState.StaticFields" />. <see cref="LinkedInstruction.IntData" /> is field index.
    /// </summary>
    get_static,

    /// <summary>
    ///     Sets value in <see cref="JvmState.StaticFields" />. <see cref="LinkedInstruction.IntData" /> is field index.
    /// </summary>
    set_static,

    /// <summary>
    ///     Gets value from <see cref="JvmState.StaticFields" />. <see cref="LinkedInstruction.Data" /> is a
    ///     <see cref="JavaClass" /> to initialize. <see cref="LinkedInstruction.IntData" /> is field index.
    /// </summary>
    get_static_init,

    /// <summary>
    ///     Sets value in <see cref="JvmState.StaticFields" />. <see cref="LinkedInstruction.Data" /> is a
    ///     <see cref="JavaClass" /> to initialize. <see cref="LinkedInstruction.IntData" /> is field index.
    /// </summary>
    set_static_init,

    #endregion

    #region Allocs

    new_obj,
    new_prim_arr,
    new_arr,
    new_multi_arr,

    #endregion

    #region Monitors

    monitor_enter,
    monitor_exit,

    #endregion

    #region Casts

    checkcast,
    instanceof,

    #endregion

    #region Bridges

    /// <summary>
    ///     <see cref="LinkedInstruction.Data" /> is an <see cref="Action" /> that takes <see cref="Frame" /> and does
    ///     something on it.
    ///     <see cref="LinkedInstruction.IntData" /> must contain count of taken values.
    ///     For example, if bridge pops 2 values and pushes 3, <see cref="LinkedInstruction.IntData" /> will be equal to 2.
    /// </summary>
    bridge,

    /// <summary>
    ///     <see cref="LinkedInstruction.Data" /> is a <see cref="ClassBoundBridge" />. It contains class to init and a bridge
    ///     to run.
    ///     <see cref="LinkedInstruction.IntData" /> must contain count of taken values.
    ///     For example, if bridge pops 2 values and pushes 3, <see cref="LinkedInstruction.IntData" /> will be equal to 2.
    /// </summary>
    bridge_init,

    #endregion

    #region Errors

    /// <summary>
    ///     Throws <see cref="NoClassDefFoundError" />. Indicates linker failure. <see cref="LinkedInstruction.Data" />
    ///     contains name of missing class.
    /// </summary>
    error_no_class,

    /// <summary>
    ///     Throws <see cref="NoSuchFieldError" />. Indicates linker failure. <see cref="LinkedInstruction.Data" /> contains
    ///     name of missing field.
    /// </summary>
    error_no_field,

    /// <summary>
    ///     Throws <see cref="NoSuchMethodError" />. Indicates linker failure. <see cref="LinkedInstruction.Data" /> contains
    ///     name of missing method.
    /// </summary>
    error_no_method,

    /// <summary>
    ///     Aborts JVM execution. Indicates mailformed bytecode that was not linked.  <see cref="LinkedInstruction.Data" /> may
    ///     (but doesn't need to ) contain explanation.
    /// </summary>
    error_bytecode,

    #endregion
}
