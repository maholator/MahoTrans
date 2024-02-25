// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using Object = java.lang.Object;

namespace javax.microedition.ams;

/// <summary>
///     Base class for all events those go through <see cref="EventQueue" />.
/// </summary>
public class Event : Object
{
    public void invoke() => throw new AbstractCall();
}