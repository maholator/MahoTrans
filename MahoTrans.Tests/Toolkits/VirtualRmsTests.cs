// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.ToolkitImpls.Rms;

namespace MahoTrans.Tests.Toolkits;

public class VirtualRmsTests
{
    [Test]
    public void TestVirtualFiles()
    {
        SortedDictionary<string, List<byte[]?>> data1 = new();

        foreach (var tuple in createData())
        {
            data1[tuple.Item1] = tuple.Item2;
        }

        VirtualRms rms1 = new VirtualRms(data1);
        var stream1 = new MemoryStream();
        rms1.Write(stream1);

        SortedDictionary<string, List<byte[]?>> data2 = new();

        foreach (var tuple in createData().Select(x => (x, Random.Shared.Next())).OrderBy(x => x.Item2)
                     .Select(x => x.x))
        {
            data2[tuple.Item1] = tuple.Item2;
        }

        VirtualRms rms2 = new VirtualRms(data2);
        var stream2 = new MemoryStream();
        rms2.Write(stream2);

        assert(stream1, stream2);
    }

    private static void assert(MemoryStream stream1, MemoryStream stream2)
    {
        var blob1 = stream1.ToArray();
        var blob2 = stream2.ToArray();
        Assert.That(blob1.SequenceEqual(blob2));
    }

    private IEnumerable<(string, List<byte[]?>)> createData()
    {
        yield return ("abc", new List<byte[]?>());
        yield return ("123", new List<byte[]?>
        {
            new byte[] { 3, 5, 6 },
        });

        yield return ("4ndjs91", new List<byte[]?>());

        yield return ("3djjsi9", new List<byte[]?>
        {
            new byte[] { 1, 2, 5, 1, 5, 2 },
            new byte[] { 51, 8, 1, 7, 24, 11, 3, 7, 1, 2, 5, 32, 121, 1, 245, 23, 4, 12 }
        });


        yield return ("1234", new List<byte[]?>
        {
            new byte[] { 3, 5, 98, 0, 1 },
        });
    }
}