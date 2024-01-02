// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using MahoTrans.Runtime;

namespace MahoTrans.Utils;

public static class DescriptorUtils
{
    public readonly struct ArrayOf
    {
        public readonly object Type;
        public readonly int DimensionsCount;

        public ArrayOf(object type, int dimensionsCount)
        {
            Type = type;
            DimensionsCount = dimensionsCount;
        }

        public ArrayOf Wrap()
        {
            return new ArrayOf(Type, DimensionsCount + 1);
        }
    }

    public static object ParseDescriptor(string descriptor)
    {
        return ParseDescriptor(descriptor, 0, out _);
    }

    public static object ParseDescriptor(string descriptor, int from, out int len)
    {
        switch (descriptor[from])
        {
            case 'B':
                len = 1;
                return typeof(sbyte);
            case 'C':
                len = 1;
                return typeof(char);
            case 'D':
                len = 1;
                return typeof(double);
            case 'F':
                len = 1;
                return typeof(float);
            case 'I':
                len = 1;
                return typeof(int);
            case 'J':
                len = 1;
                return typeof(long);
            case 'S':
                len = 1;
                return typeof(short);
            case 'Z':
                len = 1;
                return typeof(bool);
            case '[':
            {
                var t = ParseDescriptor(descriptor, from + 1, out var l2);
                len = l2 + 1;
                if (t is ArrayOf a)
                    return a.Wrap();
                return new ArrayOf(t, 1);
            }
            case 'L':
            {
                int e = descriptor.IndexOf(';', from);
                var tn = descriptor.Substring(from + 1, e - from - 1);
                len = e - from + 1;
                return tn;
            }
            default:
                throw new Exception();
        }
    }

    public static PrimitiveType ParseDescriptor(char c)
    {
        switch (c)
        {
            case 'B':
            case 'C':
            case 'S':
            case 'Z':
            case 'I':
                return PrimitiveType.Int;
            case 'J':
                return PrimitiveType.Long;
            case 'F':
                return PrimitiveType.Float;
            case 'D':
                return PrimitiveType.Double;
            default:
                return PrimitiveType.Reference;
        }
    }

    private static PrimitiveType ParseDescriptorAsPrimitive(string descriptor, int from, out int len)
    {
        switch (descriptor[from])
        {
            case '[':
            {
                ParseDescriptorAsPrimitive(descriptor, from + 1, out var l2);
                len = l2 + 1;
                return PrimitiveType.Reference;
            }
            case 'L':
            {
                len = descriptor.IndexOf(';', from) - from + 1;
                return PrimitiveType.Reference;
            }
            default:
                len = 1;
                return ParseDescriptor(descriptor[from]);
        }
    }

    public static PrimitiveType? GetMethodReturnType(string descriptor)
    {
        var cb = descriptor.IndexOf(')');
        var c = descriptor[cb + 1];
        if (c == 'V')
            return null;
        return ParseDescriptor(c);
    }

    public static (PrimitiveType? returnType, PrimitiveType[] args) ParseMethodDescriptorAsPrimitives(string descriptor)
    {
        if (descriptor[0] != '(')
            throw new ArgumentException();
        int argsEnd = descriptor.IndexOf(')');
        string argsD = descriptor.Substring(1, argsEnd - 1);
        var retD = descriptor[argsEnd + 1];
        var retPrim = retD == 'V' ? default(PrimitiveType?) : ParseDescriptor(retD);

        if (argsD.Length == 0)
            return (retPrim, Array.Empty<PrimitiveType>());

        int next = 0;
        List<PrimitiveType> t = new();
        while (true)
        {
            t.Add(ParseDescriptorAsPrimitive(argsD, next, out int len));
            next += len;
            if (next >= argsD.Length)
                break;
        }

        return (retPrim, t.ToArray());
    }

    public static (object returnType, object[] args) ParseMethodDescriptor(string descriptor)
    {
        if (descriptor[0] != '(')
            throw new ArgumentException();
        int argsEnd = descriptor.IndexOf(')');
        string argsD = descriptor.Substring(1, argsEnd - 1);
        string retD = descriptor.Substring(argsEnd + 1);
        object ret = retD.Equals("V") ? typeof(void) : ParseDescriptor(retD, 0, out _);

        if (argsD.Length == 0)
            return (ret, Array.Empty<object>());

        int next = 0;
        List<object> t = new();
        while (true)
        {
            t.Add(ParseDescriptor(argsD, next, out int len));
            next += len;
            if (next >= argsD.Length)
                break;
        }

        return (ret, t.ToArray());
    }

    public static int ParseMethodArgsCount(string descriptor)
    {
        if (descriptor[0] != '(')
            throw new ArgumentException();
        int argsEnd = descriptor.IndexOf(')');
        string argsD = descriptor.Substring(1, argsEnd - 1);

        if (argsD.Length == 0)
            return 0;

        var next = 0;
        var count = 0;
        while (true)
        {
            int len;
            switch (argsD[next])
            {
                case 'B':
                case 'C':
                case 'D':
                case 'F':
                case 'I':
                case 'J':
                case 'S':
                case 'Z':
                    len = 1;
                    break;
                case '[':
                {
                    ParseDescriptor(argsD, next + 1, out var l2);
                    len = l2 + 1;
                    break;
                }
                case 'L':
                {
                    len = argsD.IndexOf(';', next) - next + 1;
                    break;
                }
                default:
                    throw new ArgumentException();
            }

            count++;
            next += len;
            if (next >= argsD.Length)
                break;
        }

        return count;
    }

    public static bool IsTypeInt32OnStack(this Type t)
    {
        return t == typeof(int) || t == typeof(char) || t == typeof(short) || t == typeof(sbyte);
    }

    /// <summary>
    ///     Gets full type name in java style.
    /// </summary>
    /// <param name="t">Type to get name from.</param>
    /// <returns>Name where dots are replaced with slashes.</returns>
    public static string ToJavaName(this Type t) => t.FullName!.Replace('.', '/');

    /// <summary>
    ///     Gets full type name as descriptor, i.e. Lpkg/obj;.
    ///     If you want to convert native primitives like bool->Z, use <see cref="ToJavaDescriptorNative" /> instead.
    /// </summary>
    /// <param name="t">Type to get name from.</param>
    /// <returns>Name with dots replaced by slashes int L; form.</returns>
    public static string ToJavaDescriptor(this Type t) => $"L{t.ToJavaName()};";

    /// <summary>
    ///     Gets full type name as descriptor, i.e. Lpkg/obj;.
    ///     If the type if a native primitive, handles it correctly, i.e. bool->Z, void->V and so on.
    ///     If the type is guaranteed to be a java class, use <see cref="ToJavaDescriptor" /> directly.
    /// </summary>
    /// <param name="t">Type to get name from.</param>
    /// <returns>Type descriptor.</returns>
    public static string ToJavaDescriptorNative(this Type t)
    {
        if (t == typeof(Reference))
            return "Ljava/lang/Object;";
        if (t == typeof(int))
            return "I";
        if (t == typeof(long))
            return "J";
        if (t == typeof(float))
            return "F";
        if (t == typeof(double))
            return "D";
        if (t == typeof(char))
            return "C";
        if (t == typeof(short))
            return "S";
        if (t == typeof(sbyte))
            return "B";
        if (t == typeof(bool))
            return "Z";
        if (t == typeof(string))
            return "Ljava/lang/String;";
        if (t == typeof(void))
            return "V";
        return t.ToJavaDescriptor();
    }

    public static string BuildMethodDescriptor(Type returnType, params Type[] args)
    {
        var sb = new StringBuilder("(");
        foreach (var type in args)
        {
            sb.Append(type.ToJavaDescriptorNative());
        }

        sb.Append(')');
        sb.Append(returnType.ToJavaDescriptorNative());
        return sb.ToString();
    }
}