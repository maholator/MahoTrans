// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
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

    public readonly IClock Clock;

    public readonly IImageManager Images;

    public readonly IFontManager Fonts;

    public readonly IDisplay Display;

    public readonly IAms Ams;

    public readonly IRecordStore RecordStore;

    public readonly IMedia Media;

    public ILoadLogger? LoadLogger;

    public ILogger? Logger;

    public IHeapDebugger? HeapDebugger;

    public ToolkitCollection(ISystem system, IClock clock, IImageManager images, IFontManager fonts, IDisplay display,
        IAms ams,
        IRecordStore recordStore, IMedia media)
    {
        System = system;
        Clock = clock;
        Images = images;
        Fonts = fonts;
        Display = display;
        Ams = ams;
        RecordStore = recordStore;
        Media = media;
    }
}