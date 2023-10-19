using BenchmarkDotNet.Attributes;

namespace MahoTrans.Benchmarks;

[MemoryDiagnoser]
public class DictsBenchs
{
    private int[] keys;
    private int[] keysToRemove;
    private BoolCont[] values;

    public static volatile Dictionary<int, BoolCont>? temp;

    private int deletedCount = 0;

    [GlobalSetup]
    public void Setup()
    {
        keys = Enumerable.Range(0, 6000).ToArray();
        values = keys.Select(x => new BoolCont(true)).ToArray();
        keysToRemove = keys.Chunk(3).Select(x => x[0]).ToArray();
    }

    [Benchmark()]
    public void Remove()
    {
        var _heap = CreateHeap();

        var all = _heap.Keys.ToArray();
        foreach (var i in all)
        {
            var obj = _heap[i];
            if (obj.Value)
            {
                obj.Value = false;
            }
            else
            {
                _heap.Remove(i);
                deletedCount++;
            }
        }
    }

    [Benchmark()]
    public void RecreateViaKeys()
    {
        var _heap = CreateHeap();

        temp = new Dictionary<int, BoolCont>(_heap.Count);

        foreach (var i in _heap.Keys)
        {
            var obj = _heap[i];
            if (obj.Value)
            {
                obj.Value = false;
                temp.Add(i, obj);
            }
        }

        temp = null;
    }

    [Benchmark()]
    public void RecreateViaPairs()
    {
        var _heap = CreateHeap();

        temp = new Dictionary<int, BoolCont>(_heap.Count);

        foreach (var kvp in _heap)
        {
            var obj = kvp.Value;
            if (obj.Value)
            {
                obj.Value = false;
                temp.Add(kvp.Key, obj);
            }
        }

        temp = null;
    }

    [Benchmark()]
    public void RecreateViaLinq()
    {
        var _heap = CreateHeap();

        temp = new Dictionary<int, BoolCont>(_heap.Where(x => x.Value.Value));

        Thread.MemoryBarrier();

        temp = null;
    }


    private Dictionary<int, BoolCont> CreateHeap()
    {
        Dictionary<int, BoolCont> d = new Dictionary<int, BoolCont>();
        for (int i = 0; i < keys.Length; i++)
        {
            d.Add(keys[i], values[i]);
        }

        foreach (var i in keysToRemove)
        {
            d[i].Value = false;
        }

        return d;
    }

    public class BoolCont
    {
        public bool Value;

        public BoolCont(bool value)
        {
            Value = value;
        }
    }
}