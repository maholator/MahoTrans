# JVM <-> CLR marshalling in MT

## Terms

| Term      | Explanation                                                          |
|-----------|----------------------------------------------------------------------|
| JVM       | MahoTrans interpreter, i.e. `JavaRunner`                             |
| ubyte     | `byte` in C#                                                         |
| sbyte     | `byte` in java, `sbyte` in C#                                        |
| Primitive | `sbyte`,`bool`, `short`,`char`,`int`,`long`, `float`, `double` in C# |
| Ref       | MT `Reference` struct                                                |

Type checks are done from first line to last line.

## Fields

| Type      | Action                                                                   |
|-----------|--------------------------------------------------------------------------|
| Primitive | Primitive is popped from stack, stack does the conversion.               |
| Ref       | Ref is popped from stack, stack does the conversion. No checks are done. |

## Method args

| Type      | Action                                                                   |
|-----------|--------------------------------------------------------------------------|
| Primitive | Primitive is popped from stack, stack does the conversion.               |
| Ref       | Ref is popped from stack, stack does the conversion. No checks are done. |
