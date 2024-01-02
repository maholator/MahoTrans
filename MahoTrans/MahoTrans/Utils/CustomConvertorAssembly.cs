// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MahoTrans.Utils;

public class CustomConvertorAssembly : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return
            objectType.FullName == "System.Reflection.Assembly"
            || objectType.FullName == "System.Reflection.Emit.InternalAssemblyBuilder"
            || objectType.IsSubclassOf(typeof(Assembly))
            || objectType.IsAssignableFrom(typeof(Assembly));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object? value,
        JsonSerializer serializer)
    {
        if (value is Assembly ass)
        {
            var safeAss = new SafeAssembly(ass);
            var objectToken = JObject.FromObject(safeAss);
            objectToken.WriteTo(writer);
        }
    }
}