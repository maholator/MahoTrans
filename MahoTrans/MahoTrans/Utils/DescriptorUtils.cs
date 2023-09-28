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
}