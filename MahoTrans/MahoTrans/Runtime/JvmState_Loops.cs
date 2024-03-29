// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;

namespace MahoTrans.Runtime;

public partial class JvmState
{
    private void executeInternalStrict()
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
            } while (_shouldRun);

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
                var target = Toolkit.Clock.TicksPerCycleStep;
                while (target - (DateTime.UtcNow.Ticks - clrTicks) > 0)
                {
                    Thread.SpinWait(50);
                }

                clrTicks += target;
            }
        } while (_shouldRun);
    }

    private void executeInternalWeak()
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
            } while (_shouldRun);

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
                var target = Toolkit.Clock.TicksPerCycleStep;
                while (target - (DateTime.UtcNow.Ticks - clrTicks) > 0)
                {
                    Thread.Sleep(1);
                }

                clrTicks += target;
            }
        } while (_shouldRun);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void executeInternalUnlocked()
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
                } while (_shouldRun);
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
        } while (_shouldRun);
    }
}
