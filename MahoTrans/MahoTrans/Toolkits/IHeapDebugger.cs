// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Object = java.lang.Object;

namespace MahoTrans.Toolkits;

public interface IHeapDebugger : IToolkit
{
    void ObjectCreated(Object obj);

    void ObjectDeleted(Object obj);
}