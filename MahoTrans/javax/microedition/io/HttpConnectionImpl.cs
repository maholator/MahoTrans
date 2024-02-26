// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.io;
using java.lang;
using MahoTrans;
using MahoTrans.Utils;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Builder;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;
using IOException = java.io.IOException;
using Thread = System.Threading.Thread;
using String = java.lang.String;

namespace javax.microedition.io;

public class HttpConnectionImpl : Object, HttpConnection
{
    [JavaIgnore] private readonly HttpRequestMessage Request = new HttpRequestMessage();
    [JavaIgnore] private readonly HttpClient Client = new HttpClient();
    [JavaIgnore] private HttpResponseMessage Response = null!;

    [JavaIgnore] private readonly List<string> ReqHeaderKeys = new();
    [JavaIgnore] private readonly Dictionary<string, string> ReqHeadersTable = new();

    [JavaIgnore] private readonly List<string> ResHeaders = new();
    [JavaIgnore] private readonly Dictionary<string, string> ResHeadersTable = new();

    private Reference InputStream;
    private Reference OutputStream;
    private Reference ByteOutputStream;

    public bool Closed;
    public bool Destroyed;
    public bool RequestSent;

    public int InputState;
    public int OutputState;

    public Reference RequestLock;
    [JavaType(typeof(IOException))] public Reference RequestException;

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
    private void SendRequest(JvmState Jvm)
    {
        try
        {
            if (OutputState != 0)
            {
                ByteArrayOutputStream baos = Jvm.Resolve<ByteArrayOutputStream>(ByteOutputStream);
                Request.Content = new ByteArrayContent(baos._buf.ToUnsigned(), 0, baos.count);
                if (GetRequestHeader("Content-Length") is null)
                    SetRequestHeader("Content-Length", "" + baos.count);
                if (GetRequestHeader("Content-Type") is null)
                    SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            }
            if (GetRequestHeader("User-Agent") is null)
                SetRequestHeader("User-Agent", "MahoTrans");
            foreach (string k in ReqHeaderKeys)
            {
                string v = ReqHeadersTable[k.ToLower()];
                if (Request.Content is null)
                {
                    Request.Headers.Add(k, v);
                    continue;
                }
                try
                {
                    Request.Content!.Headers.Add(k, v);
                }
                catch
                {
                    Request.Headers.Add(k, v);
                }
            }
            Response = Client.Send(Request);

            foreach (KeyValuePair<string, IEnumerable<string>> h in Response.Content.Headers.AsEnumerable())
                ParseHeaders(h.Key, h.Value);
            foreach (KeyValuePair<string, IEnumerable<string>> h in Response.Headers.AsEnumerable())
                ParseHeaders(h.Key, h.Value);
        }
        catch (System.Exception e)
        {
            IOException ioe = Jvm.AllocateObject<IOException>();
            ioe.Init(Jvm.AllocateString(e.ToString()));
            RequestException = ioe.This;
        }
        // XXX
        var rl = Jvm.ResolveObject(RequestLock);
        if (rl.Waiters == null || rl.Waiters.Count == 0)
            return;
        var mw = rl.Waiters[^1];
        rl.Waiters.RemoveAt(rl.Waiters.Count - 1);

        Jvm.Attach(mw.MonitorOwner);
    }

    public void DoRequestAsync()
    {
        JvmState jvm = Jvm;
        Thread thread = new Thread(() => SendRequest(jvm));
        thread.Start();
    }

    [JavaIgnore]
    private void ParseHeaders(string key, IEnumerable<string> values)
    {
        foreach (string v in values)
        {
            ResHeaders.Add(key);
            ResHeaders.Add(v);
        }
        ResHeadersTable.Add(key.ToLower(), values.First());
    }

    // Streams

    [return: JavaType(typeof(InputStream))]
    public Reference OpenInputStreamInternal()
    {
        InputState = 1;
        HttpInputStream i = Jvm.AllocateObject<HttpInputStream>();
        i.Stream = Response.Content.ReadAsStream();
        i.Connection = this;
        return InputStream = i.This;
    }

    [return: JavaType(typeof(OutputStream))]
    public Reference OpenOutputStreamInternal()
    {
        Request.Method = new HttpMethod("POST"); // force POST method

        OutputState = 1;

        HttpOutputStream o = Jvm.AllocateObject<HttpOutputStream>();
        ByteArrayOutputStream baos = Jvm.AllocateObject<ByteArrayOutputStream>();
        baos.Init();
        o.ByteStream = ByteOutputStream = baos.This;
        o.Connection = this;
        return OutputStream = o.This;
    }

    public void close()
    {
        if (Closed)
            return;
        Closed = true;
        InternalClose();
    }

    public int GetResponseCodeInternal()
    {
        return (int)Response.StatusCode;
    }

    [return: String]
    public Reference GetResponseMessageInternal()
    {
        return Jvm.AllocateString(Response.ReasonPhrase!);
    }

    public void SetRequestMethodInternal([String] Reference method)
    {
        string s = Jvm.ResolveString(method);
        if (!s.Equals("GET") && !s.Equals("POST") && !s.Equals("HEAD"))
            Jvm.Throw<IOException>("Invalid method");
        Request.Method = new HttpMethod(s);
    }

    public void SetRequestPropertyInternal([String] Reference field, [String] Reference value)
    {
        SetRequestHeader(Jvm.ResolveString(field), Jvm.ResolveString(value));
    }

    public long GetExpirationInternal()
    {
        try
        {
            return Response.Content.Headers.Expires!.Value.ToUnixTimeMilliseconds();
        }
        catch
        {
            return 0;
        }
    }

    public long GetDateInternal()
    {
        try
        {
            return Response.Headers.Date!.Value.ToUnixTimeMilliseconds();
        }
        catch
        {
            return 0;
        }
    }

    public long GetLastModifiedInternal()
    {
        try
        {
            return Response.Content.Headers.LastModified!.Value.ToUnixTimeMilliseconds();
        }
        catch
        {
            return 0;
        }
    }

    [return: String]
    public Reference getRequestMethod()
    {
        return Jvm.AllocateString(Request.Method.ToString());
    }

    [return: String]
    public Reference getRequestProperty([String] Reference key)
    {
        try
        {
            var v = GetRequestHeader(Jvm.ResolveString(key));
            if (v is null)
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
        return Jvm.AllocateString(Request.RequestUri!.OriginalString);
    }

    [return: String]
    public Reference getProtocol()
    {
        return Jvm.AllocateString(Request.RequestUri!.Scheme);
    }

    [return: String]
    public Reference getHost()
    {
        return Jvm.AllocateString(Request.RequestUri!.Host);
    }

    public int getPort()
    {
        return Request.RequestUri!.Port;
    }

    [return: String]
    public Reference getFile()
    {
        string file = Request.RequestUri!.OriginalString;
        int idx = file.IndexOf("//");
        if (idx != -1)
            file = file[(idx + 2)..];
        if (!file.Contains('/'))
            return Reference.Null;
        file = file[(file.LastIndexOf('/') + 1)..];

        idx = file.IndexOf('?');
        if (idx != -1)
            return Jvm.AllocateString(file[..idx]);
        idx = file.IndexOf('#');
        if (idx != -1)
            return Jvm.AllocateString(file[..idx]);
        return Jvm.AllocateString(file);
    }

    [return: String]
    public Reference getQuery()
    {
        //return Jvm.AllocateString(Request.RequestUri!.Query);
        string file = Request.RequestUri!.OriginalString;
        int idx = file.IndexOf("//");
        if (idx != -1)
            file = file[(idx + 2)..];
        if (!file.Contains('/'))
            return Reference.Null;
        file = file[(file.LastIndexOf('/') + 1)..];

        idx = file.IndexOf('?');
        if (idx == -1)
            return Reference.Null;
        file = file[(idx + 1)..];
        idx = file.IndexOf('#');
        if (idx != -1)
            return Jvm.AllocateString(file[..idx]);
        return Jvm.AllocateString(file);
    }

    [return: String]
    public Reference getRef()
    {
        string file = Request.RequestUri!.OriginalString;
        int idx = file.IndexOf("//");
        if (idx != -1)
            file = file[(idx + 2)..];
        if (!file.Contains('/'))
            return Reference.Null;
        file = file[(file.LastIndexOf('/') + 1)..];

        idx = file.IndexOf('#');
        if (idx == -1)
            return Reference.Null;
        return Jvm.AllocateString(file[(idx + 1)..]);
    }

    [return: String]
    public Reference GetHeaderFieldInternal([String] Reference key)
    {
        if (key.IsNull)
            return key;
        string k = Jvm.ResolveString(key).ToLower();
        if (ResHeadersTable.ContainsKey(k))
            return Jvm.AllocateString(ResHeadersTable[k]);
        return Reference.Null;
    }

    [return: String]
    public Reference GetHeaderFieldInternal(int n)
    {
        n = (n * 2) + 1;
        if (n >= ResHeaders.Count)
            return Reference.Null;
        return Jvm.AllocateString(ResHeaders[n]);
    }

    [return: String]
    public Reference GetHeaderFieldKeyInternal(int n)
    {
        n *= 2;
        if (n >= ResHeaders.Count)
            return Reference.Null;
        return Jvm.AllocateString(ResHeaders[n]);
    }

    public long GetHeaderFieldDateInternal([String] Reference name, long def)
    {
        return GetHeaderDate(Jvm.ResolveString(name), def);
    }

    // ContentConnection methods

    [return: String]
    public Reference GetEncodingInternal()
    {
        try
        {
            return Jvm.AllocateString(Response.Content.Headers.ContentEncoding.First());
        }
        catch
        {
            return Reference.Null;
        }
    }

    public long GetLengthInternal()
    {
        long? l = Response.Content.Headers.ContentLength;
        if (l == null)
            return -1;
        return (long)l;
    }

    [return: String]
    public Reference GetTypeInternal()
    {
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
        if (InputState != 2)
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

    [JavaIgnore]
    private string? GetRequestHeader(string key)
    {
        if (!ReqHeadersTable.TryGetValue(key.ToLower(), out var v))
            return null;
        return v;
    }

    [JavaIgnore]
    private void SetRequestHeader(string key, string value)
    {
        if (!ReqHeadersTable.ContainsKey(key))
            ReqHeaderKeys.Add(key);
        ReqHeadersTable.Add(key.ToLower(), value);
    }

    [JavaIgnore]
    private long GetHeaderDate(string key, long def)
    {
        key = key.ToLower();
        if (!ResHeadersTable.ContainsKey(key))
            return def;
        try
        {
            return new DateTimeOffset(DateTime.Parse(ResHeadersTable[key])).ToUnixTimeMilliseconds();
        }
        catch
        {
            return def;
        }

    }

    public override bool OnObjectDelete()
    {
        if (InputState != 0 && InputState != 2)
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

    // bytecode

    [JavaDescriptor("()V")]
    public JavaMethodBody doRequest(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetField(nameof(HttpConnectionImpl.RequestSent), typeof(bool), typeof(HttpConnectionImpl));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.AppendReturn();
        }
        b.AppendThis();
        b.Append(JavaOpcode.iconst_1);
        b.AppendPutField(nameof(HttpConnectionImpl.RequestSent), typeof(bool), typeof(HttpConnectionImpl));
        b.AppendThis();
        b.AppendNewObject<Object>();
        b.Append(JavaOpcode.dup);
        b.AppendVirtcall("<init>", "()V");
        b.AppendPutField(nameof(HttpConnectionImpl.RequestLock), typeof(Object), typeof(HttpConnectionImpl));
        b.AppendThis();
        b.AppendVirtcall("DoRequestAsync", "()V");
        using (var tr = b.BeginTry<InterruptedException>())
        {
            b.AppendThis();
            b.AppendGetField(nameof(HttpConnectionImpl.RequestLock), typeof(Object), typeof(HttpConnectionImpl));
            b.Append(JavaOpcode.dup);
            b.Append(JavaOpcode.astore_1);
            b.Append(JavaOpcode.monitorenter);
            b.AppendThis();
            b.AppendGetField(nameof(HttpConnectionImpl.RequestLock), typeof(Object), typeof(HttpConnectionImpl));
            b.AppendVirtcall("wait", "()V");
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.monitorexit);

            b.AppendThis();
            b.AppendGetField(nameof(HttpConnectionImpl.RequestException), typeof(IOException), typeof(HttpConnectionImpl));
            using (b.AppendGoto(JavaOpcode.ifnull))
            {
                b.AppendThis();
                b.AppendGetField(nameof(HttpConnectionImpl.RequestException), typeof(IOException), typeof(HttpConnectionImpl));
                b.Append(JavaOpcode.athrow);
            }
            b.AppendReturn();

            tr.CatchSection();

            b.Append(JavaOpcode.astore_1);
            b.AppendNewObject<IOException>();
            b.Append(JavaOpcode.dup);
            b.AppendConstant("Interrupted");
            b.AppendVirtcall("<init>", "(Ljava/lang/String;)V");
            b.Append(JavaOpcode.athrow);
        }
        b.AppendReturn(); // XXX
        return b.Build(4, 2);
    }

    [JavaDescriptor("()Ljava/io/InputStream;")]
    public JavaMethodBody openInputStream(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall("checkOpen", "()V");
        b.AppendThis();
        b.AppendGetField(nameof(InputState), typeof(int), typeof(HttpConnectionImpl));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.AppendNewObject<IOException>();
            b.Append(JavaOpcode.dup);
            b.AppendConstant("Stream already open");
            b.AppendVirtcall("<init>", "(Ljava/lang/String;)V");
            b.Append(JavaOpcode.athrow);
        }
        b.AppendThis();
        b.AppendVirtcall("doRequest", "()V");
        b.AppendThis();
        b.AppendVirtcall(nameof(OpenInputStreamInternal), "()Ljava/lang/InputStream;");
        b.AppendReturnReference();
        return b.Build(3, 1);
    }

    [JavaDescriptor("()Ljava/io/OutputStream;")]
    public JavaMethodBody openOutputStream(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall("checkOpen", "()V");
        b.AppendThis();
        b.AppendGetField(nameof(RequestSent), typeof(bool), typeof(HttpConnectionImpl));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.AppendNewObject<IOException>();
            b.Append(JavaOpcode.dup);
            b.AppendConstant("Request sent");
            b.AppendVirtcall("<init>", "(Ljava/lang/String;)V");
            b.Append(JavaOpcode.athrow);
        }
        b.AppendThis();
        b.AppendGetField(nameof(OutputState), typeof(int), typeof(HttpConnectionImpl));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.AppendNewObject<IOException>();
            b.Append(JavaOpcode.dup);
            b.AppendConstant("Stream already open");
            b.AppendVirtcall("<init>", "(Ljava/lang/String;)V");
            b.Append(JavaOpcode.athrow);
        }
        b.AppendThis();
        b.AppendVirtcall("OpenOutputStreamInternal", "()Ljava/lang/OutputStream;");
        b.AppendReturnReference();
        return b.Build(3, 1);
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
        return b.Build(3, 1);
    }

    [JavaDescriptor("()Ljava/io/DataOutputStream;")]
    public JavaMethodBody openDataOutputStream(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendNewObject<DataOutputStream>();
        b.Append(JavaOpcode.dup);
        b.AppendThis();
        b.AppendVirtcall("openOutputStream", "()Ljava/io/OutputStream;");
        b.AppendVirtcall("<init>", "(Ljava/io/OutputStream;)V");
        b.AppendReturnReference();
        return b.Build(3, 1);
    }

    [JavaDescriptor("()I")]
    public JavaMethodBody getResponseCode(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall("checkOpen", "()V");
        b.AppendThis();
        b.AppendVirtcall("doRequest", "()V");
        b.AppendThis();
        b.AppendVirtcall("GetResponseCodeInternal", "()I");
        b.AppendReturnInt();
        return b.Build(1, 1);
    }

    [JavaDescriptor("()Ljava/lang/String;")]
    public JavaMethodBody getResponseMessage(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall("checkOpen", "()V");
        b.AppendThis();
        b.AppendVirtcall("doRequest", "()V");
        b.AppendThis();
        b.AppendVirtcall("GetResponseMessageInternal", "()Ljava/lang/String;");
        b.AppendReturnReference();
        return b.Build(1, 1);
    }

    [JavaDescriptor("(Ljava/lang/String;)V")]
    public JavaMethodBody setRequestMethod(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall("checkOpen", "()V");
        b.AppendThis();
        b.AppendGetField(nameof(RequestSent), typeof(bool), typeof(HttpConnectionImpl));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.AppendNewObject<IOException>();
            b.Append(JavaOpcode.dup);
            b.AppendConstant("Request sent");
            b.AppendVirtcall("<init>", "(Ljava/lang/String;)V");
            b.Append(JavaOpcode.athrow);
        }
        b.AppendThis();
        b.AppendGetField(nameof(OutputState), typeof(int), typeof(HttpConnectionImpl));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.AppendReturn();
        }
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall("SetRequestMethodInternal", "(Ljava/lang/String;)V");
        b.AppendReturn();
        return b.Build(3, 2);
    }

    [JavaDescriptor("(Ljava/lang/String;Ljava/lang/String;)V")]
    public JavaMethodBody setRequestProperty(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall("checkOpen", "()V");
        b.AppendThis();
        b.AppendGetField(nameof(RequestSent), typeof(bool), typeof(HttpConnectionImpl));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.AppendNewObject<IOException>();
            b.Append(JavaOpcode.dup);
            b.AppendConstant("Request sent");
            b.AppendVirtcall("<init>", "(Ljava/lang/String;)V");
            b.Append(JavaOpcode.athrow);
        }
        b.AppendThis();
        b.AppendGetField(nameof(OutputState), typeof(int), typeof(HttpConnectionImpl));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.AppendReturn();
        }
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.aload_2);
        b.AppendVirtcall("SetRequestPropertyInternal", "(Ljava/lang/String;Ljava/lang/String;)V");
        b.AppendReturn();
        return b.Build(3, 3);
    }

    [JavaDescriptor("()J")]
    public JavaMethodBody getExpiration(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        using (var tr = b.BeginTry<IOException>())
        {
            b.AppendThis();
            b.AppendVirtcall("checkOpen", "()V");
            b.AppendThis();
            b.AppendVirtcall("doRequest", "()V");
            b.AppendThis();
            b.AppendVirtcall("GetExpirationInternal", "()J");
            b.AppendReturnLong();

            tr.CatchSection();

            b.Append(JavaOpcode.astore_1);
        }
        b.Append(JavaOpcode.lconst_0);
        b.AppendReturnLong();
        return b.Build(2, 2);
    }

    [JavaDescriptor("()J")]
    public JavaMethodBody getDate(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        using (var tr = b.BeginTry<IOException>())
        {
            b.AppendThis();
            b.AppendVirtcall("checkOpen", "()V");
            b.AppendThis();
            b.AppendVirtcall("doRequest", "()V");
            b.AppendThis();
            b.AppendVirtcall("GetDateInternal", "()J");
            b.AppendReturnLong();

            tr.CatchSection();

            b.Append(JavaOpcode.astore_1);
        }
        b.Append(JavaOpcode.lconst_0);
        b.AppendReturnLong();
        return b.Build(2, 2);
    }

    [JavaDescriptor("()J")]
    public JavaMethodBody getLastModifed(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        using (var tr = b.BeginTry<IOException>())
        {
            b.AppendThis();
            b.AppendVirtcall("checkOpen", "()V");
            b.AppendThis();
            b.AppendVirtcall("doRequest", "()V");
            b.AppendThis();
            b.AppendVirtcall("GetLastModifiedInternal", "()J");
            b.AppendReturnLong();

            tr.CatchSection();

            b.Append(JavaOpcode.astore_1);
        }
        b.Append(JavaOpcode.lconst_0);
        b.AppendReturnLong();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(Ljava/lang/String;)Ljava/lang/String;")]
    public JavaMethodBody getHeaderField(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall("checkOpen", "()V");
        b.AppendThis();
        b.AppendVirtcall("doRequest", "()V");
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall("GetHeaderFieldInternal", "(Ljava/lang/String;)Ljava/lang/String;");
        b.AppendReturnReference();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(I)Ljava/lang/String;")]
    public JavaMethodBody getHeaderField___int(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall("checkOpen", "()V");
        b.AppendThis();
        b.AppendVirtcall("doRequest", "()V");
        b.AppendThis();
        b.Append(JavaOpcode.iload_1);
        b.AppendVirtcall("GetHeaderFieldInternal", "(I)Ljava/lang/String;");
        b.AppendReturnReference();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(I)Ljava/lang/String;")]
    public JavaMethodBody getHeaderFieldKey(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall("checkOpen", "()V");
        b.AppendThis();
        b.AppendVirtcall("doRequest", "()V");
        b.AppendThis();
        b.Append(JavaOpcode.iload_1);
        b.AppendVirtcall("GetHeaderFieldKeyInternal", "(I)Ljava/lang/String;");
        b.AppendReturnReference();
        return b.Build(2, 2);
    }

    [JavaDescriptor("(Ljava/lang/String;I)I")]
    public JavaMethodBody getHeaderFieldInt(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall("checkOpen", "()V");
        b.AppendThis();
        b.AppendVirtcall("doRequest", "()V");
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall("GetHeaderField", "(Ljava/lang/String;)Ljava/lang/String;");
        b.Append(JavaOpcode.astore_3);
        b.Append(JavaOpcode.aload_3);
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            b.Append(JavaOpcode.iload_2);
            b.AppendReturnInt();
        }
        using (var tr = b.BeginTry<NumberFormatException>())
        {
            b.Append(JavaOpcode.aload_3);
            b.AppendStaticCall<Integer>(nameof(Integer.parseInt), typeof(int), typeof(String));
            b.AppendReturnInt();

            tr.CatchSection();

            b.Append(JavaOpcode.astore, 4);
        }
        b.Append(JavaOpcode.iload_2);
        b.AppendReturnInt();
        return b.Build(2, 5);
    }

    [JavaDescriptor("(Ljava/lang/String;J)J")]
    public JavaMethodBody getHeaderFieldDate(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendVirtcall("checkOpen", "()V");
        b.AppendThis();
        b.AppendVirtcall("doRequest", "()V");
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.lload_2);
        b.AppendVirtcall("GetHeaderFieldDateInternal", "(Ljava/lang/String;J)J");
        b.AppendReturnLong();
        return b.Build(4, 4);
    }

    [JavaDescriptor("()Ljava/lang/String;")]
    public JavaMethodBody getEncoding(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        using (var tr = b.BeginTry<NumberFormatException>())
        {
            b.AppendThis();
            b.AppendVirtcall("checkOpen", "()V");
            b.AppendThis();
            b.AppendVirtcall("doRequest", "()V");
            b.AppendThis();
            b.AppendVirtcall("GetEncodingInternal", "()Ljava/lang/String;");
            b.AppendReturnReference();

            tr.CatchSection();

            b.Append(JavaOpcode.astore_1);
        }
        b.Append(JavaOpcode.aconst_null);
        b.AppendReturnReference();
        return b.Build(4, 4);
    }

    [JavaDescriptor("()J")]
    public JavaMethodBody getLength(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        using (var tr = b.BeginTry<NumberFormatException>())
        {
            b.AppendThis();
            b.AppendVirtcall("checkOpen", "()V");
            b.AppendThis();
            b.AppendVirtcall("doRequest", "()V");
            b.AppendThis();
            b.AppendVirtcall("GetEncodingInternal", "()J");
            b.AppendReturnLong();

            tr.CatchSection();

            b.Append(JavaOpcode.astore_1);
        }
        var c = cls.PushConstant(-1L).Split();
        b.Append(new Instruction(JavaOpcode.ldc2_w, c));
        b.AppendReturnLong();
        return b.Build(4, 4);
    }

    [JavaDescriptor("()Ljava/lang/String;")]
    public JavaMethodBody getType(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        using (var tr = b.BeginTry<NumberFormatException>())
        {
            b.AppendThis();
            b.AppendVirtcall("checkOpen", "()V");
            b.AppendThis();
            b.AppendVirtcall("doRequest", "()V");
            b.AppendThis();
            b.AppendVirtcall("GetTypeInternal", "()Ljava/lang/String;");
            b.AppendReturnReference();

            tr.CatchSection();

            b.Append(JavaOpcode.astore_1);
        }
        b.Append(JavaOpcode.aconst_null);
        b.AppendReturnReference();
        return b.Build(4, 4);
    }


    [JavaDescriptor("()V")]
    public JavaMethodBody checkOpen(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.AppendGetField(nameof(Closed), typeof(bool), typeof(HttpConnectionImpl));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.AppendNewObject<IOException>();
            b.Append(JavaOpcode.dup);
            b.AppendConstant("Closed");
            b.AppendVirtcall("<init>", "(Ljava/lang/String;)V");
            b.Append(JavaOpcode.athrow);
        }
        b.AppendReturn();
        return b.Build(3, 1);
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

    public long skip(long n)
    {
        if (n < 0) return 0;
        try
        {
            if(Stream.Position + n >= Stream.Length)
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
