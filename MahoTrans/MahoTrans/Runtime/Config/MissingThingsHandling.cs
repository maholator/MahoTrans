// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace MahoTrans.Runtime.Config;

public enum MissingThingsHandling
{
    [Description("Throw java error")] ThrowJavaError,

    [Description("Crash")] Crash,
}