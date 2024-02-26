// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace MahoTrans.Abstractions;

/// <summary>
///     Various message categories for event messages.
/// </summary>
public enum EventCategory
{
    Common = 1,
    [Description("Class initializers")] ClassInitializer,
    [Description("Resources")] Resources,
    [Description("GC")] Gc,
    Threading,
}