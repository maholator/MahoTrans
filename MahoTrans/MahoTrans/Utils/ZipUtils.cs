using System.IO.Compression;
using System.Text;

namespace MahoTrans.Utils;

internal static class ZipUtils
{
    /// <summary>
    /// Adds content to archive.
    /// </summary>
    /// <param name="zip">Zip archive to work with.</param>
    /// <param name="name">Name of the entry.</param>
    /// <param name="action">Action that writes data to stream. Do not close it.</param>
    public static void AddEntry(this ZipArchive zip, string name, Action<Stream> action)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using (var stream = entry.Open())
        {
            action(stream);
        }
    }

    /// <summary>
    /// Adds content to archive.
    /// </summary>
    /// <param name="zip">Zip archive to work with.</param>
    /// <param name="name">Name of the entry.</param>
    /// <param name="action">Action that writes data to stream. Do not close it.</param>
    public static void AddTextEntry(this ZipArchive zip, string name, Action<StreamWriter> action)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using (var stream = entry.Open())
        {
            using (var sw = new StreamWriter(stream, Encoding.UTF8))
            {
                action(sw);
            }
        }
    }

    public static string ReadTextEntry(this ZipArchive zip, string name)
    {
        var entry = zip.GetEntry(name);
        using var stream = entry.Open();
        using var sw = new StreamReader(stream, Encoding.UTF8);
        return sw.ReadToEnd();
    }
}