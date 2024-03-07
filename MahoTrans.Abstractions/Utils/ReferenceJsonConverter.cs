// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;
using Newtonsoft.Json;

namespace MahoTrans.Utils;

public class ReferenceJsonConverter : JsonConverter<Reference>
{
    public override void WriteJson(JsonWriter writer, Reference value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Index);
    }

    public override Reference ReadJson(JsonReader reader, Type objectType, Reference existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        return new Reference(serializer.Deserialize<int>(reader));
    }
}
