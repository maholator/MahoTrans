// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Native;

/// <summary>
///     Mark something with this attribute to make it invisible for JVM.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method)]
public class JavaIgnoreAttribute : Attribute
{
}