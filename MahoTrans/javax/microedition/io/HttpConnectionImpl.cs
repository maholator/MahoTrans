// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.io;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Builder;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;
using IOException = java.io.IOException;
using MahoTrans.Utils;
using java.lang;

namespace javax.microedition.io;

public class HttpConnectionImpl : Object, HttpConnection
{
    [JavaIgnore] private readonly HttpRequestMessage Request = new HttpRequestMessage();
    [JavaIgnore] private readonly HttpClient Client = new HttpClient();
    [JavaIgnore] private HttpResponseMessage Response = null!;
    [JavaIgnore] private readonly Dictionary<string, string> ReqHeaders = new();

    private Reference InputStream;
    private Reference OutputStream;
    private Reference ByteOutputStream;

    public bool Closed;
    private bool Destroyed;
    private bool RequestSent;

    public int InputState;
    public int OutputState;

    [InitMethod]
    public new void Init()
    {
        InputStream = Reference.Null;
        OutputStream = Reference.Null;
        ByteOutputStream = Reference.Null;
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
            if (OutputState != 0)
            {
                ByteArrayOutputStream baos = Jvm.Resolve<ByteArrayOutputStream>(ByteOutputStream);
                sbyte[] buf = new sbyte[baos.count];
                System.Array.Copy(baos._buf, buf, baos.count);
                Request.Content = new ByteArrayContent(buf.ToUnsigned());
                if (GetRequestHeader("Content-Length") is null)
                    SetRequestHeader("Content-Length", "" + baos.count);
                if (GetRequestHeader("Content-Type") is null)
                    SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                Console.WriteLine("out set " + baos.count);
            }
            if (GetRequestHeader("User-Agent") is null)
                SetRequestHeader("User-Agent", "MahoTrans");
            foreach (string k in ReqHeaders.Keys)
            {
                try
                {
                    Request.Content!.Headers.Add(k, ReqHeaders[k]);
                }
                catch
                {
                    Request.Headers.Add(k, ReqHeaders[k]);
                }
            }
            // TODO set default User-Agent header
            Response = Client.Send(Request);
        }
        catch (System.Exception e)
        {
            Jvm.Throw<IOException>(e.ToString());
        }
    }

    // Streams

    [JavaDescriptor("()Ljava/io/InputStream;")]
    public Reference openInputStream()
    {
        CheckOpen();
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
        CheckOpen();
        if (RequestSent)
            Jvm.Throw<IOException>("Request sent");
        if (OutputState != 0)
            Jvm.Throw<IOException>("Open already");
        Request.Method = new HttpMethod("POST"); // force POST method

        OutputState = 1;

        HttpOutputStream o = Jvm.AllocateObject<HttpOutputStream>();
        ByteArrayOutputStream baos = Jvm.AllocateObject<ByteArrayOutputStream>();
        baos.Init();
        o.ByteStream = ByteOutputStream = baos.This;
        o.Connection = this;
        return OutputStream = o.This;
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
        CheckOpen();
        DoRequest();
        return (int)Response.StatusCode;
    }

    [return: String]
    public Reference getResponseMessage()
    {
        CheckOpen();
        DoRequest();
        return Jvm.AllocateString(Response.ReasonPhrase!);
    }

    public void setRequestMethod([String] Reference method)
    {
        CheckOpen();
        if (RequestSent)
            Jvm.Throw<IOException>("Request sent");
        if (OutputState != 0)
            return;
        string s = Jvm.ResolveString(method);
        if (!s.Equals("GET") && !s.Equals("POST") && !s.Equals("HEAD"))
            Jvm.Throw<IOException>("Invalid method");
        Request.Method = new HttpMethod(s);
    }

    public void setRequestProperty([String] Reference field, [String] Reference value)
    {
        CheckOpen();
        if (RequestSent)
            Jvm.Throw<IOException>("Request sent");
        if (OutputState != 0)
            return;
        ReqHeaders.Add(Jvm.ResolveString(field), Jvm.ResolveString(value));
        //Request.Content!.Headers.Add(Jvm.ResolveString(field), Jvm.ResolveString(value));
    }

    public long getExpiration()
    {
        CheckOpen();
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
        CheckOpen();
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
        CheckOpen();
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
        CheckOpen();
        DoRequest();
        string k = Jvm.ResolveString(key);
        try
        {
            return Jvm.AllocateString(Response.Content.Headers.GetValues(k).First());
        }
        catch
        {
            try
            {
                return Jvm.AllocateString(Response.Headers.GetValues(k).First());
            }
            catch
            {
            }
        }
        return Reference.Null;
    }

    [return: String]
    public Reference getRequestMethod()
    {
        CheckOpen();
        return Jvm.AllocateString(Request.Method.ToString());
    }

    [return: String]
    public Reference getRequestProperty([String] Reference key)
    {
        CheckOpen();
        try
        {
            string k = Jvm.ResolveString(key);
            if (!ReqHeaders.TryGetValue(k, out var v))
                if (!ReqHeaders.TryGetValue(k.ToLower(), out v))
                    return Reference.Null;
            return Jvm.AllocateString(v);
        }
        catch
        {
            return Reference.Null;
        }
    }

    [return: String]
    public Reference getURL()
    {
        CheckOpen();
        return Jvm.AllocateString(Request.RequestUri!.OriginalString);
    }

    [return: String]
    public Reference getProtocol()
    {
        CheckOpen();
        return Jvm.AllocateString(Request.RequestUri!.Scheme);
    }

    [return: String]
    public Reference getHost()
    {
        CheckOpen();
        return Jvm.AllocateString(Request.RequestUri!.Host);
    }

    [return: String]
    public Reference getQuery()
    {
        CheckOpen();
        return Jvm.AllocateString(Request.RequestUri!.Query);
    }

    public int getPort()
    {
        CheckOpen();
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
        CheckOpen();
        try
        {
            return Integer.parseInt(getHeaderField(name));
        }
        catch
        {
            return def;
        }
    }

    // ContentConnection methods

    [return: String]
    public Reference getEncoding()
    {
        CheckOpen();
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
        CheckOpen();
        DoRequest();
        long? l = Response.Content.Headers.ContentLength;
        if (l == null)
            return -1;
        return (long)l;
    }

    [return: String]
    public Reference getType()
    {
        CheckOpen();
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
        if (!OutputStream.IsNull)
            Jvm.Resolve<HttpOutputStream>(OutputStream).close();
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
    public void OutputClosed()
    {
        if (OutputState != 2)
            OutputState = 2;
    }

    private void CheckOpen()
    {
        if (Closed)
            Jvm.Throw<IOException>("Closed");
    }

    [JavaIgnore]
    private string? GetRequestHeader(string key)
    {
        if (!ReqHeaders.TryGetValue(key, out var v))
            if (!ReqHeaders.TryGetValue(key.ToLower(), out v))
                return null;
        return v;
    }

    [JavaIgnore]
    private void SetRequestHeader(string key, string value)
    {
        if (ReqHeaders.ContainsKey(key.ToLower()))
            ReqHeaders.Add(key.ToLower(), value);
        else
            ReqHeaders.Add(key, value);
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

    public int read([JavaType("[B")] Reference buf)
    {
        return read(buf, 0, Jvm.ResolveArray<sbyte>(buf).Length);
    }

    public int read([JavaType("[B")] Reference buf, int offset, int length)
    {
        if (Connection.Closed)
        {
            close();
            Jvm.Throw<IOException>("closed");
        }
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

public class HttpOutputStream : OutputStream
{
    [JavaIgnore] public HttpConnectionImpl Connection = null!;
    public Reference ByteStream;
    private bool Disposed;

    public new void write(int value)
    {
        if (Connection.OutputState == 2)
            Jvm.Throw<IOException>("closed");
        Jvm.Resolve<ByteArrayOutputStream>(ByteStream).write(value);
    }

    public void write([JavaType("[B")] Reference buf, int offset, int length)
    {
        if (Connection.OutputState == 2)
            Jvm.Throw<IOException>("closed");
        Jvm.Resolve<ByteArrayOutputStream>(ByteStream).write(buf, offset, length);
    }

    public void write([JavaType("[B")] Reference buf)
    {
        if (Connection.OutputState == 2)
            Jvm.Throw<IOException>("closed");
        Jvm.Resolve<ByteArrayOutputStream>(ByteStream).write(buf, 0, Jvm.ResolveArray<sbyte>(buf).Length);
    }

    public new void flush()
    {
    }

    public new void close()
    {
        if (Disposed)
            return;
        Disposed = true;
        Connection.OutputClosed();
        //Jvm.Resolve<ByteArrayOutputStream>(ByteStream).close();
    }

    public override bool OnObjectDelete()
    {
        close();
        return false;
    }
}
