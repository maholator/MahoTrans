// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans;
using MahoTrans.Builder;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Runtime.Types;
using MahoTrans.Utils;
using Newtonsoft.Json;
using Object = java.lang.Object;

// ReSharper disable MemberCanBePrivate.Global

namespace java.util;

public class Hashtable : Object
{
    [JavaIgnore] [JsonProperty] private Dictionary<int, Reference> _storage = null!;

    public override void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        queue.Enqueue(_storage.Values);
    }

    [InitMethod]
    public new void Init()
    {
        base.Init();
        _storage = new Dictionary<int, Reference>();
    }

    [InitMethod]
    public void Init(int cap)
    {
        _storage = new Dictionary<int, Reference>();
        _storage.EnsureCapacity(cap);
    }

    public void clear() => _storage.Clear();

    [JavaDescriptor("(Ljava/lang/Object;)Z")]
    public JavaMethodBody contains(JavaClass cls)
    {
        // locals: this > obj > enum
        JavaMethodBuilder b = new JavaMethodBuilder(cls);

        b.AppendThis();
        b.AppendVirtcall("elements", typeof(Enumeration));
        b.Append(JavaOpcode.astore_2);

        using (var loop = b.BeginLoop(JavaOpcode.ifne))
        {
            b.Append(JavaOpcode.aload_2);
            b.AppendVirtcall(nameof(ArrayEnumerator.nextElement), typeof(Object));
            b.Append(JavaOpcode.aload_1);
            b.Append(JavaOpcode.swap);
            b.AppendVirtcall(nameof(equals), typeof(bool), typeof(Reference));

            using (b.AppendGoto(JavaOpcode.ifeq))
            {
                b.Append(JavaOpcode.iconst_1);
                b.AppendReturnInt();
            }

            loop.ConditionSection();

            b.Append(JavaOpcode.aload_2);
            b.AppendVirtcall(nameof(ArrayEnumerator.hasMoreElements), typeof(bool));
        }

        b.Append(JavaOpcode.iconst_0);
        b.AppendReturnInt();

        return b.Build(3, 3);
    }

    [JavaDescriptor("(Ljava/lang/Object;)Z")]
    public JavaMethodBody containsKey(JavaClass cls)
    {
        // locals: this > key > entry
        JavaMethodBuilder b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall(nameof(hashCode), "()I");
        b.AppendVirtcall(nameof(GetList), "(I)Ljava/util/HashtableEntry;");
        b.Append(JavaOpcode.dup);
        b.Append(JavaOpcode.astore_2);
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            b.Append(JavaOpcode.iconst_0);
            b.AppendReturnInt();
        }

        var loop = b.PlaceLabel();
        b.Append(JavaOpcode.aload_2);
        b.AppendGetField(nameof(HashtableEntry.Key), typeof(Reference), typeof(HashtableEntry));
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall(nameof(equals), typeof(bool), typeof(Reference));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.Append(JavaOpcode.iconst_1);
            b.AppendReturnInt();
        }

        b.Append(JavaOpcode.aload_2);
        b.AppendGetField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
        b.Append(JavaOpcode.dup);
        using (b.AppendGoto(JavaOpcode.ifnull))
        {
            b.Append(JavaOpcode.astore_2);
            b.AppendGoto(JavaOpcode.@goto, loop);
        }

        b.Append(JavaOpcode.pop);
        b.Append(JavaOpcode.iconst_0);
        b.AppendReturnInt();

        return b.Build(4, 3);
    }

    [return: JavaType(typeof(Enumeration))]
    public Reference elements()
    {
        var e = Jvm.AllocateObject<ArrayEnumerator>();
        e.Value = _storage.Values.SelectMany(x =>
        {
            List<Reference> els = new();
            var next = x;
            while (!next.IsNull)
            {
                var nextEntry = Jvm.Resolve<HashtableEntry>(next);
                els.Add(nextEntry.Value);
                next = nextEntry.Next;
            }

            return els;
        }).ToArray();
        return e.This;
    }

    [JavaDescriptor("(Ljava/lang/Object;)Ljava/lang/Object;")]
    public JavaMethodBody get(JavaClass cls)
    {
        // locals: this > key > entry
        JavaMethodBuilder b = new JavaMethodBuilder(cls);
        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall(nameof(hashCode), "()I");
        b.AppendVirtcall(nameof(GetList), "(I)Ljava/util/HashtableEntry;");
        b.Append(JavaOpcode.dup);
        b.Append(JavaOpcode.astore_2);
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            b.AppendReturnNull();
        }

        var loop = b.PlaceLabel();
        b.Append(JavaOpcode.aload_2);
        b.AppendGetField(nameof(HashtableEntry.Key), typeof(Reference), typeof(HashtableEntry));
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall(nameof(equals), typeof(bool), typeof(Reference));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.Append(JavaOpcode.aload_2);
            b.AppendGetField(nameof(HashtableEntry.Value), typeof(Reference), typeof(HashtableEntry));
            b.AppendReturnReference();
        }

        b.Append(JavaOpcode.aload_2);
        b.AppendGetField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
        b.Append(JavaOpcode.dup);
        using (b.AppendGoto(JavaOpcode.ifnull))
        {
            b.Append(JavaOpcode.astore_2);
            b.AppendGoto(JavaOpcode.@goto, loop);
        }

        b.Append(JavaOpcode.pop);
        b.AppendReturnNull();

        return b.Build(4, 3);
    }

    public bool isEmpty() => _storage.Count != 0;

    [return: JavaType(typeof(Enumeration))]
    public Reference keys()
    {
        var e = Jvm.AllocateObject<ArrayEnumerator>();
        e.Value = _storage.Values.SelectMany(x =>
        {
            List<Reference> els = new();
            var next = x;
            while (!next.IsNull)
            {
                var nextEntry = Jvm.Resolve<HashtableEntry>(next);
                els.Add(nextEntry.Key);
                next = nextEntry.Next;
            }

            return els;
        }).ToArray();
        return e.This;
    }

    [JavaDescriptor("(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;")]
    public JavaMethodBody put(JavaClass cls)
    {
        // locals: this > key > value > entry > hash
        JavaMethodBuilder b = new JavaMethodBuilder(cls);

        b.AppendThis();
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall(nameof(hashCode), "()I");
        b.Append(JavaOpcode.dup);
        b.Append(new Instruction(JavaOpcode.istore, new byte[] { 4 }));
        b.AppendVirtcall(nameof(GetList), "(I)Ljava/util/HashtableEntry;");
        b.Append(JavaOpcode.dup);
        b.Append(JavaOpcode.astore_3);
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            // there is no such hash, new entry must be created
            b.AppendThis();
            b.Append(new Instruction(JavaOpcode.iload, new byte[] { 4 }));
            b.Append(JavaOpcode.aload_1, JavaOpcode.aload_2);
            b.AppendVirtcall(nameof(AddNewList), typeof(void), typeof(int), typeof(Reference), typeof(Reference));
            b.Append(JavaOpcode.aconst_null);
            b.AppendReturnReference();
        }

        var loop = b.PlaceLabel();
        b.Append(JavaOpcode.aload_3);
        b.AppendGetField(nameof(HashtableEntry.Key), typeof(Reference), typeof(HashtableEntry));
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall(nameof(equals), typeof(bool), typeof(Reference));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            // if key is equal to passed, save old value, put new and return old
            b.Append(JavaOpcode.aload_3);
            b.AppendGetField(nameof(HashtableEntry.Value), typeof(Reference), typeof(HashtableEntry));
            b.Append(JavaOpcode.aload_3);
            b.Append(JavaOpcode.aload_2);
            b.AppendPutField(nameof(HashtableEntry.Value), typeof(Reference), typeof(HashtableEntry));
            b.AppendReturnReference();
        }
        // else, take next entry and check it

        b.Append(JavaOpcode.aload_3);
        b.AppendGetField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
        b.Append(JavaOpcode.dup);
        using (b.AppendGoto(JavaOpcode.ifnull))
        {
            b.Append(JavaOpcode.astore_3);
            b.AppendGoto(JavaOpcode.@goto, loop);
        }

        b.Append(JavaOpcode.pop);

        // entry.Next = new Entry(var1, var2)
        b.Append(JavaOpcode.aload_3);
        b.AppendNewObject<HashtableEntry>();
        b.Append(JavaOpcode.dup);
        b.Append(JavaOpcode.aload_1);
        b.Append(JavaOpcode.aload_2);
        b.AppendVirtcall("<init>", typeof(void), typeof(Reference), typeof(Reference));
        b.AppendPutField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
        b.Append(JavaOpcode.aconst_null);
        b.AppendReturnReference();
        return b.Build(5, 5);
    }

#pragma warning disable CA1822
    public void rehash()
#pragma warning restore CA1822
    {
    }

    [JavaDescriptor("(Ljava/lang/Object;)Ljava/lang/Object;")]
    public JavaMethodBody remove(JavaClass cls)
    {
        /* Java source:

        public class HT {
	        public Object remove(Object key) {
		        int hash = key.hashCode();
 		        HashtableEntry e = GetList(hash);
		        if(e.Key.equals(key)) {
			        SetList(hash, e.Next);
			        return e.Value;
		        }
		        while(true) {
			        if(e.Next == null) return null;
			        if(e.Next.Key.equals(key)) {
				        Object was = e.Next.Value;
				        e.Next = e.Next.Next;
				        return was;
			        }
			        e = e.Next;
		        }
	        }
	        public HashtableEntry GetList(int i) { return null; }
	        public void SetList(int i, HashtableEntry e) { }
        }
        class HashtableEntry {
	        public Object Key;
	        public Object Value;
	        public HashtableEntry Next;
        }

        Bytecode below is created by javac --release 8.
         */

        JavaMethodBuilder b = new JavaMethodBuilder(cls);

        // hash code
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall(nameof(hashCode), "()I");
        b.Append(JavaOpcode.istore_2);

        // list
        b.AppendThis();
        b.Append(JavaOpcode.iload_2);
        b.AppendVirtcall(nameof(GetList), "(I)Ljava/util/HashtableEntry;");
        b.Append(JavaOpcode.dup);
        b.Append(JavaOpcode.astore_3);

        // root existence check
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            b.AppendReturnNull();
        }

        // root key check

        b.Append(JavaOpcode.aload_3);
        b.AppendGetField(nameof(HashtableEntry.Key), typeof(Reference), typeof(HashtableEntry));
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall(nameof(equals), typeof(bool), typeof(Reference));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.Append(JavaOpcode.aload_0);
            b.Append(JavaOpcode.iload_2);
            b.Append(JavaOpcode.aload_3);
            b.AppendGetField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
            b.AppendVirtcall(nameof(SetList), typeof(void), typeof(int), typeof(HashtableEntry));
            b.Append(JavaOpcode.aload_3);
            b.AppendGetField(nameof(HashtableEntry.Value), typeof(Reference), typeof(HashtableEntry));
            b.AppendReturnReference();
        }

        var loop = b.PlaceLabel();

        // entry.next == null ?
        b.Append(JavaOpcode.aload_3);
        b.AppendGetField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
        using (b.AppendGoto(JavaOpcode.ifnonnull))
        {
            b.AppendReturnNull();
        }

        // entry.next.key EQ key ?
        b.Append(JavaOpcode.aload_3);
        b.AppendGetField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
        b.AppendGetField(nameof(HashtableEntry.Key), typeof(Reference), typeof(HashtableEntry));
        b.Append(JavaOpcode.aload_1);
        b.AppendVirtcall(nameof(equals), typeof(bool), typeof(Reference));
        using (b.AppendGoto(JavaOpcode.ifeq))
        {
            b.Append(JavaOpcode.aload_3);
            b.AppendGetField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
            b.AppendGetField(nameof(HashtableEntry.Value), typeof(Reference), typeof(HashtableEntry));
            b.Append(new Instruction(JavaOpcode.astore, new byte[] { 4 }));
            b.Append(JavaOpcode.aload_3);
            b.Append(JavaOpcode.aload_3);
            b.AppendGetField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
            b.AppendGetField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
            b.AppendPutField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
            b.Append(new Instruction(JavaOpcode.aload, new byte[] { 4 }));
            b.AppendReturnReference();
        }

        b.Append(JavaOpcode.aload_3);
        b.AppendGetField(nameof(HashtableEntry.Next), typeof(HashtableEntry), typeof(HashtableEntry));
        b.Append(JavaOpcode.astore_3);
        b.AppendGoto(JavaOpcode.@goto, loop);
        return b.Build(4, 5);
    }

    public int size() => Jvm.Resolve<ArrayEnumerator>(elements()).Value.Length;

    [return: String]
    public Reference toString()
    {
        //TODO {key: value, key:value}
        return Jvm.AllocateString($"hashtable, {size()} elements");
    }

    #region Utils

    [return: JavaType(typeof(HashtableEntry))]
    public Reference GetList(int hash)
    {
        if (_storage.TryGetValue(hash, out var p))
            return p;
        return Reference.Null;
    }

    public void SetList(int hash, [JavaType(typeof(HashtableEntry))] Reference list)
    {
        if (list.IsNull)
        {
            _storage.Remove(hash);
            return;
        }

        _storage[hash] = list;
    }

    public void AddNewList(int hash, Reference key, Reference value)
    {
        var e = Jvm.AllocateObject<HashtableEntry>();
        e.Init(key, value);
        _storage.Add(hash, e.This);
    }

    #endregion
}