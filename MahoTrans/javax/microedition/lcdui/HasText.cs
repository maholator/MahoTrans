// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENSE file in the repository root for full license text.

namespace javax.microedition.lcdui;

/// <summary>
///     LCDUI objects that have readable/writable text. Invisible to JVM. Can be used by frontend to get/edit text in a
///     common way.
/// </summary>
public interface HasText
{
    string Text { get; set; }

    int MaxSize { get; set; }
}
