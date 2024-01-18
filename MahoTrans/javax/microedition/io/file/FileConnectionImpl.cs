// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.io;
using java.lang;
using java.util;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;
using IOException = java.io.IOException;

namespace javax.microedition.io.file;

public class FileConnectionImpl : Object, FileConnection
{
    [JavaIgnore] private string Url = null!;
    [JavaIgnore] private string SystemUrl = null!;
    [JavaIgnore] private FileStream Stream = null!;
    private bool Closed;
    private bool IsDirectoryPath;

    [InitMethod]
    public new void Init()
    {
        base.Init();
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
            Stream = File.Create(SystemUrl);
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
        throw new NotImplementedException();
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

    [return: JavaType(typeof(DataInputStream))]
    public Reference openDataInputStream()
    {
        CheckOpen();
        if(!exists() || isDirectory())
            Jvm.Throw<IOException>();
        throw new NotImplementedException();
    }

    [return: JavaType(typeof(DataOutputStream))]
    public Reference openDataOutputStream()
    {
        CheckOpen();
        if (isDirectory())
            Jvm.Throw<IOException>();
        throw new NotImplementedException();
    }

    [return: JavaType(typeof(InputStream))]
    public Reference openInputStream()
    {
        CheckOpen();
        if (!exists() || isDirectory())
            Jvm.Throw<IOException>();
        throw new NotImplementedException();
    }

    [return: JavaType(typeof(OutputStream))]
    public Reference openOutputStream()
    {
        CheckOpen();
        if (isDirectory())
            Jvm.Throw<IOException>();
        throw new NotImplementedException();
    }

    [return: JavaType(typeof(OutputStream))]
    public Reference openOutputStream___offset(long byteOffset)
    {
        CheckOpen();
        if (isDirectory())
            Jvm.Throw<IOException>();
        throw new NotImplementedException();
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

}
