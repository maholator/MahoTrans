using System.Diagnostics.CodeAnalysis;
using System.Text;
using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using MahoTrans.Utils;
using Newtonsoft.Json;
using Object = java.lang.Object;

namespace java.util;

public class TimeZone : Object
{
    [JavaIgnore] [JsonProperty] private static Dictionary<string, SimpleTimeZone>? AvailableZones;
    [JavaIgnore] [JsonProperty] private static SimpleTimeZone? Default;
    [JavaIgnore] [JsonProperty] private static SimpleTimeZone GMT = null!;

    [ClassInit]
    public static void ClInit()
    {
        GMT = new SimpleTimeZone(0, "GMT");
    }

    [MemberNotNull(nameof(AvailableZones))]
    private static void initializeAvailable()
    {
        SimpleTimeZone[] zones = TimeZones.GetTimeZones();
        AvailableZones = new Dictionary<string, SimpleTimeZone>();
        AvailableZones.Add(GMT.ID, GMT);
        foreach (var t in zones)
            AvailableZones.Add(t.ID, t);
    }

    // for serializer
    public TimeZone()
    {
    }

    [return: JavaType("[Ljava/lang/String;")]
    public static Reference getAvailableIDs()
    {
        if (AvailableZones == null) initializeAvailable();
        int length = AvailableZones.Count;
        string[] result = AvailableZones.Keys.ToArray();
        return result.AsJavaArray();
    }

    [return: JavaType(typeof(TimeZone))]
    public static Reference getDefault()
    {
        if (Default == null)
            setDefault(null);
        return Default.This;
    }

    [return: String]
    public virtual Reference getID() => Reference.Null;

    public virtual int getOffset(int era, int year, int month, int day, int dayOfWeek, int time) =>
        throw new AbstractJavaMethodCallError();

    public virtual int getRawOffset() => throw new AbstractJavaMethodCallError();

    [return: JavaType(typeof(TimeZone))]
    public static Reference getTimeZone([String] Reference name)
    {
        return getTimeZone(Jvm.ResolveString(name)).This;
    }

    [JavaIgnore]
    private static SimpleTimeZone getTimeZone(string name)
    {
        if (AvailableZones == null) initializeAvailable();
        var zone = AvailableZones.GetValueOrDefault(name);
        if (zone == null)
        {
            if (name.StartsWith("GMT") && name.Length > 3)
            {
                char sign = name[3];
                if (sign == '+' || sign == '-')
                {
                    int[] position = new int[1];
                    var formattedName = formatTimeZoneName(name, 4);
                    if (formattedName == null)
                    {
                        return GMT;
                    }

                    int hour = parseNumber(formattedName, 4, position);
                    if (hour < 0 || hour > 23)
                    {
                        return GMT;
                    }

                    int index = position[0];
                    if (index != -1)
                    {
                        int raw = hour * 3600000;
                        if (index < formattedName.Length && formattedName[index] == ':')
                        {
                            int minute = parseNumber(formattedName, index + 1, position);
                            if (position[0] == -1 || minute < 0 || minute > 59)
                                return GMT;
                            raw += minute * 60000;
                        }
                        else if (hour >= 30 || index > 6)
                        {
                            raw = (hour / 100 * 3600000) + (hour % 100 * 60000);
                        }

                        if (sign == '-') raw = -raw;
                        return new SimpleTimeZone(raw, formattedName);
                    }
                }
            }

            zone = GMT;
        }

        return zone;
    }

    private static string? formatTimeZoneName(string name, int offset)
    {
        StringBuilder buf = new();
        int index = offset;
        int length = name.Length;
        buf.Append(name.Substring(0, offset));
        int colonIndex = -1;

        while (index < length)
        {
            char c = name[index];
            // allow only basic latin block digits
            if ('0' <= c && c <= '9')
            {
                buf.Append(c);
                if ((length - (index + 1)) == 2)
                {
                    if (colonIndex != -1) return null;
                    colonIndex = buf.Length;
                    buf.Append(':');
                }
            }
            else if (c == ':')
            {
                if (colonIndex != -1) return null;
                colonIndex = buf.Length;
                buf.Append(':');
            }
            else
            {
                // invalid name
                return null;
            }

            index++;
        }

        if (colonIndex == -1)
        {
            colonIndex = buf.Length;
            buf.Append(":00");
        }

        if (colonIndex == 5)
        {
            buf.Insert(4, '0');
        }

        return buf.ToString();
    }

    [JavaIgnore]
    private static int parseNumber(string str, int offset, int[] position)
    {
        var index = offset;
        int digit;
        var result = 0;
        while (index < str.Length && (digit = Character.digit(str[index], 10)) != -1)
        {
            index++;
            result = result * 10 + digit;
        }

        position[0] = index == offset ? -1 : index;
        return result;
    }


    [MemberNotNull(nameof(Default))]
    [JavaIgnore]
    private static void setDefault(SimpleTimeZone? timezone)
    {
        if (timezone != null)
        {
            Default = timezone;
            return;
        }

        Default = getTimeZone(Jvm.Toolkit.System.TimeZone);
    }

    public virtual bool useDaylightTime() => throw new AbstractJavaMethodCallError();
}