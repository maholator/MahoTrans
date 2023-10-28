namespace MahoTrans.Utils;

public class CustomConvertorAssembly : Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert ( Type objectType )
    {
        return 
            objectType.FullName == "System.Reflection.Assembly" 
            || objectType.FullName == "System.Reflection.Emit.InternalAssemblyBuilder"
            || objectType.IsSubclassOf ( typeof( System.Reflection.Assembly ) )
            || objectType.IsAssignableFrom ( typeof ( System.Reflection.Assembly ) )
            ;
    }

    public override object ReadJson ( Newtonsoft.Json.JsonReader reader , Type objectType , object existingValue , Newtonsoft.Json.JsonSerializer serializer )
    {
        throw new NotImplementedException ( );
    }

    public override void WriteJson ( Newtonsoft.Json.JsonWriter writer , object value , Newtonsoft.Json.JsonSerializer serializer )
    {
        if ( value is System.Reflection.Assembly ass )
        {
            var safeAss = new SafeAssembly(ass);
            var objectToken = Newtonsoft.Json.Linq.JObject.FromObject(safeAss);
            objectToken.WriteTo ( writer );
        }
    }
}