// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;

namespace java.util;

class GregorianCalendar : Calendar
{
    public const int
        BC = 0,
        AD = 1;

    private const long gregorianCutover = -12219292800000L;
    private const int changeYear = 1582;

    private static readonly int julianSkew = ((changeYear - 2000) / 400) + julianError() -
                                             ((changeYear - 2000) / 100);

    private int dst_offset;

    [JavaIgnore]
    public static sbyte[] DaysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    [JavaIgnore]
    private static int[] DaysInYear = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };

    private const int CACHED_YEAR = 0;
    private const int CACHED_MONTH = 1;
    private const int CACHED_DATE = 2;
    private const int CACHED_DAY_OF_WEEK = 3;
    private const int CACHED_ZONE_OFFSET = 4;
    private bool isCached;

    [JavaIgnore]
    private int[]? cachedFields;

    private long nextMidnightMillis;
    private long lastMidnightMillis;

    [InitMethod]
    public new void Init()
    {
        base.Init();
    }

    [InitMethod]
    public void Init(long milliseconds)
    {
        base.Init();
        setTimeInMillis(milliseconds);
    }

    [InitMethod]
    public void Init([JavaType(typeof(TimeZone))] Reference timezone)
    {
        base.Init();
        setTimeZone(timezone);
        setTimeInMillis(lang.System.currentTimeMillis());
    }

    private void fullFieldsCalc(long time, int orgMillis, int zoneOffset)
    {
        long days = time / 86400000;

        // Cannot add ZONE_OFFSET to time as it might overflow
        long zoneMillis = (long)orgMillis + zoneOffset;
        while (zoneMillis < 0)
        {
            zoneMillis += 86400000;
            days--;
        }

        while (zoneMillis >= 86400000)
        {
            zoneMillis -= 86400000;
            days++;
        }

        int millis = (int)zoneMillis;

        int dayOfYear = computeYearAndDay(days, time + zoneOffset);
        int month = dayOfYear / 32;
        bool leapYear = isLeapYear(fields[YEAR]);
        int date = dayOfYear - daysInYear(leapYear, month);
        if (date > daysInMonth(leapYear, month))
        {
            date -= daysInMonth(leapYear, month);
            month++;
        }

        fields[DAY_OF_WEEK] = mod7(days - 3) + 1;
        int dstOffset = fields[YEAR] <= 0
            ? 0
            : Jvm.Resolve<TimeZone>(Zone).getOffset(AD, fields[YEAR], month, date, fields[DAY_OF_WEEK], millis);
        if (fields[YEAR] > 0) dstOffset -= zoneOffset;
        dst_offset = dstOffset;
        if (dstOffset != 0)
        {
            long oldDays = days;
            millis += dstOffset;
            if (millis < 0)
            {
                millis += 86400000;
                days--;
            }
            else if (millis >= 86400000)
            {
                millis -= 86400000;
                days++;
            }

            if (oldDays != days)
            {
                dayOfYear = computeYearAndDay(days, time - zoneOffset + dstOffset);
                month = dayOfYear / 32;
                leapYear = isLeapYear(fields[YEAR]);
                date = dayOfYear - daysInYear(leapYear, month);
                if (date > daysInMonth(leapYear, month))
                {
                    date -= daysInMonth(leapYear, month);
                    month++;
                }

                fields[DAY_OF_WEEK] = mod7(days - 3) + 1;
            }
        }

        fields[MILLISECOND] = millis % 1000;
        millis /= 1000;
        fields[SECOND] = millis % 60;
        millis /= 60;
        fields[MINUTE] = millis % 60;
        millis /= 60;
        fields[HOUR_OF_DAY] = millis % 24;
        millis /= 24;
        fields[AM_PM] = fields[HOUR_OF_DAY] > 11 ? 1 : 0;
        fields[HOUR] = fields[HOUR_OF_DAY] % 12;

        if (fields[YEAR] <= 0)
            fields[YEAR] = -fields[YEAR] + 1;
        fields[MONTH] = month;
        fields[DATE] = date;
    }

    private void updateCachedFields()
    {
        cachedFields ??= new[] { 0, 0, 0, 0, 0, 0 }; // this probably will never fire but...
        fields[YEAR] = cachedFields[CACHED_YEAR];
        fields[MONTH] = cachedFields[CACHED_MONTH];
        fields[DATE] = cachedFields[CACHED_DATE];
        fields[DAY_OF_WEEK] = cachedFields[CACHED_DAY_OF_WEEK];
    }

    public override void computeFields()
    {
        actualComputeFields();
        for (int i = 0; i < FIELD_COUNT; i++) isSet[i] = true;
    }

    private void actualComputeFields()
    {
        var zone = Jvm.Resolve<TimeZone>(Zone);
        cachedFields ??= new[] { 0, 0, 0, 0, 0, 0 };
        int zoneOffset = zone.getRawOffset();

        int millis = (int)(time % 86400000);
        int savedMillis = millis;
        int dstOffset = dst_offset;
        int offset = zoneOffset + dstOffset; // compute without a change in daylight saving time
        long newTime = time + offset;

        if (time > 0L && newTime < 0L && offset > 0)
            newTime = 0x7fffffffffffffffL;
        else if (time < 0L && newTime > 0L && offset < 0)
            newTime = long.MinValue;

        if (zoneOffset != cachedFields[CACHED_ZONE_OFFSET] ||
            zoneOffset <= -86400000 || zoneOffset >= 86400000)
        {
            isCached = false;
        }

        if (isCached)
        {
            if (millis < 0)
            {
                millis += 86400000;
            }

            // Cannot add ZONE_OFFSET to time as it might overflow
            millis += zoneOffset;

            if (millis < 0)
            {
                millis += 86400000;
            }
            else if (millis >= 86400000)
            {
                millis -= 86400000;
            }

            if (isCached)
            {
                isCached = newTime < nextMidnightMillis && newTime > lastMidnightMillis;
            }

            if (isCached)
            {
                int dstSavings = 0;
                if (zone.useDaylightTime())
                {
                    dstSavings = cachedFields[CACHED_YEAR] <= 0
                        ? 0
                        : zone.getOffset(AD, cachedFields[CACHED_YEAR], cachedFields[CACHED_MONTH],
                            cachedFields[CACHED_DATE], cachedFields[CACHED_DAY_OF_WEEK], millis) - zoneOffset;
                }

                if (dstSavings != dstOffset) isCached = false;
            }

            if (isCached)
            {
                updateCachedFields();
                millis += dstOffset;
                fields[MILLISECOND] = millis % 1000;
                millis /= 1000;
                fields[SECOND] = millis % 60;
                millis /= 60;
                fields[MINUTE] = millis % 60;
                millis /= 60;
                fields[HOUR_OF_DAY] = millis % 24;
                millis /= 24;
                fields[AM_PM] = fields[HOUR_OF_DAY] > 11 ? 1 : 0;
                fields[HOUR] = fields[HOUR_OF_DAY] % 12;
            }
            else
            {
                fullFieldsCalc(time, savedMillis, zoneOffset);
            }
        }
        else
            fullFieldsCalc(time, savedMillis, zoneOffset);

        //Caching
        if (!isCached
            && newTime != 0x7fffffffffffffffL
            && newTime != long.MinValue
            && (!zone.useDaylightTime() || Jvm.Resolve<TimeZone>(Zone) is SimpleTimeZone))
        {
            int cacheMillis = 0;

            cachedFields[CACHED_YEAR] = fields[YEAR];
            cachedFields[CACHED_MONTH] = fields[MONTH];
            cachedFields[CACHED_DATE] = fields[DATE];
            cachedFields[CACHED_DAY_OF_WEEK] = fields[DAY_OF_WEEK];
            cachedFields[CACHED_ZONE_OFFSET] = zoneOffset;

            cacheMillis += (23 - fields[HOUR_OF_DAY]) * 60 * 60 * 1000;
            cacheMillis += (59 - fields[MINUTE]) * 60 * 1000;
            cacheMillis += (59 - fields[SECOND]) * 1000;
            nextMidnightMillis = newTime + cacheMillis;

            cacheMillis = fields[HOUR_OF_DAY] * 60 * 60 * 1000;
            cacheMillis += fields[MINUTE] * 60 * 1000;
            cacheMillis += fields[SECOND] * 1000;
            lastMidnightMillis = newTime - cacheMillis;

            isCached = true;
        }
    }

    public override void computeTime()
    {
        if (isSet[MONTH] && (fields[MONTH] < 0 || fields[MONTH] > 11))
            Jvm.Throw<IllegalArgumentException>();

        long time;
        int hour = 0;
        if (isSet[HOUR_OF_DAY] && lastTimeFieldSet != HOUR)
            hour = fields[HOUR_OF_DAY];
        else if (isSet[HOUR])
            hour = (fields[AM_PM] * 12) + fields[HOUR];
        time = hour * 3600000;

        if (isSet[MINUTE]) time += fields[MINUTE] * 60000;
        if (isSet[SECOND]) time += fields[SECOND] * 1000;
        if (isSet[MILLISECOND]) time += fields[MILLISECOND];

        long days;
        int year = isSet[YEAR] ? fields[YEAR] : 1970;
        lastTimeFieldSet = 0;
        time %= 86400000;
        int month = isSet[MONTH] ? fields[MONTH] : 0;
        bool leapYear = isLeapYear(year);
        days = daysFromBaseYear(year) + daysInYear(leapYear, month);
        if (isSet[DATE])
        {
            if (fields[DATE] < 1 || fields[DATE] > daysInMonth(leapYear, month))
                Jvm.Throw<IllegalArgumentException>();
            days += fields[DATE] - 1;
        }

        time += days * 86400000;
        // Use local time to compare with the gregorian change
        if (year == changeYear && time >= gregorianCutover + julianError() * 86400000)
            time -= julianError() * 86400000;
        time -= getOffset(time);
        this.time = time;
        if (!areFieldsSet)
        {
            actualComputeFields();
            areFieldsSet = true;
        }
    }

    private int computeYearAndDay(long dayCount, long localTime)
    {
        int year = 1970;
        long days = dayCount;
        if (localTime < gregorianCutover) days -= julianSkew;
        int approxYears;

        while ((approxYears = (int)(days / 365)) != 0)
        {
            year = year + approxYears;
            days = dayCount - daysFromBaseYear(year);
        }

        if (days < 0)
        {
            year = year - 1;
            days = days + 365 + (isLeapYear(year) ? 1 : 0);
            if (year == changeYear && localTime < gregorianCutover)
                days -= julianError();
        }

        fields[YEAR] = year;
        return (int)days + 1;
    }

    private long daysFromBaseYear(int year)
    {
        if (year >= 1970)
        {
            long days = (year - 1970) * (long)365 + ((year - 1969) / 4);
            if (year > changeYear)
                days -= ((year - 1901) / 100) - ((year - 1601) / 400);
            else days += julianSkew;
            return days;
        }

        if (year <= changeYear)
        {
            return (year - 1970) * (long)365 + ((year - 1972) / 4) + julianSkew;
        }

        return (year - 1970) * (long)365 + ((year - 1972) / 4) -
            ((year - 2000) / 100) + ((year - 2000) / 400);
    }

    private int daysInMonth(bool leapYear, int month)
    {
        if (leapYear && month == FEBRUARY) return DaysInMonth[month] + 1;
        return DaysInMonth[month];
    }

    private int daysInYear(bool leapYear, int month)
    {
        if (leapYear && month > FEBRUARY) return DaysInYear[month] + 1;
        return DaysInYear[month];
    }

    int getOffset(long localTime)
    {
        TimeZone timeZone = Jvm.Resolve<TimeZone>(Zone);
        if (!timeZone.useDaylightTime()) return timeZone.getRawOffset();

        long dayCount = localTime / 86400000;
        int millis = (int)(localTime % 86400000);
        if (millis < 0)
        {
            millis += 86400000;
            dayCount--;
        }

        int year = 1970;
        long days = dayCount;
        if (localTime < gregorianCutover) days -= julianSkew;
        int approxYears;

        while ((approxYears = (int)(days / 365)) != 0)
        {
            year = year + approxYears;
            days = dayCount - daysFromBaseYear(year);
        }

        if (days < 0)
        {
            year = year - 1;
            days = days + 365 + (isLeapYear(year) ? 1 : 0);
            if (year == changeYear && localTime < gregorianCutover)
                days -= julianError();
        }

        if (year <= 0) return timeZone.getRawOffset();
        int dayOfYear = (int)days + 1;

        int month = dayOfYear / 32;
        bool leapYear = isLeapYear(year);
        int date = dayOfYear - daysInYear(leapYear, month);
        if (date > daysInMonth(leapYear, month))
        {
            date -= daysInMonth(leapYear, month);
            month++;
        }

        int dayOfWeek = mod7(dayCount - 3) + 1;
        int offset = timeZone.getOffset(AD, year, month, date, dayOfWeek, millis);
        return offset;
    }

    bool isLeapYear(int year)
    {
        if (year > changeYear)
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        return year % 4 == 0;
    }

    private static int julianError()
    {
        return changeYear / 100 - changeYear / 400 - 2;
    }

    private int mod7(long num1)
    {
        int rem = (int)(num1 % 7);
        if (num1 < 0 && rem < 0) return rem + 7;
        return rem;
    }
}
