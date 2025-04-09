// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

using System.Reflection;

namespace MahoTrans.Utils;

public class SafeAssembly
{
    public string? FullName;

    public SafeAssembly(Assembly ass)
    {
        FullName = ass.FullName;
    }
}
