// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using MahoTrans.Runtime.Errors;

namespace MahoTrans.Native;

/// <summary>
///     Throw this if method should not be called by JVM and declared only to be declared.
/// </summary>
public class AbstractCall : JavaRuntimeError
{
}
