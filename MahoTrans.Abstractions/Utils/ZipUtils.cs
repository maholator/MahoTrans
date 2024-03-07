// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO.Compression;
using System.Text;

namespace MahoTrans.Utils;

/// <summary>
///     Some helper methods for working with zip archives.
/// </summary>
public static class ZipUtils
{
    /// <summary>
    ///     Adds content to archive.
    /// </summary>
    /// <param name="zip">Zip archive to work with.</param>
    /// <param name="name">Name of the entry.</param>
    /// <param name="action">Action that writes data to stream. Do not close it.</param>
    public static void AddEntry(this ZipArchive zip, string name, Action<Stream> action)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        entry.SetUnixRights();
        using (var stream = entry.Open())
        {
            action(stream);
        }
    }

    /// <summary>
    ///     Adds content to archive.
    /// </summary>
    /// <param name="zip">Zip archive to work with.</param>
    /// <param name="name">Name of the entry.</param>
    /// <param name="data">Data to write.</param>
    public static void AddEntry(this ZipArchive zip, string name, ReadOnlySpan<byte> data)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        entry.SetUnixRights();
        using var stream = entry.Open();
        stream.Write(data);
    }

    /// <summary>
    ///     Adds content to archive.
    /// </summary>
    /// <param name="zip">Zip archive to work with.</param>
    /// <param name="name">Name of the entry.</param>
    /// <param name="action">Action that writes data to stream. Do not close it.</param>
    public static void AddTextEntry(this ZipArchive zip, string name, Action<StreamWriter> action)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        entry.SetUnixRights();
        using (var stream = entry.Open())
        {
            using (var sw = new StreamWriter(stream, Encoding.UTF8))
            {
                action(sw);
            }
        }
    }

    /// <summary>
    ///     Adds content to archive.
    /// </summary>
    /// <param name="zip">Zip archive to work with.</param>
    /// <param name="name">Name of the entry.</param>
    /// <param name="data">Data to write.</param>
    public static void AddTextEntry(this ZipArchive zip, string name, string data)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        entry.SetUnixRights();
        using var stream = entry.Open();
        using var sw = new StreamWriter(stream, Encoding.UTF8);
        sw.Write(data);
    }

    public static byte[] ReadEntry(this ZipArchive zip, string name)
    {
        var entry = zip.GetEntry(name);
        if (entry == null)
            throw new FileNotFoundException();
        using var stream = entry.Open();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public static string ReadTextEntry(this ZipArchive zip, string name)
    {
        var entry = zip.GetEntry(name);
        if (entry == null)
            throw new FileNotFoundException();
        using var stream = entry.Open();
        using var sw = new StreamReader(stream, Encoding.UTF8);
        return sw.ReadToEnd();
    }

    public static void SetUnixRights(this ZipArchiveEntry entry, string rights = "666")
    {
        entry.ExternalAttributes = entry.ExternalAttributes | (Convert.ToInt32(rights, 8) << 16);
    }
}
