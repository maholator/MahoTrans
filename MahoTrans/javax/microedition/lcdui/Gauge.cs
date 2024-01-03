// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace javax.microedition.lcdui;

public class Gauge : Item
{
    public const int CONTINUOUS_IDLE = 0;
    public const int CONTINUOUS_RUNNING = 2;
    public const int INCREMENTAL_IDLE = 1;
    public const int INCREMENTAL_UPDATING = 3;
    public const int INDEFINITE = -1;
}