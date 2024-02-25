// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;

namespace javax.microedition.media.control;

[JavaInterface]
public interface RateControl : Control
{
    public int setRate(int rate) => throw new AbstractCall();

    public int getMinRate() => throw new AbstractCall();

    public int getMaxRate() => throw new AbstractCall();
}