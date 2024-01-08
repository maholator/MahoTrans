// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Handles;
using MahoTrans.Runtime;

namespace MahoTrans.Abstractions;

/// <summary>
///     Object which receives callbacks from media toolkit.
/// </summary>
public interface IMediaCallbacks
{
    void Update(MediaHandle player, string eventName, Reference data);
}