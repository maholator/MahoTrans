using MahoTrans.Runtime.Types;

namespace MahoTrans.Loader;

/// <summary>
/// Created by <see cref="ClassLoader.ReadJar"/>.
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
}