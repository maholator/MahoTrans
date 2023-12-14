using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace javax.microedition.media;

public class Manager : Object
{
    [JavaDescriptor("(Ljava/io/InputStream;Ljava/lang/String;)Ljavax/microedition/media/Player;")]
    public static Reference createPlayer(Reference stream, Reference str)
    {
        Jvm.Throw<MediaException>();
        return Reference.Null;
    }

    [return: JavaType(typeof(Player))]
    public static Reference createPlayer([String] Reference mrl)
    {
        Jvm.Throw<MediaException>();
        return Reference.Null;
    }
}