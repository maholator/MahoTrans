// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using javax.microedition.lcdui;

namespace MahoTrans.Runtime.Config;

/// <summary>
///     How to handle <see cref="Graphics" /> objects?
/// </summary>
public enum GraphicsFlow
{
    /// <summary>
    ///     Each time <see cref="Canvas.ObtainGraphics" /> is called, new object is created. This is how this should work
    ///     according to MIDP docs.
    /// </summary>
    [Description("Create new each time")]
    CreateNewEachTime,

    /// <summary>
    ///     ...well, MIDP docs also say that <see cref="Graphics" /> usage outside of <see cref="Canvas.paint" /> is UB. Some
    ///     games rely on fact that the same object is kept alive and returned on each call.
    /// </summary>
    [Description("Cache objects and reuse them")]
    CacheAndReset,
}