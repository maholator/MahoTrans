using MahoTrans.Native;
using MahoTrans.Utils;
using Object = java.lang.Object;

namespace java.util;

public sealed class TimeZones : Object
{
    public const int ONE_HOUR = 3600000;

    [JavaIgnore]
    public static SimpleTimeZone[] GetTimeZones()
    {
        var zones = new[]
        {
            new SimpleTimeZone(-11 * ONE_HOUR, "MIT"),
            new SimpleTimeZone(-10 * ONE_HOUR, "HST"),
            new SimpleTimeZone(-9 * ONE_HOUR, "AST",
                Calendar.APRIL, 1, -Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                ONE_HOUR),
            new SimpleTimeZone(-8 * ONE_HOUR, "PST",
                Calendar.APRIL, 1, -Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                ONE_HOUR),
            new SimpleTimeZone(-7 * ONE_HOUR, "MST",
                Calendar.APRIL, 1, -Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                ONE_HOUR),
            new SimpleTimeZone(-7 * ONE_HOUR, "PNT"),
            new SimpleTimeZone(-6 * ONE_HOUR, "CST",
                Calendar.APRIL, 1, -Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                ONE_HOUR),
            new SimpleTimeZone(-5 * ONE_HOUR, "EST",
                Calendar.APRIL, 1, -Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                ONE_HOUR),
            new SimpleTimeZone(-5 * ONE_HOUR, "IET"),
            new SimpleTimeZone(-4 * ONE_HOUR, "PRT"),
            new SimpleTimeZone(-3 * ONE_HOUR - 1800000, "CNT",
                Calendar.APRIL, 1, -Calendar.SUNDAY, 60000, SimpleTimeZone.WALL_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 60000, SimpleTimeZone.WALL_TIME,
                ONE_HOUR),
            new SimpleTimeZone(-3 * ONE_HOUR, "AGT"),
            new SimpleTimeZone(-3 * ONE_HOUR, "BET",
                Calendar.OCTOBER, 8, -Calendar.SUNDAY, 0 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                Calendar.FEBRUARY, 15, -Calendar.SUNDAY, 0 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                ONE_HOUR),
            new SimpleTimeZone(0 * ONE_HOUR, "UTC"),
            new SimpleTimeZone(0 * ONE_HOUR, "WET",
                Calendar.MARCH, -1, Calendar.SUNDAY, 1 * ONE_HOUR, SimpleTimeZone.UTC_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 1 * ONE_HOUR, SimpleTimeZone.UTC_TIME,
                ONE_HOUR),
            new SimpleTimeZone(1 * ONE_HOUR, "CET",
                Calendar.MARCH, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                ONE_HOUR),
            new SimpleTimeZone(1 * ONE_HOUR, "ECT",
                Calendar.MARCH, -1, Calendar.SUNDAY, 1 * ONE_HOUR, SimpleTimeZone.UTC_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 1 * ONE_HOUR, SimpleTimeZone.UTC_TIME,
                ONE_HOUR),
            new SimpleTimeZone(1 * ONE_HOUR, "MET",
                Calendar.MARCH, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                ONE_HOUR),
            new SimpleTimeZone(2 * ONE_HOUR, "ART",
                Calendar.APRIL, -1, Calendar.FRIDAY, 0 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                Calendar.SEPTEMBER, -1, Calendar.THURSDAY, 23 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                ONE_HOUR),
            new SimpleTimeZone(2 * ONE_HOUR, "CAT"),
            new SimpleTimeZone(2 * ONE_HOUR, "EET",
                Calendar.MARCH, -1, Calendar.SUNDAY, 1 * ONE_HOUR, SimpleTimeZone.UTC_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 1 * ONE_HOUR, SimpleTimeZone.UTC_TIME,
                ONE_HOUR),
            new SimpleTimeZone(3 * ONE_HOUR, "EAT"),
            new SimpleTimeZone(4 * ONE_HOUR, "NET",
                Calendar.MARCH, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                ONE_HOUR),
            new SimpleTimeZone(5 * ONE_HOUR, "PLT"),
            new SimpleTimeZone(5 * ONE_HOUR + 1800000, "IST"),
            new SimpleTimeZone(6 * ONE_HOUR, "BST"),
            new SimpleTimeZone(7 * ONE_HOUR, "VST"),
            new SimpleTimeZone(8 * ONE_HOUR, "CTT"),
            new SimpleTimeZone(8 * ONE_HOUR, "PRC"),
            new SimpleTimeZone(9 * ONE_HOUR, "JST"),
            new SimpleTimeZone(9 * ONE_HOUR, "ROK"),
            new SimpleTimeZone(9 * ONE_HOUR + 1800000, "ACT"),
            new SimpleTimeZone(10 * ONE_HOUR, "AET",
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                Calendar.MARCH, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                ONE_HOUR),
            new SimpleTimeZone(11 * ONE_HOUR, "SST"),
            new SimpleTimeZone(12 * ONE_HOUR, "NST",
                Calendar.OCTOBER, 1, -Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                Calendar.MARCH, 15, -Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                ONE_HOUR),

            new SimpleTimeZone(-6 * ONE_HOUR, "America/Costa_Rica"),
            new SimpleTimeZone(-4 * ONE_HOUR, "America/Halifax",
                Calendar.APRIL, 1, -Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                ONE_HOUR),
            new SimpleTimeZone(-2 * ONE_HOUR, "Atlantic/South_Georgia"),
            new SimpleTimeZone(0 * ONE_HOUR, "Europe/London",
                Calendar.MARCH, -1, Calendar.SUNDAY, 1 * ONE_HOUR, SimpleTimeZone.UTC_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 1 * ONE_HOUR, SimpleTimeZone.UTC_TIME,
                ONE_HOUR),
            new SimpleTimeZone(1 * ONE_HOUR, "Africa/Algiers"),
            new SimpleTimeZone(2 * ONE_HOUR, "Africa/Cairo",
                Calendar.APRIL, -1, Calendar.FRIDAY, 0 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                Calendar.SEPTEMBER, -1, Calendar.THURSDAY, 23 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                ONE_HOUR),
            new SimpleTimeZone(2 * ONE_HOUR, "Africa/Harare"),
            new SimpleTimeZone(2 * ONE_HOUR, "Asia/Jerusalem",
                Calendar.APRIL, 1, 0, 1 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                Calendar.OCTOBER, 1, 0, 1 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                ONE_HOUR),
            new SimpleTimeZone(2 * ONE_HOUR, "Europe/Bucharest",
                Calendar.MARCH, -1, Calendar.SUNDAY, 1 * ONE_HOUR, SimpleTimeZone.UTC_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 1 * ONE_HOUR, SimpleTimeZone.UTC_TIME,
                ONE_HOUR),
            new SimpleTimeZone(3 * ONE_HOUR, "Europe/Moscow",
                Calendar.MARCH, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                ONE_HOUR),
            new SimpleTimeZone(3 * ONE_HOUR + 1800000, "Asia/Tehran",
                Calendar.MARCH, 21, 0, 0 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                Calendar.SEPTEMBER, 21, 0, 0 * ONE_HOUR, SimpleTimeZone.WALL_TIME,
                ONE_HOUR),
            new SimpleTimeZone(4 * ONE_HOUR + 1800000, "Asia/Kabul"),
            new SimpleTimeZone(9 * ONE_HOUR + 1800000, "Australia/Adelaide",
                Calendar.OCTOBER, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                Calendar.MARCH, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                ONE_HOUR),
            new SimpleTimeZone(10 * ONE_HOUR, "Australia/Brisbane"),
            new SimpleTimeZone(10 * ONE_HOUR, "Australia/Hobart",
                Calendar.OCTOBER, 1, -Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                Calendar.MARCH, -1, Calendar.SUNDAY, 2 * ONE_HOUR, SimpleTimeZone.STANDARD_TIME,
                ONE_HOUR),
        };

        foreach (var zone in zones)
        {
            zone.JavaClass = Jvm.Classes[typeof(TimeZone).ToJavaName()];
            Jvm.PutToHeap(zone);
        }

        return zones;
    }
}