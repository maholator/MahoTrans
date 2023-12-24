using MahoTrans.Runtime.Types;

namespace MahoTrans.Loader;

/// <summary>
/// Created by <see cref="ClassLoader.ReadJarFile"/>.
/// </summary>
public class JarPackage
{
    public readonly JavaClass[] Classes;
    public readonly Dictionary<string, byte[]> Resources;
    public readonly Dictionary<string, string> Manifest;

    public JarPackage(JavaClass[] classes, Dictionary<string, byte[]> resources, Dictionary<string, string> manifest)
    {
        Classes = classes;
        Resources = resources;
        Manifest = manifest;
    }

    private static string GetMidletKey(int i) => $"MIDlet-{i}";

    public int GetMidletsCount()
    {
        for (int i = 1; true; i++)
        {
            var key = GetMidletKey(i);

            if (!Manifest.ContainsKey(key))
            {
                return i - 1;
            }
        }
    }

    public string? GetMidletName(int n)
    {
        return GetMidletInfoLine(n)?[0].Trim();
    }

    public string? GetMidletIcon(int n)
    {
        var icon = GetMidletInfoLine(n)?[1].Trim().Trim('/');
        if (string.IsNullOrEmpty(icon))
            return null;
        return icon;
    }

    public string? GetMidletClass(int n)
    {
        return GetMidletInfoLine(n)?[2].Trim();
    }

    private string[]? GetMidletInfoLine(int n)
    {
        if (Manifest.TryGetValue(GetMidletKey(n), out var l))
            return l.Split(',', 3, StringSplitOptions.TrimEntries);
        return null;
    }
}