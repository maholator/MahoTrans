// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Loader;

/// <summary>
///     This is a fallback class which is used by <see cref="ClassCompiler" /> when real interface is not loaded.
/// </summary>
public interface DummyInterface : IJavaObject
{
}
