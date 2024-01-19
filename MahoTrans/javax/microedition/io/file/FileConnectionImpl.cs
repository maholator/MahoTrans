// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.io;
using java.lang;
using java.util;
using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Object = java.lang.Object;
using IOException = java.io.IOException;

namespace javax.microedition.io.file;

public class FileConnectionImpl : Object, FileConnection
{
    [JavaIgnore] private string Url = null!;
    [JavaIgnore] private string SystemUrl = null!;
    [JavaIgnore] private FileStream Stream = null!;

    private Reference InputStream;
    private Reference OutputStream;

    private bool Closed;
    private bool IsDirectoryPath;

    [InitMethod]
    public new void Init()
    {
        InputStream = Reference.Null;
        OutputStream = Reference.Null;
    }

    [JavaIgnore]
    public void Open(string url)
    {
        Url = url;
        SystemUrl = url.Replace("file:///", "");
        IsDirectoryPath = Path.EndsInDirectorySeparator(SystemUrl);
    }

    public long availableSize()
    {
        CheckOpen();
        throw new NotImplementedException();
    }

    public bool canRead()
    {
        CheckOpen();
        return true;
    }

    public bool canWrite()
    {
        CheckOpen();
        return true;
    }

    public void create()
    {
        CheckOpen();
        if (exists() || isDirectory())
            Jvm.Throw<IOException>();
        try
        {
            File.Create(SystemUrl).Dispose();
        }
        catch
        {
            Jvm.Throw<IOException>();
        }
    }

    public void delete()
    {
        CheckOpen();
        if (!exists() || isDirectory())
            Jvm.Throw<IOException>();
        try
        {
            File.Delete(SystemUrl);
        }
        catch
        {
            Jvm.Throw<IOException>();
        }
    }

    public long directorySize(bool includeSubDirs)
    {
        CheckOpen();
        if (!isDirectory())
            Jvm.Throw<IOException>();
        throw new NotImplementedException();
    }

    public bool exists()
    {
        CheckOpen();
        if(isDirectory())
            return Directory.Exists(SystemUrl);
        return File.Exists(SystemUrl);
    }

    public long fileSize()
    {
        CheckOpen();
        if (!exists() || isDirectory())
            Jvm.Throw<IOException>();
        throw new NotImplementedException();
    }


    [return: String]
    public Reference getName()
    {
        CheckOpen();
        if (isDirectory())
            return Jvm.AllocateString(Path.GetDirectoryName(SystemUrl)!);
        return Jvm.AllocateString(Path.GetFileName(SystemUrl));
    }

    [return: String]
    public Reference getPath()
    {
        CheckOpen();
        throw new NotImplementedException();
    }

    [return: String]
    public Reference getURL()
    {
        CheckOpen();
        return Jvm.AllocateString(Url);
    }

    public bool isDirectory()
    {
        CheckOpen();
        return IsDirectoryPath || (!File.Exists(SystemUrl) && Directory.Exists(SystemUrl));
    }

    public bool isHidden()
    {
        CheckOpen();
        throw new NotImplementedException();
    }

    public bool isOpen()
    {
        return !Closed;
    }

    public long lastModified()
    {
        CheckOpen();
        throw new NotImplementedException();
    }

    [return: JavaType(typeof(Enumeration))]
    public Reference list()
    {
        CheckOpen();
        if (!Directory.Exists(SystemUrl))
            Jvm.Throw<IOException>();
        try
        {
            List<string> list = new();
            foreach (string s in Directory.EnumerateFileSystemEntries(SystemUrl))
                list.Add(s);
            Reference[] r = new Reference[list.Count];
            for (int i = 0; i < r.Length; i++)
                r[i] = Jvm.AllocateString(list[i]);
            var enumerator = Jvm.AllocateObject<ArrayEnumerator>();
            enumerator.Value = r;
            enumerator.Init();
            return enumerator.This;
        }
        catch
        {
            Jvm.Throw<IOException>();
            return default;
        }
    }

    [return: JavaType(typeof(Enumeration))]
    public Reference list([String] Reference filter, bool includeHidden)
    {
        CheckOpen();
        if (!Directory.Exists(SystemUrl))
            Jvm.Throw<IOException>();
        throw new NotImplementedException();
    }

    public void mkdir()
    {
        CheckOpen();
        if (Directory.Exists(SystemUrl))
            Jvm.Throw<IOException>();
        Directory.CreateDirectory(SystemUrl);
    }

    [return: JavaType(typeof(InputStream))]
    public Reference openInputStream()
    {
        CheckOpen();
        if (!exists() || isDirectory())
            Jvm.Throw<IOException>();
        if (!InputStream.IsNull || !OutputStream.IsNull)
            Jvm.Throw<IOException>("Invalid state");
        FileInputStream i = Jvm.AllocateObject<FileInputStream>();
        i.Stream = File.OpenRead(SystemUrl);
        i.Connection = this;
        return InputStream = i.This;
    }

    [return: JavaType(typeof(OutputStream))]
    public Reference openOutputStream()
    {
        CheckOpen();
        if (isDirectory())
            Jvm.Throw<IOException>();
        if (!InputStream.IsNull || !OutputStream.IsNull)
            Jvm.Throw<IOException>("Invalid state");
        FileOutputStream o = Jvm.AllocateObject<FileOutputStream>();
        o.Stream = Stream = File.OpenWrite(SystemUrl);
        o.Connection = this;
        return OutputStream = o.This;
    }

    [return: JavaType(typeof(OutputStream))]
    public Reference openOutputStream(long byteOffset)
    {
        CheckOpen();
        if (isDirectory())
            Jvm.Throw<IOException>();
        throw new NotImplementedException();
    }

    [JavaDescriptor("()Ljava/io/DataInputStream;")]
    public JavaMethodBody openDataInputStream(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendNewObject<DataInputStream>();
        b.Append(JavaOpcode.dup);
        b.AppendThis();
        b.AppendVirtcall("openInputStream", "()Ljava/io/InputStream;");
        b.AppendVirtcall("<init>", "(Ljava/io/InputStream;)V");
        b.AppendReturnReference();
        return b.Build(2, 1);
    }

    [JavaDescriptor("()Ljava/io/DataOutputStream;")]
    public JavaMethodBody openDataOutputStream(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendNewObject<DataInputStream>();
        b.Append(JavaOpcode.dup);
        b.AppendThis();
        b.AppendVirtcall("openOutputStream", "()Ljava/ioOutputStream;");
        b.AppendVirtcall("<init>", "(Ljava/io/OutputStream;)V");
        b.AppendReturnReference();
        return b.Build(2, 1);
    }

    public void rename([String] Reference newName)
    {
        CheckOpen();

        throw new NotImplementedException();
    }

    public void setFileConnection([String] Reference fileName)
    {
        CheckOpen();
        throw new NotImplementedException();
    }

    public void setHidden(bool hidden)
    {
        CheckOpen();
        if (!exists())
            Jvm.Throw<IOException>();
        throw new NotImplementedException();
    }

    public void setReadable(bool readable)
    {
    }

    public void setWritable(bool writable)
    {
    }

    public long totalSize()
    {
        CheckOpen();
        throw new NotImplementedException();
    }

    public void truncate(long byteOffset)
    {
        CheckOpen();
        throw new NotImplementedException();
    }

    public long usedSize()
    {
        CheckOpen();
        throw new NotImplementedException();
    }

    public void close()
    {
        Closed = true;
    }

    private void CheckOpen()
    {
        if (Closed)
            Jvm.Throw<IOException>("Closed");
    }

    private bool IsFile()
    {
        return !IsDirectoryPath && (!Directory.Exists(SystemUrl) || File.Exists(SystemUrl));
    }

    public void InputClosed()
    {
        InputStream = Reference.Null;
    }

    public void OutputClosed()
    {
        OutputStream = Reference.Null;
    }
}

public class FileInputStream : InputStream
{
    [JavaIgnore] public Stream Stream = null!;
    [JavaIgnore] public FileConnectionImpl Connection = null!;
    private bool Disposed;

    public new void close()
    {
        if (Disposed)
            return;
        Disposed = true;
        Stream.Dispose();
        Connection.InputClosed();
    }
    public new int read()
    {
        if (Disposed)
            Jvm.Throw<IOException>("closed");
        try
        {
            return Stream.ReadByte();
        }
        catch (System.Exception e)
        {
            Jvm.Throw<IOException>(e.ToString());
        }
        return default;
    }

    public int read([JavaType("[B")] Reference buf)
    {
        return read(buf, 0, Jvm.ResolveArray<sbyte>(buf).Length);
    }

    public int read([JavaType("[B")] Reference buf, int offset, int length)
    {
        if (Disposed)
            Jvm.Throw<IOException>("closed");
        sbyte[] b = Jvm.ResolveArray<sbyte>(buf);
        try
        {
            int len = Stream.Read(b.ToUnsigned(), 0, length);
            if (len == 0)
                return -1;
            return len;
        }
        catch (System.Exception e)
        {
            Jvm.Throw<IOException>(e.ToString());
        }
        return default;
    }

    public new int available()
    {
        try
        {
            return (int)Stream.Length;
        }
        catch (System.Exception e)
        {
            Jvm.Throw<IOException>(e.ToString());
        }
        return default;
    }

    public long skip(long n)
    {
        if (n < 0) return 0;
        try
        {
            if (Stream.Position + n >= Stream.Length)
            {
                Stream.Position = Stream.Length;
                return Stream.Length - Stream.Position;
            }
            Stream.Position += n;
        }
        catch (System.Exception e)
        {
            Jvm.Throw<IOException>(e.ToString());
        }
        return n;
    }

    public override bool OnObjectDelete()
    {
        close();
        return false;
    }
}


public class FileOutputStream : OutputStream
{
    [JavaIgnore] public Stream Stream = null!;
    [JavaIgnore] public FileConnectionImpl Connection = null!;
    private bool Disposed;

    public new void write(int value)
    {
        if (Disposed)
            Jvm.Throw<IOException>("closed");
        Stream.WriteByte((byte)(uint)value);
    }

    public void write([JavaType("[B")] Reference buf, int offset, int length)
    {
        if (Disposed)
            Jvm.Throw<IOException>("closed");
        sbyte[] b = Jvm.ResolveArray<sbyte>(buf);
        Stream.Write(b.ToUnsigned(), offset, length);
    }

    public void write([JavaType("[B")] Reference buf)
    {
        write(buf, 0, Jvm.ResolveArray<sbyte>(buf).Length);
    }

    public new void flush()
    {
        Stream.Flush();
    }

    public new void close()
    {
        if (Disposed)
            return;
        Disposed = true;
        Stream.Dispose();
        Connection.OutputClosed();
    }

    public override bool OnObjectDelete()
    {
        close();
        return false;
    }
}

