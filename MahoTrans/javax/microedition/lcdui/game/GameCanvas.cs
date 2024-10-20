// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;

namespace javax.microedition.lcdui.game;

public class GameCanvas : Canvas
{
    [InitMethod]
    public void Init(bool events)
    {
        //TODO
        base.Init();
    }

    [return: JavaType(typeof(Graphics))]
    public Reference getGraphics()
    {
        return ObtainGraphics();
    }

    public void flushGraphics(int x, int y, int width, int height)
    {
        //TODO
        flushGraphics();
    }

    public int getKeyStates() => 0;

    public const int DOWN_PRESSED = 64;
    public const int FIRE_PRESSED = 256;
    public const int GAME_A_PRESSED = 512;
    public const int GAME_B_PRESSED = 1024;
    public const int GAME_C_PRESSED = 2048;
    public const int GAME_D_PRESSED = 4096;
    public const int LEFT_PRESSED = 4;
    public const int RIGHT_PRESSED = 32;
    public const int UP_PRESSED = 2;
}
