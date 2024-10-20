// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using Math = System.Math;

namespace javax.microedition.lcdui;

public class Gauge : Item
{
    public int Value;
    public int MaxValue;
    public bool Editable;

    [InitMethod]
    public void Init([String] Reference label, bool interactive, int maxValue, int value)
    {
        base.Init();
        Label = label;
        MaxValue = maxValue;
        Editable = interactive;
        setValueInternal(value);
    }

    public void setValue(int value)
    {
        setValueInternal(value);
        NotifyToolkit();
    }

    public int getValue() => Value;

    public int getMaxValue() => MaxValue;

    //TODO: setMaxValue?

    public bool isInteractive() => Editable;

    private void setValueInternal(int value)
    {
        if (Editable)
        {
            if (MaxValue <= 0)
                Jvm.Throw<IllegalArgumentException>();
            Value = Math.Clamp(value, 0, MaxValue);
            return;
        }

        if (MaxValue > 0)
        {
            value = Math.Clamp(value, 0, MaxValue);
        }
        else if (MaxValue == INDEFINITE)
        {
            if (value < 0 || value > INCREMENTAL_UPDATING)
                Jvm.Throw<IllegalArgumentException>();
        }
        else
        {
            Jvm.Throw<IllegalArgumentException>();
        }

        Value = value;
    }

    public const int CONTINUOUS_IDLE = 0;
    public const int CONTINUOUS_RUNNING = 2;
    public const int INCREMENTAL_IDLE = 1;
    public const int INCREMENTAL_UPDATING = 3;
    public const int INDEFINITE = -1;
}
