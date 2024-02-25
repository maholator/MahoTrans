# `Reference`, heap and serializer

While designing MT, i wanted to use CLR and CTS as much as possible. One thing that is bad for us - managed references
and objects are not something that you can work with like with pointers and spans/struct. So i did a hack.

`Reference` struct is just a wrapper over signed `int`. Any "JVM" object has a field where it has this integer,
called `HeapAddress`. MahoTrans also manages its "heap". The heap is a managed array of alive objects (at CLR level -
managed references to such objects, at low level - volatile pointers). When new object is constructed, it's placed into
free slot in that array and stored there. It never changes during object lifetime. Index of this slot/element is
called "heap address" and stored to that field.

Consequences:

- Actual managed reference is kept in single place, to kill object we just write null there.
- Each object always knows its address.
- We can operate with this addresses to not deal with CLR managed references while still working with CTS objects.
- Address resolution is simple: we just look to the array. At low level this gives us volatile pointer which is then
  resolved by CLR each field/virtslot access.
- No managed refs loops, etc.

It's forbidden to store object (well, *managed reference to such object*) that is stored in MT heap anywhere. All "java"
objects have `Reference` fields instead. All toolkits and frontends also operate on `Reference`s. `Reference`s must be "
converted" to managed references as late as possible, this usually happens inside bridges or interpreter.

Serialization of the heap is done via json serializer. We just serialize the whole *array* and deserialize it back when
needed. This works because the only "owner" of each object is that *array*, all others know only index in it.