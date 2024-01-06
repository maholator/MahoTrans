// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace java.util;

public class TimerTree : Object
{
    [JavaType(typeof(TimerNode))] public Reference Root;

    public bool isEmpty()
    {
        return Root.IsNull;
    }

    [JavaIgnore]
    public void insert(TimerNode z)
    {
        TimerNode? y = null;
        var x = Root.AsNullable<TimerNode>();
        while (x != null)
        {
            y = x;
            if (z.Task.As<TimerTask>().when < x.Task.As<TimerTask>().when)
                x = x.Left.AsNullable<TimerNode>();
            else
                x = x.Right.AsNullable<TimerNode>();
        }

        z.Parent = y?.This ?? Reference.Null;
        if (y == null)
            Root = z.This;
        else if (z.Task.As<TimerTask>().when < y.Task.As<TimerTask>().when)
            y.Left = z.This;
        else
            y.Right = z.This;
    }

    [JavaIgnore]
    public void delete(TimerNode z)
    {
        TimerNode? y;
        TimerNode? x;
        if (z.Left.IsNull || z.Right.IsNull)
            y = z;
        else
            y = successor(z);

        // TODO check that y is not null?
        if (!y!.Left.IsNull)
            x = y.Left.AsNullable<TimerNode>();
        else
            x = y.Right.AsNullable<TimerNode>();
        if (x != null)
            x.Parent = y.Parent;
        if (y.Parent.IsNull)
            Root = x?.This ?? Reference.Null;
        else if (y == y.Parent.AsNullable<TimerNode>()?.Left.AsNullable<TimerNode>())
            y.Parent.As<TimerNode>().Left = x?.This ?? Reference.Null;
        else
            y.Parent.As<TimerNode>().Right = x?.This ?? Reference.Null;
        if (y != z)
            z.Task = y.Task;
    }

    private static TimerNode? successor(TimerNode x)
    {
        if (!x.Right.IsNull)
            return minimum(x.Right).As<TimerNode>();
        TimerNode? y = x.Parent.AsNullable<TimerNode>();
        while (y != null && x == y.Right.AsNullable<TimerNode>())
        {
            x = y;
            y = y.Parent.AsNullable<TimerNode>();
        }

        return y;
    }

    [return: JavaType(typeof(TimerNode))]
    public static Reference minimum([JavaType(typeof(TimerNode))] Reference x)
    {
        while (!x.As<TimerNode>().Left.IsNull)
            x = x.As<TimerNode>().Left;
        return x;
    }

    [return: JavaType(typeof(TimerNode))]
    public Reference minimum() => minimum(Root);
}