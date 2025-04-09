// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans.Native;

namespace javax.microedition.media.control;

public interface MIDIControl : Control
{
    public void setChannelVolume(int a, int b) => throw new AbstractCall();
}
