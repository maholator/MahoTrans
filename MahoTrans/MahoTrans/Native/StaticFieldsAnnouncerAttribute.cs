// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using MahoTrans.Runtime;

namespace MahoTrans.Native;

/// <summary>
///     Notifies GC about hidden references in static fields.
///     Static <see cref="Reference" /> fields are included in roots automatically.
///     This may be needed if you store them is lists or something like that.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public class StaticFieldsAnnouncerAttribute : Attribute
{
}