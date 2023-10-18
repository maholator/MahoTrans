using java.lang;
using MahoTrans.Native;
using Object = java.lang.Object;

namespace java.util;

public class Random : Object
{
    public long Seed;

    [InitMethod]
    public void Init() => Init(Toolkit.Clock.GetCurrentMs());

    [InitMethod]
    public void Init(long seed) => setSeed(seed);

    public void setSeed(long seed)
    {
        Seed = (seed ^ 0x5DEECE66DL) & ((1L << 48) - 1);
    }

    public int next(int bits)
    {
        Seed = (Seed * 0x5DEECE66DL + 0xBL) & ((1L << 48) - 1);
        return (int)Urs(Seed, (48 - bits));
    }

    public int nextInt() => next(32);

    public int nextInt(int n)
    {
        if (n <= 0)
            Heap.Throw<IllegalArgumentException>();

        if ((n & -n) == n) // i.e., n is a power of 2
            return (int)((n * (long)next(31)) >> 31);

        int bits, val;
        do
        {
            bits = next(31);
            val = bits % n;
        } while (bits - val + (n - 1) < 0);

        return val;
    }

    public long nextLong()
    {
        return ((long)next(32) << 32) + next(32);
    }

    public float nextFloat() => next(24) / ((float)(1 << 24));

    public double nextDouble()
    {
        return (((long)next(26) << 27) + next(27)) / (double)(1L << 53);
    }

    private long Urs(long val, int sh)
    {
        ulong uval = (ulong)val;
        return (long)(uval >> sh);
    }
}