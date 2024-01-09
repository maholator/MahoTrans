// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.io;
using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace javax.microedition.media;

public class Manager : Object
{
    [JavaDescriptor("(Ljava/io/InputStream;Ljava/lang/String;)Ljavax/microedition/media/Player;")]
    public static Reference createPlayer(Reference stream, Reference type)
    {
        var str = stream.As<InputStream>();
        if (str is ByteArrayInputStream bais)
        {
            var buf = bais.buf.As<Array<sbyte>>().Value;
            var mem = buf.AsMemory(bais.pos, bais.count - bais.pos);
            var player = Jvm.AllocateObject<Player>();
            player.Handle = Toolkit.Media.Create(mem, Jvm.ResolveStringOrDefault(type), player);
            return player.This;
        }

        throw new NotSupportedException("Non-BAIS stream is not supported for player for now");
    }

    [return: JavaType(typeof(Player))]
    public static Reference createPlayer([String] Reference mrl)
    {
        Jvm.Throw<MediaException>();
        return Reference.Null;
    }
}