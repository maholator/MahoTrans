// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
