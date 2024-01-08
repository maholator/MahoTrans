// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Various message categories for debug messages.
/// </summary>
public enum DebugMessageCategory
{
    Common = 0,
    ClassInitializer = 1,
    Jit = 2,
    Exceptions = 3,
    Resources = 4,
    Gc = 5,
    Threading = 6,
}