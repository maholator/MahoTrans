// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.ToolkitImpls.Clocks;
using MahoTrans.ToolkitImpls.Rms;

namespace MahoTrans.ToolkitImpls.Stub;

public static class StubToolkit
{
    public static ToolkitCollection Create() => new(
        new StubSystem(),
        new RealTimeClock(),
        new StubImages(),
        new StubFonts(),
        new NotImplementedDisplay(),
        new VirtualRms(),
        new StubMedia()
    );
}