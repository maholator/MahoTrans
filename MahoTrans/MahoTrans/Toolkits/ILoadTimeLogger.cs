// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MahoTrans.Toolkits;

public interface ILoadTimeLogger : IToolkit
{
    public void Log(LoadIssueType type, string className, string message);
}