using System.Runtime.CompilerServices;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    private void ExecuteInternalStrict()
    {
        var clrTicks = DateTime.UtcNow.Ticks;
        do
        {
            do
            {
                for (var i = AliveThreads.Count - 1; i >= 0; i--)
                    JavaRunner.Step(AliveThreads[i], this);

                _cycleNumber++;

                if (_cycleNumber % CYCLES_PER_BUNCH == 0)
                    break;
            } while (_running);

            if (_cycleNumber % CYCLES_PER_BUNCH == 0)
            {
                // synchronize with externals, attach threads
                BetweenBunches?.Invoke(_cycleNumber);
                CheckWakeups();

                // gc
                if (_gcPending)
                {
                    _gcPending = false;
                    RunGarbageCollector();
                }

                // this will be positive if we are running faster than needed
                var target = Toolkit.Clock.GetTicksPerCycleBunch();
                while (target - (DateTime.UtcNow.Ticks - clrTicks) > 0)
                {
                    Thread.SpinWait(50);
                }

                clrTicks += target;
            }
        } while (_running);
    }

    private void ExecuteInternalWeak()
    {
        var clrTicks = DateTime.UtcNow.Ticks;
        do
        {
            do
            {
                for (var i = AliveThreads.Count - 1; i >= 0; i--)
                    JavaRunner.Step(AliveThreads[i], this);

                _cycleNumber++;

                if (_cycleNumber % CYCLES_PER_BUNCH == 0)
                    break;
            } while (_running);

            if (_cycleNumber % CYCLES_PER_BUNCH == 0)
            {
                // synchronize with externals, attach threads
                BetweenBunches?.Invoke(_cycleNumber);
                CheckWakeups();

                // gc
                if (_gcPending)
                {
                    _gcPending = false;
                    RunGarbageCollector();
                }

                // this will be positive if we are running faster than needed
                var target = Toolkit.Clock.GetTicksPerCycleBunch();
                while (target - (DateTime.UtcNow.Ticks - clrTicks) > 0)
                {
                    Thread.Sleep(1);
                }

                clrTicks += target;
            }
        } while (_running);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void ExecuteInternalUnlocked()
    {
        do
        {
            if (AliveThreads.Count == 0)
            {
                Thread.Sleep(1);
                // this will be CYCLES_PER_BUNCH if we were exactly at bunch boundary.
                var cyclesLeft = CYCLES_PER_BUNCH - (_cycleNumber % CYCLES_PER_BUNCH);
                _cycleNumber += cyclesLeft;
            }
            else
            {
                do
                {
                    for (var i = AliveThreads.Count - 1; i >= 0; i--)
                        JavaRunner.Step(AliveThreads[i], this);

                    _cycleNumber++;

                    if (_cycleNumber % CYCLES_PER_BUNCH == 0)
                        break;
                } while (_running);
            }

            if (_cycleNumber % CYCLES_PER_BUNCH == 0)
            {
                // synchronize with externals, attach threads
                BetweenBunches?.Invoke(_cycleNumber);
                CheckWakeups();

                // gc
                if (_gcPending)
                {
                    _gcPending = false;
                    RunGarbageCollector();
                }
            }
        } while (_running);
    }
}