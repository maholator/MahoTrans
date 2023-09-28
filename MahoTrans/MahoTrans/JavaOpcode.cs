// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace MahoTrans;

public enum JavaOpcode : byte
{
    nop = 0,
    aconst_null = 1,
    iconst_m1 = 2,
    iconst_0 = 3,
    iconst_1 = 4,
    iconst_2 = 5,
    iconst_3 = 6,
    iconst_4 = 7,
    iconst_5 = 8,
    lconst_0 = 9,
    lconst_1 = 10,
    fconst_0 = 11,
    fconst_1 = 12,
    fconst_2 = 13,
    dconst_0 = 14,
    dconst_1 = 15,
    [OpcodeArgsCount(1)] bipush = 16,
    [OpcodeArgsCount(2)] sipush = 17,
    [OpcodeArgsCount(1)] ldc = 18,
    [OpcodeArgsCount(2)] ldc_w = 19,
    [OpcodeArgsCount(2)] ldc2_w = 20,
    [OpcodeArgsCount(1)] iload = 21,
    [OpcodeArgsCount(1)] lload = 22,
    [OpcodeArgsCount(1)] fload = 23,
    [OpcodeArgsCount(1)] dload = 24,
    [OpcodeArgsCount(1)] aload = 25,
    iload_0 = 26,
    iload_1 = 27,
    iload_2 = 28,
    iload_3 = 29,
    lload_0 = 30,
    lload_1 = 31,
    lload_2 = 32,
    lload_3 = 33,
    fload_0 = 34,
    fload_1 = 35,
    fload_2 = 36,
    fload_3 = 37,
    dload_0 = 38,
    dload_1 = 39,
    dload_2 = 40,
    dload_3 = 41,
    aload_0 = 42,
    aload_1 = 43,
    aload_2 = 44,
    aload_3 = 45,
    iaload = 46,
    laload = 47,
    faload = 48,
    daload = 49,
    aaload = 50,
    baload = 51,
    caload = 52,
    saload = 53,
    [OpcodeArgsCount(1)] istore = 54,
    [OpcodeArgsCount(1)] lstore = 55,
    [OpcodeArgsCount(1)] fstore = 56,
    [OpcodeArgsCount(1)] dstore = 57,
    [OpcodeArgsCount(1)] astore = 58,
    istore_0 = 59,
    istore_1 = 60,
    istore_2 = 61,
    istore_3 = 62,
    lstore_0 = 63,
    lstore_1 = 64,
    lstore_2 = 65,
    lstore_3 = 66,
    fstore_0 = 67,
    fstore_1 = 68,
    fstore_2 = 69,
    fstore_3 = 70,
    dstore_0 = 71,
    dstore_1 = 72,
    dstore_2 = 73,
    dstore_3 = 74,
    astore_0 = 75,
    astore_1 = 76,
    astore_2 = 77,
    astore_3 = 78,
    iastore = 79,
    lastore = 80,
    fastore = 81,
    dastore = 82,
    aastore = 83,
    bastore = 84,
    castore = 85,
    sastore = 86,
    pop = 87,
    pop2 = 88,
    dup = 89,
    dup_x1 = 90,
    dup_x2 = 91,
    dup2 = 92,
    dup2_x1 = 93,
    dup2_x2 = 94,
    swap = 95,
    iadd = 96,
    ladd = 97,
    fadd = 98,
    dadd = 99,
    isub = 100,
    lsub = 101,
    fsub = 102,
    dsub = 103,
    imul = 104,
    lmul = 105,
    fmul = 106,
    dmul = 107,
    idiv = 108,
    ldiv = 109,
    fdiv = 110,
    ddiv = 111,
    irem = 112,
    lrem = 113,
    frem = 114,
    drem = 115,
    ineg = 116,
    lneg = 117,
    fneg = 118,
    dneg = 119,
    ishl = 120,
    lshl = 121,
    ishr = 122,
    lshr = 123,
    iushr = 124,
    lushr = 125,
    iand = 126,
    land = 127,
    ior = 128,
    lor = 129,
    ixor = 130,
    lxor = 131,
    [OpcodeArgsCount(2)] iinc = 132,
    i2l = 133,
    i2f = 134,
    i2d = 135,
    l2i = 136,
    l2f = 137,
    l2d = 138,
    f2i = 139,
    f2l = 140,
    f2d = 141,
    d2i = 142,
    d2l = 143,
    d2f = 144,
    i2b = 145,
    i2c = 146,
    i2s = 147,
    lcmp = 148,
    fcmpl = 149,
    fcmpg = 150,
    dcmpl = 151,
    dcmpg = 152,
    [OpcodeArgsCount(2)] ifeq = 153,
    [OpcodeArgsCount(2)] ifne = 154,
    [OpcodeArgsCount(2)] iflt = 155,
    [OpcodeArgsCount(2)] ifge = 156,
    [OpcodeArgsCount(2)] ifgt = 157,
    [OpcodeArgsCount(2)] ifle = 158,
    [OpcodeArgsCount(2)] if_icmpeq = 159,
    [OpcodeArgsCount(2)] if_icmpne = 160,
    [OpcodeArgsCount(2)] if_icmplt = 161,
    [OpcodeArgsCount(2)] if_icmpge = 162,
    [OpcodeArgsCount(2)] if_icmpgt = 163,
    [OpcodeArgsCount(2)] if_icmple = 164,
    [OpcodeArgsCount(2)] if_acmpeq = 165,
    [OpcodeArgsCount(2)] if_acmpne = 166,
    [OpcodeArgsCount(2)] @goto = 167,
    [OpcodeArgsCount(2)] jsr = 168,
    [OpcodeArgsCount(1)] ret = 169,
    [OpcodeArgsCount(-1)] tableswitch = 170,
    [OpcodeArgsCount(-1)] lookupswitch = 171,
    ireturn = 172,
    lreturn = 173,
    freturn = 174,
    dreturn = 175,
    areturn = 176,
    @return = 177,
    [OpcodeArgsCount(2)] getstatic = 178,
    [OpcodeArgsCount(2)] putstatic = 179,
    [OpcodeArgsCount(2)] getfield = 180,
    [OpcodeArgsCount(2)] putfield = 181,
    [OpcodeArgsCount(2)] invokevirtual = 182,
    [OpcodeArgsCount(2)] invokespecial = 183,
    [OpcodeArgsCount(2)] invokestatic = 184,
    [OpcodeArgsCount(4)] invokeinterface = 185,
    [OpcodeArgsCount(4)] invokedynamic = 186,
    [OpcodeArgsCount(2)] newobject = 187,
    [OpcodeArgsCount(1)] newarray = 188,
    [OpcodeArgsCount(2)] anewarray = 189,
    arraylength = 190,
    athrow = 191,
    [OpcodeArgsCount(2)] checkcast = 192,
    [OpcodeArgsCount(2)] instanceof = 193,
    monitorenter = 194,
    monitorexit = 195,
    [OpcodeArgsCount(-1)] wide = 196,
    [OpcodeArgsCount(3)] multianewarray = 197,
    [OpcodeArgsCount(2)] ifnull = 198,
    [OpcodeArgsCount(2)] ifnonnull = 199,
    [OpcodeArgsCount(4)] goto_w = 200,
    [OpcodeArgsCount(4)] jsr_w = 201,
    breakpoint = 202,
}