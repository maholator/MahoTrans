// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using System.ComponentModel;

namespace MahoTrans.Runtime.Config;

public enum MissingThingsHandling : byte
{
    [Description("Throw java error")]
    ThrowJavaError,

    [Description("Crash")]
    Crash,
}
