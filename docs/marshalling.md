# JVM <-> CLR marshalling in MT

## Terms

| Term      | Explanation                                                              |
|-----------|--------------------------------------------------------------------------|
| JVM       | MahoTrans interpreter, i.e. `JavaRunner` and `Frame`                     |
| ubyte     | `byte` in C#                                                             |
| sbyte     | `byte` in java, `sbyte` in C#                                            |
| Primitive | `sbyte`, `bool`, `short`, `char`, `int`, `long`, `float`, `double` in C# |
| Ref       | MTA `Reference` struct                                                   |

Type checks are done from first line to last line.

## Fields

| Type      | Action                                                                   |
|-----------|--------------------------------------------------------------------------|
| Primitive | Primitive is popped from stack, stack does the conversion.               |
| Ref       | Ref is popped from stack, stack does the conversion. No checks are done. |

## Method args

| Type         | Action                                                                   |
|--------------|--------------------------------------------------------------------------|
| Primitive    | Primitive is popped from stack, stack does the conversion.               |
| Ref          | Ref is popped from stack, stack does the conversion. No checks are done. |
| string       | Ref is popped from stack. Ref resolved as string. NPE may be thrown.     |
| string?      | Ref is popped from stack. Ref safely resolved as string.                 |
| Primitive[]  | Ref is popped from stack. Ref resolved as array. NPE may be thrown.      |
| Primitive[]? | Ref is popped from stack. Ref safely resolved as array.                  |
| Ref[]        | Ref is popped from stack. Ref resolved as array. NPE may be thrown.      |
| Ref[]?       | Ref is popped from stack. Ref safely resolved as array.                  |

## `JavaType` attributes

- Ignored on anything but `Ref`, `Ref[]` and `Ref[]?`.
- When not present on `Ref`, this argument will be linked as `java.lang.Object`.
- When present on `Ref`, sets the type.
- When present on `Ref[]` or `Ref[]?`, sets type of **array elements**.

These are equal:

```
Reference[] r,
[JavaType(typeof(Object))] Reference[] r,
[JavaType("[Ljava/lang/Object;")] Reference r,
```