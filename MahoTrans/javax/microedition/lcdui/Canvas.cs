// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using javax.microedition.ams;
using javax.microedition.ams.events;
using MahoTrans;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Config;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;

namespace javax.microedition.lcdui;

public class Canvas : Displayable
{
    [JavaType(typeof(Graphics))] public Reference CachedGraphics;

    [InitMethod]
    public override void Init()
    {
        base.Init();
    }

    public void paint([JavaType(typeof(Graphics))] Reference g)
    {
    }

    [return: JavaType(typeof(Graphics))]
    public Reference ObtainGraphics()
    {
        if (Jvm.GraphicsFlow == GraphicsFlow.CacheAndReset)
        {
            if (CachedGraphics.IsNull)
            {
                var cg = Jvm.Allocate<Graphics>();
                cg.Init();
                cg.Handle = Toolkit.Display.GetGraphics(Handle);
                CachedGraphics = cg.This;
            }
            else
            {
                CachedGraphics.As<Graphics>().Reset();
            }

            return CachedGraphics;
        }

        var g = Jvm.Allocate<Graphics>();
        g.Init();
        g.Handle = Toolkit.Display.GetGraphics(Handle);
        return g.This;
    }

    public void repaint()
    {
        Jvm.EventQueue.Enqueue<RepaintEvent>(x => x.Target = This);
    }

    public void repaint(int x, int y, int w, int h)
    {
        //TODO rect
        Jvm.EventQueue.Enqueue<RepaintEvent>(s => s.Target = This);
    }

    public void flushGraphics()
    {
        Toolkit.Display.Flush(Handle);
        if (!CachedGraphics.IsNull)
        {
            CachedGraphics.As<Graphics>().Reset();
        }
    }

    [JavaDescriptor("()V")]
    public JavaMethodBody serviceRepaints(JavaClass cls)
    {
        return new JavaMethodBody(1, 1)
        {
            RawCode = new Instruction[]
            {
                new(JavaOpcode.aload_0),
                new(JavaOpcode.invokespecial,
                    cls.PushConstant(new NameDescriptorClass(nameof(getQueue), "()Ljava/lang/Object;", typeof(Canvas)))
                        .Split()),
                new(JavaOpcode.invokespecial,
                    cls.PushConstant(new NameDescriptorClass(nameof(EventQueue.serviceRepaints), "()V",
                            typeof(EventQueue)))
                        .Split()),
                new(JavaOpcode.@return),
            }
        };
    }

    public Reference getQueue() => Jvm.EventQueue.This;

    public int getGameAction(int keyCode)
    {
        switch (keyCode)
        {
            case -1:
            case '2':
                return UP;
            case -2:
            case '8':
                return DOWN;
            case -3:
            case '4':
                return LEFT;
            case -4:
            case '6':
                return RIGHT;
            case -5:
            case 5:
                return FIRE;
            case '1':
                return GAME_A;
            case '3':
                return GAME_B;
            case '7':
                return GAME_C;
            case '9':
                return GAME_D;
            default:
                return 0;
        }
    }

    public int getKeyCode(int action)
    {
        switch (action)
        {
            case FIRE:
                return -5;
            case GAME_A:
                return '1';
            case GAME_B:
                return '3';
            case GAME_C:
                return '7';
            case GAME_D:
                return '9';
            case UP:
                return -1;
            case DOWN:
                return -2;
            case LEFT:
                return -3;
            case RIGHT:
                return -4;
            default:
                Jvm.Throw<IllegalArgumentException>();
                return 0;
        }
    }

    [return: String]
    public Reference getKeyName(int keyCode)
    {
        return Jvm.AllocateString($"Key {keyCode}");
    }

    public bool hasPointerEvents() => true;
    public bool isDoubleBuffered() => true;
    public bool hasPointerMotionEvents() => true;
    public bool hasRepeatEvents() => false;

    public void setFullScreenMode(bool mode)
    {
        Toolkit.Display.SetFullscreen(Handle, mode);
    }

    #region Event stubs

    public void showNotify()
    {
    }

    public void hideNotify()
    {
    }

    public void pointerPressed(int x, int y)
    {
    }

    public void pointerDragged(int x, int y)
    {
    }

    public void pointerReleased(int x, int y)
    {
    }

    public void keyPressed(int k)
    {
    }

    public void keyReleased(int k)
    {
    }

    #endregion

    public const int DOWN = 6;
    public const int FIRE = 8;
    public const int GAME_A = 9;
    public const int GAME_B = 10;
    public const int GAME_C = 11;
    public const int GAME_D = 12;
    public const int KEY_NUM0 = 48;
    public const int KEY_NUM1 = 49;
    public const int KEY_NUM2 = 50;
    public const int KEY_NUM3 = 51;
    public const int KEY_NUM4 = 52;
    public const int KEY_NUM5 = 53;
    public const int KEY_NUM6 = 54;
    public const int KEY_NUM7 = 55;
    public const int KEY_NUM8 = 56;
    public const int KEY_NUM9 = 57;
    public const int KEY_POUND = 35;
    public const int KEY_STAR = 42;
    public const int LEFT = 2;
    public const int RIGHT = 5;
    public const int UP = 1;
}