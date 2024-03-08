// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
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
    void Log(LoadIssueType type, string className, string message);

    /// <summary>
    ///     Reports progress of linker's work.
    /// </summary>
    /// <param name="num">Number of class that will be processed.</param>
    /// <param name="total">Total number of classes.</param>
    /// <param name="name">Name of the class.</param>
    /// <remarks>
    ///     This never achieves 100% (num == total) because this reports BEFORE the work starts.
    /// </remarks>
    void ReportLinkProgress(int num, int total, string name);

    /// <summary>
    ///     Reports progress of compiler's work.
    /// </summary>
    /// <param name="num">Number of class that will be processed.</param>
    /// <param name="total">Total number of classes.</param>
    /// <param name="name">Name of the class.</param>
    /// <remarks>
    ///     This never achieves 100% (num == total) because this reports BEFORE the work starts.
    /// </remarks>
    void ReportCompileProgress(int num, int total, string name);
}
