using MahoTrans.Builder;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using Object = java.lang.Object;

namespace java.io;

public class InputStreamUtils : Object
{
    [JavaDescriptor("(Ljava/io/InputStream;)[B")]
    public static JavaMethodBody readBytes(JavaClass cls)
    {
        var b = new JavaMethodBuilder(cls);

        // buf = new byte[4096];
        b.AppendShort(4096);
        b.Append(JavaOpcode.newarray, (byte)ArrayType.T_BYTE);
        b.Append(JavaOpcode.astore_1);

        // count = 0
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.istore_2);

        // readBuf = new byte[4096];
        b.AppendShort(4096);
        b.Append(JavaOpcode.newarray, (byte)ArrayType.T_BYTE);
        b.Append(JavaOpcode.astore_3);

        using (var loop = b.BeginLoop(JavaOpcode.ifle))
        {
            // read = in.read()
            b.Append(JavaOpcode.aload_0);
            b.Append(JavaOpcode.aload_3);
            b.AppendVirtcall("read", "([B)I");
            b.Append(JavaOpcode.istore, 4);

            loop.ConditionSection();

            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.iload, 4);
            b.Append(JavaOpcode.iadd);
            b.Append(JavaOpcode.aload, 1);
            b.Append(JavaOpcode.arraylength);

            // grow buffer
            using (b.AppendGoto(JavaOpcode.if_icmple)) // if(count + readLen > buf.length)
            {
                // tmp = new byte[count * 2];
                b.Append(JavaOpcode.iload_2);
                b.Append(JavaOpcode.iconst_2);
                b.Append(JavaOpcode.imul);
                b.Append(JavaOpcode.newarray, (byte)ArrayType.T_BYTE);
                b.Append(JavaOpcode.astore, 5);

                // System.arraycopy(tmp, 0, buf, count, read);
                b.Append(JavaOpcode.aload_1);
                b.Append(JavaOpcode.iconst_0);
                b.Append(JavaOpcode.aload, 5);
                b.Append(JavaOpcode.iconst_0);
                b.Append(JavaOpcode.iload_2);
                b.AppendStaticCall(new NameDescriptorClass("arraycopy", "(Ljava/lang/Object;ILjava/lang/Object;II)V", typeof(java.lang.System)));

                // buf = tmp
                b.Append(JavaOpcode.aload, 5);
                b.Append(JavaOpcode.astore_1);
            }

            // System.arraycopy(readBuf, 0, buf, count, read);
            b.Append(JavaOpcode.aload_3);
            b.Append(JavaOpcode.iconst_0);
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.iload, 4);
            b.AppendStaticCall(new NameDescriptorClass("arraycopy", "(Ljava/lang/Object;ILjava/lang/Object;II)V", typeof(java.lang.System)));

            // count += read
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.iload, 4);
            b.Append(JavaOpcode.iadd);
            b.Append(JavaOpcode.istore_2);

            b.Append(JavaOpcode.iload, 4);
        }

        // TODO: if(buf.length == count) return buf

        // res = new byte[count];
        b.Append(JavaOpcode.iload_2);
        b.Append(JavaOpcode.newarray, (byte)ArrayType.T_BYTE);
        b.Append(JavaOpcode.astore, 5);

        // System.arraycopy(buf, 0, res, 0, count);
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.aload, 5);
        b.Append(JavaOpcode.iconst_0);
        b.Append(JavaOpcode.iload_2);
        b.AppendStaticCall(new NameDescriptorClass("arraycopy", "(Ljava/lang/Object;ILjava/lang/Object;II)V", typeof(java.lang.System)));

        // return res;
        b.Append(JavaOpcode.aload, 5);
        b.AppendReturnReference();

        return b.Build(5, 6);
    }


}

