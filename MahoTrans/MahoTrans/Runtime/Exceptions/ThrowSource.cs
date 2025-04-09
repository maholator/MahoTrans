// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

namespace MahoTrans.Runtime.Exceptions;

/// <summary>
///     How this exception was thrown?
/// </summary>
public enum ThrowSource : byte
{
    /// <summary>
    ///     Exception is thrown via <see cref="JavaOpcode" />.<see cref="JavaOpcode.athrow" />.
    /// </summary>
    Java = 1,

    /// <summary>
    ///     Exception is thrown via <see cref="JvmState" />.<see cref="JvmState.Throw{T}()" />.
    /// </summary>
    Native = 2,
}
