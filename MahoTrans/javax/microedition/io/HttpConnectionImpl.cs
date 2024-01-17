// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.io;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;
using IOException = java.io.IOException;

namespace javax.microedition.io;
public class HttpConnectionImpl : Object, HttpConnection
{
    [JavaIgnore] private readonly HttpRequestMessage Request = new HttpRequestMessage();
    [JavaIgnore] private readonly HttpClient Client = new HttpClient();
    [JavaIgnore] private HttpResponseMessage Response;
    private Reference InputStream;
    private Reference OutputStream;
    public bool Closed;
    private bool RequestSent;
    private int InputState;

    [InitMethod]
    public new void Init()
    {
        InputStream = Reference.Null;
        OutputStream = Reference.Null;
    }

    [JavaIgnore]
    public void Open(string url)
    {
        Request.RequestUri = new Uri(url);
    }

    [JavaIgnore]
    public void DoRequest()
    {
        if (RequestSent)
        {
            return;
        }
        RequestSent = true;
        try
        {
            Response = Client.Send(Request);
        }
        catch (System.Exception e)
        {
            Jvm.Throw<IOException>(e.ToString());
        }
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

    public long getLength()
    {
        CheckClosed();
        DoRequest();
        long? l = Response.Content.Headers.ContentLength;
        if (l == null)
            return -1;
        return (long)l;
    }

    public void InternalClose()
    {
        if (Closed)
        {
            return;
        }
        Closed = true;
        if (!InputStream.IsNull)
        {
            Jvm.Resolve<HttpInputStream>(InputStream).close();
        }
        Request.Dispose();
        Response.Dispose();
        Client.Dispose();
    }

    public void close()
    {
        InternalClose();
    }

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

    [return: String]
    public Reference getRequestMethod()
    {
        return Jvm.AllocateString(Request.Method.ToString());
    }

    public override bool OnObjectDelete()
    {
        if(InputState != 0 && InputState != 2)
        {
            // reading right now
            return true;
        }
        InternalClose();
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
