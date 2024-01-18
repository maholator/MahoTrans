// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.io;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;
using IOException = java.io.IOException;
using MahoTrans.Builder;
using MahoTrans.Runtime.Types;
using MahoTrans;

namespace javax.microedition.io;
public class HttpConnectionImpl : Object, HttpConnection
{
    [JavaIgnore] private readonly HttpRequestMessage Request = new HttpRequestMessage();
    [JavaIgnore] private readonly HttpClient Client = new HttpClient();
    [JavaIgnore] private HttpResponseMessage Response = null!;

    private Reference InputStream;

    public bool Closed;
    private bool Destroyed;
    private bool RequestSent;
    private int InputState;

    [InitMethod]
    public new void Init()
    {
        InputStream = Reference.Null;
    }

    [JavaIgnore]
    public void Open(string url)
    {
        Request.RequestUri = new Uri(url);
    }

    // Sends http request
    [JavaIgnore]
    private void DoRequest()
    {
        if (RequestSent)
        {
            return;
        }
        RequestSent = true;
        try
        {
            // TODO find out how to send POST data
            //Request.Content = ReadOnlyMemoryContent();
            Response = Client.Send(Request);
        }
        catch (System.Exception e)
        {
            Jvm.Throw<IOException>(e.ToString());
        }
    }

    // Streams

    //[return: JavaType(nameof(java.io.InputStream))]
    [JavaDescriptor("()Ljava/io/InputStream;")]
    public Reference openInputStream()
    {
        CheckClosed();
        if (InputState != 0)
            Jvm.Throw<IOException>("Open already");
        DoRequest();
        InputState = 1;
        HttpInputStream i = Jvm.AllocateObject<HttpInputStream>();
        i.Stream = Response.Content.ReadAsStream();
        i.Connection = this;
        return InputStream = i.This;
    }

    [JavaDescriptor("()Ljava/io/OutputStream;")]
    public Reference openOutputStream()
    {
        CheckClosed();
        if (RequestSent)
            Jvm.Throw<IOException>("Request sent");
        // TODO
        Jvm.Throw<IOException>("Not implemented");
        return default;
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

    public void close()
    {
        if (Closed)
            return;
        Closed = true;
        InternalClose();
    }

    public int getResponseCode()
    {
        CheckClosed();
        DoRequest();
        return (int)Response.StatusCode;
    }

    [return: String]
    public Reference getResponseMessage()
    {
        CheckClosed();
        DoRequest();
        return Jvm.AllocateString(Response.ReasonPhrase!);
    }

    public void setRequestMethod([String] Reference method)
    {
        CheckClosed();
        if (RequestSent)
            Jvm.Throw<IOException>("Request sent");
        string s = Jvm.ResolveString(method);
        if (!s.Equals("GET") && !s.Equals("POST") && !s.Equals("HEAD"))
            Jvm.Throw<IOException>("Invalid method");
        Request.Method = new HttpMethod(s);
    }

    public void setRequestProperty([String] Reference field, [String] Reference value)
    {
        CheckClosed();
        if (RequestSent)
            Jvm.Throw<IOException>("Request sent");
        Request.Headers.Add(Jvm.ResolveString(field), Jvm.ResolveString(value));
    }

    public long getExpiration()
    {
        CheckClosed();
        DoRequest();
        try
        {
            return Response.Content.Headers.Expires!.Value.ToUnixTimeMilliseconds();
        }
        catch
        {
            return -1;
        }
    }

    public long getDate()
    {
        // TODO
        CheckClosed();
        DoRequest();
        try
        {
            //return Response.Content.Headers.Expires!.Value.ToUnixTimeMilliseconds();
            return 0;
        }
        catch
        {
            return -1;
        }
    }

    public long getLastModified()
    {
        CheckClosed();
        DoRequest();
        try
        {
            return Response.Content.Headers.LastModified!.Value.ToUnixTimeMilliseconds();
        }
        catch
        {
            return -1;
        }
    }

    [return: String]
    public Reference getHeaderField([String] Reference key)
    {
        CheckClosed();
        DoRequest();
        try
        {
            return Jvm.AllocateString(Response.Headers.GetValues(Jvm.ResolveString(key)).First());
        }
        catch
        {
            return Reference.Null;
        }
    }

    [return: String]
    public Reference getRequestMethod()
    {
        CheckClosed();
        return Jvm.AllocateString(Request.Method.ToString());
    }

    [return: String]
    public Reference getRequestProperty([String] Reference key)
    {
        CheckClosed();
        try
        {
            return Jvm.AllocateString(Request.Headers.GetValues(Jvm.ResolveString(key)).First());
        }
        catch
        {
            return Reference.Null;
        }
    }

    [return: String]
    public Reference getURL()
    {
        CheckClosed();
        return Jvm.AllocateString(Request.RequestUri!.OriginalString);
    }

    [return: String]
    public Reference getProtocol()
    {
        CheckClosed();
        return Jvm.AllocateString(Request.RequestUri!.Scheme);
    }

    [return: String]
    public Reference getHost()
    {
        CheckClosed();
        return Jvm.AllocateString(Request.RequestUri!.Host);
    }

    [return: String]
    public Reference getQuery()
    {
        CheckClosed();
        return Jvm.AllocateString(Request.RequestUri!.Query);
    }

    public int getPort()
    {
        CheckClosed();
        return Request.RequestUri!.Port;
    }

    // TODO methods

    [return: String]
    public Reference getFile()
    {
        throw new NotImplementedException();
    }

    [return: String]
    public Reference getRef()
    {
        throw new NotImplementedException();
    }

    [return: String]
    public Reference getHeaderField(int n)
    {
        throw new NotImplementedException();
    }

    [return: String]
    public Reference getHeaderFieldKey(int n)
    {
        throw new NotImplementedException();
    }

    public long getHeaderFieldDate([String] Reference name, long def)
    {
        throw new NotImplementedException();
    }

    public int getHeaderFieldInt([String] Reference name, int def)
    {
        throw new NotImplementedException();
    }

    // ContentConnection methods

    [return: String]
    public Reference getEncoding()
    {
        CheckClosed();
        DoRequest();
        try
        {
            return Jvm.AllocateString(Response.Content.Headers.ContentEncoding.First());
        }
        catch
        {
            return Reference.Null;
        }
    }

    public long getLength()
    {
        CheckClosed();
        DoRequest();
        long? l = Response.Content.Headers.ContentLength;
        if (l == null)
            return -1;
        return (long)l;
    }

    [return: String]
    public Reference getType()
    {
        CheckClosed();
        DoRequest();
        try
        {
            return Jvm.AllocateString(Response.Content.Headers.ContentType!.MediaType!);
        }
        catch
        {
            return Reference.Null;
        }
    }

    //

    private void InternalClose()
    {
        if (Destroyed)
            return;
        Destroyed = true;
        if (!InputStream.IsNull)
            Jvm.Resolve<HttpInputStream>(InputStream).close();
        Client.Dispose();
    }

    // on inputstream closed
    public void InputClosed()
    {
        if(InputState != 2)
        {
            InputState = 2;
            InternalClose();
        }
    }

    private void CheckClosed()
    {
        if (Closed)
            Jvm.Throw<IOException>("Closed");
    }

    public override bool OnObjectDelete()
    {
        if(InputState != 0 && InputState != 2)
        {
            // reading right now
            return true;
        }
        InternalClose();
        // these are needed for get methods, so keep them till garbage collected
        Request.Dispose();
        Response.Dispose();
        return false;
    }
}

public class HttpInputStream : InputStream
{
    [JavaIgnore] public Stream Stream = null!;
    [JavaIgnore] public HttpConnectionImpl Connection = null!;
    private bool Disposed;

    public new int read()
    {
        if (Connection.Closed)
        {
            close();
            Jvm.Throw<IOException>("closed");
        }
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

    public new void close()
    {
        if (Disposed)
            return;
        Disposed = true;
        Stream.Close();
        Stream.Dispose();
        Connection.InputClosed();
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

    public void skip(int n)
    {
        try
        {
            Stream.Position += n;
        }
        catch (System.Exception e)
        {
            Jvm.Throw<IOException>(e.ToString());
        }
    }

    public override bool OnObjectDelete()
    {
        close();
        return false;
    }
}
