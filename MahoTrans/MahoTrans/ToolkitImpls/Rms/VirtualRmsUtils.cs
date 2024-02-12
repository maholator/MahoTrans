// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.Serialization;
using System.Text;
using Be.IO;

namespace MahoTrans.ToolkitImpls.Rms;

/// <summary>
///     Provides utils to read/write snapshots of <see cref="VirtualRms" />.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <listheader>File structure:</listheader>
///         <item>Int32 - count of stores (N).</item>
///         <item>Blob[N] - N blobs of "store" structure.</item>
///     </list>
///     <list type="bullet">
///         <listheader>"store" structure:</listheader>
///         <item>Int32 - length of name, L.</item>
///         <item>byte[L] - name of the store as UTF8.</item>
///         <item>Int32 - count of slots in the store, N.</item>
///         <item>Blob[N] - N blobs of "slot" structure.</item>
///     </list>
///     <list type="bullet">
///         <listheader>"slot" structure:</listheader>
///         <item>Int32 - length of slot data, L. -1 if there is no data. 0 if data is here but it has zero length.</item>
///         <item>byte[L] - slot data. If L is -1, consider it equal 0.</item>
///     </list>
/// </remarks>
public static class VirtualRmsUtils
{
    public static void Write(this VirtualRms rms, Stream stream)
    {
        using var writer = new BeBinaryWriter(stream, Encoding.UTF8, true);
        var dict = rms.Storage;

        // count of stores
        writer.Write(dict.Count);

        // stores data
        foreach (var (name, slots) in dict.OrderBy(x => x.Key))
        {
            // store name
            var enc = Encoding.UTF8.GetBytes(name);
            writer.Write(enc.Length);
            writer.Write(enc);

            // count of slots
            writer.Write(slots.Count);

            // slots data
            foreach (var slot in slots)
            {
                if (slot == null)
                    writer.Write(-1);
                else
                {
                    writer.Write(slot.Length);
                    writer.Write(slot);
                }
            }
        }
    }

    public static VirtualRms Read(Stream stream)
    {
        var dict = new Dictionary<string, List<byte[]?>>();
        using var reader = new BeBinaryReader(stream, Encoding.UTF8, true);

        // count of stores
        int storesCount = reader.ReadInt32();

        Span<byte> bBuf = stackalloc byte[128];

        for (int i = 0; i < storesCount; i++)
        {
            // store name
            int storeNameLen = reader.ReadInt32();
            if (storeNameLen > 128)
                throw new SerializationException($"Too long store name: {storeNameLen}");

            int pos = 0;
            while (pos != storeNameLen)
                pos += reader.Read(bBuf.Slice(pos, storeNameLen - pos));

            var name = Encoding.UTF8.GetString(bBuf.Slice(0, storeNameLen));

            // count of slots
            var slotsCount = reader.ReadInt32();
            List<byte[]?> store = new();

            // slots data
            for (int j = 0; j < slotsCount; j++)
            {
                var len = reader.ReadInt32();
                if (len < 0)
                {
                    store.Add(null);
                    continue;
                }

                if (len == 0)
                {
                    store.Add(Array.Empty<byte>());
                    continue;
                }

                var slot = new byte[len];
                pos = 0;
                while (pos != len)
                    pos += reader.Read(slot.AsSpan(pos));

                store.Add(slot);
            }

            dict.Add(name, store);
        }

        return new VirtualRms(dict);
    }
}