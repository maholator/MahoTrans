namespace javax.microedition.lcdui;

[Flags]
public enum GraphicsAnchor : int
{
    HCenter = 1,
    VCenter = 2,
    Left = 4,
    Right = 8,
    Top = 16,
    Bottom = 32,
    Baseline = 64,

    AllHorizontal = Left | HCenter | Right,
    AllVertical = Top | VCenter | Bottom | Baseline,
}