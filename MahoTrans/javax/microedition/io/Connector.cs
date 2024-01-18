// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.io;
using java.lang;
using MahoTrans.Builder;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;
using String = java.lang.String;
using javax.microedition.io.file;

namespace javax.microedition.io;

public class Connector : Object
{
    public const int READ = 1;
    public const int WRITE = 2;
    public const int READ_WRITE = 3;

    [return: JavaType(typeof(Connection))]
    public static Reference open([String] Reference name)
    {
        return open(name, READ_WRITE, false);
    }

    [return: JavaType(typeof(Connection))]
    public static Reference open([String] Reference name, int mode)
    {
        return open(name, mode, false);
    }

    [return: JavaType(typeof(Connection))]
    public static Reference open([String] Reference name, int mode, bool timeout)
    {
        if (name.IsNull)
            Jvm.Throw<NullPointerException>();
        string s = Jvm.ResolveString(name);
        if (!s.Contains(':'))
            Jvm.Throw<ConnectionNotFoundException>();
        string protocol = s[0..s.IndexOf(':')];
        if (s.Contains(';')) // clear params
            s = s[..s.IndexOf(';')];
        switch (protocol)
        {
            case "http":
            case "https":
                HttpConnectionImpl http = Jvm.AllocateObject<HttpConnectionImpl>();
                http.Init();
                http.Open(s);
                return http.This;
            case "file":
                FileConnectionImpl file = Jvm.AllocateObject<FileConnectionImpl>();
                file.Init();
                file.Open(s);
                return file.This;
        }
        Jvm.Throw<ConnectionNotFoundException>();
        return default;
    }

    [JavaDescriptor("(Ljava/lang/String;)Ljava/io/DataInputStream;")]
    public static JavaMethodBody openDataInputStream(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.iconst_2);
        b.AppendStaticCall<Connector>(nameof(open), typeof(Connection), typeof(String), typeof(int));
        b.AppendVirtcall("openDataInputStream", typeof(DataInputStream));
        b.AppendReturnReference();
        return b.Build(2, 1);
    }

    [JavaDescriptor("(Ljava/lang/String;)Ljava/io/DataOutputStream;")]
    public static JavaMethodBody openDataOutputStream(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.iconst_2);
        b.AppendStaticCall<Connector>(nameof(open), typeof(Connection), typeof(String), typeof(int));
        b.AppendVirtcall("openDataOutputStream", typeof(DataOutputStream));
        b.AppendReturnReference();
        return b.Build(2, 1);
    }

    [JavaDescriptor("(Ljava/lang/String;)Ljava/io/InputStream;")]
    public static JavaMethodBody openInputStream(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendStaticCall<Connector>("openDataInputStream", typeof(DataInputStream), typeof(String));
        b.AppendReturnReference();
        return b.Build(2, 1);
    }

    [JavaDescriptor("(Ljava/lang/String;)Ljava/io/OutputStream;")]
    public static JavaMethodBody openOutputStream(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendStaticCall<Connector>("openDataOutputStream", typeof(DataOutputStream), typeof(String));
        b.AppendReturnReference();
        return b.Build(2, 1);
    }
}