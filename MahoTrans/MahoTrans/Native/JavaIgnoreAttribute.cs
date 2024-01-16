// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Loader;

namespace MahoTrans.Native;

/// <summary>
///     Mark something with this attribute to make it invisible for JVM. Note that some types of fields are invisible by default - see <see cref="NativeLinker.IsJavaVisible"/> implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Method)]
public class JavaIgnoreAttribute : Attribute
{
}