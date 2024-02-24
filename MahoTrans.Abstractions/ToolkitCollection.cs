// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using MahoTrans.Abstractions;

namespace MahoTrans;

/// <summary>
///     Object which holds set of implementations needed for JVM to work.
/// </summary>
[PublicAPI]
public class ToolkitCollection
{
    public readonly ISystem System;

    public Clock Clock;

    public readonly IImageManager Images;

    public IFontManager Fonts;

    public readonly IDisplay Display;

    public IAmsCallbacks? AmsCallbacks;

    public IRecordStore RecordStore;

    public readonly IMedia Media;

    public ILoadLogger? LoadLogger;

    public ILogger? Logger;

    public IHeapDebugger? HeapDebugger;

    public ToolkitCollection(ISystem system, Clock clock, IImageManager images, IFontManager fonts, IDisplay display,
        IRecordStore recordStore, IMedia media)
    {
        System = system;
        Clock = clock;
        Images = images;
        Fonts = fonts;
        Display = display;
        RecordStore = recordStore;
        Media = media;
    }
}