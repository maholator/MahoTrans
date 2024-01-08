// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Abstractions;

/// <summary>
///     Logger which is used by linker to report issues during classes load.
/// </summary>
public interface ILoadLogger : IToolkit
{
    /// <summary>
    ///     Logs an issue, detected by linker during classes load.
    /// </summary>
    /// <param name="type">Type of the issue.</param>
    /// <param name="className">Name of the class, where the issue was detected.</param>
    /// <param name="message">Description of the issue.</param>
    public void Log(LoadIssueType type, string className, string message);
}