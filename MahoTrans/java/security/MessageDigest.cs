// Copyright (c) Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;
using System.Security.Cryptography;
using Object = java.lang.Object;

namespace java.security;

public class MessageDigest : Object
{
    [JavaIgnore] private string Algorithm = null!;
    [JavaIgnore] private HashAlgorithm Hash = null!;

    [return: JavaType(typeof(MessageDigest))]
    public static Reference getInstance([String] Reference algorithm)
    {
        string s = Jvm.ResolveString(algorithm).ToUpper().Trim();
        if (s.Length < 3)
            Jvm.Throw<NoSuchAlgorithmException>(s);
        MessageDigest md = Jvm.AllocateObject<MessageDigest>();
        md.Algorithm = s;
        md.InitHash();
        return md.This;
    }

    private void InitHash()
    {
        HashAlgorithm ha;
        switch (Algorithm)
        {
            case "MD5":
                ha = MD5.Create();
                break;
            case "SHA1":
            case "SHA-1":
                ha = SHA1.Create();
                break;
            case "SHA256":
            case "SHA-256":
                ha = SHA256.Create();
                break;
            case "SHA384":
            case "SHA-384":
                ha = SHA384.Create();
                break;
            case "SHA512":
            case "SHA-512":
                ha = SHA512.Create();
                break;
            //TODO MD2
            default:
                Jvm.Throw<NoSuchAlgorithmException>(Algorithm);
                return;
        }
        ha.Initialize();
        Hash = ha;
    }

    public void reset()
    {
        Hash.Clear();
        InitHash();
    }

    public void update([JavaType("[B")] Reference input, int offset, int len)
    {
        sbyte[] b = Jvm.ResolveArray<sbyte>(input);
        Hash.TransformBlock(b.ToUnsigned(), offset, len, null, 0);
    }

    public int digest([JavaType("[B")] Reference buf, int offset, int len)
    {
        sbyte[] b = Jvm.ResolveArray<sbyte>(buf);
        try
        {
            Hash.TransformFinalBlock(System.Array.Empty<byte>(), 0, 0);
            byte[]? hash = Hash.Hash;
            if (hash == null)
                Jvm.Throw<DigestException>();
            int i = 0;
            while (i < Hash.HashSize && i < len)
                b[offset + i] = (sbyte)hash[i++];
            return i;
        }
        catch
        {
            Jvm.Throw<DigestException>();
        }
        return 0;
    }

    public override bool OnObjectDelete()
    {
        Hash.Dispose();
        return false;
    }
}
