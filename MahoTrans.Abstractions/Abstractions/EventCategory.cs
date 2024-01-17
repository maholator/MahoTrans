// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Various message categories for event messages.
/// </summary>
public enum EventCategory
{
    Common = 1,
    ClassInitializer,
    Jit,
    Resources,
    Gc,
    Threading,
}