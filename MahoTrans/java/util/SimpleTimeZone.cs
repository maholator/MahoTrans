using java.lang;
using MahoTrans.Native;
using MahoTrans.Runtime;
using String = System.String;

namespace java.util;

public class SimpleTimeZone : TimeZone
{
    public string ID;
    private int rawOffset;
    private int startYear, startMonth, startDay, startDayOfWeek, startTime;
    private int endMonth, endDay, endDayOfWeek, endTime;
    private int startMode, endMode;
    private int startTimeMode, endTimeMode;

    private const int
        DOM_MODE = 1,
        DOW_IN_MONTH_MODE = 2,
        DOW_GE_DOM_MODE = 3,
        DOW_LE_DOM_MODE = 4;

    public static readonly int UTC_TIME = 2;

    public static readonly int STANDARD_TIME = 1;

    public static readonly int WALL_TIME = 0;

    private bool useDaylight;
    private int dstSavings = 3600000;

    // for serializer
    public SimpleTimeZone()
    {
    }

    public SimpleTimeZone(int offset, String name)
    {
        ID = name;
        rawOffset = offset;
    }

    public SimpleTimeZone(int offset, string name, int startMonth, int startDay,
        int startDayOfWeek, int startTime, int endMonth, int endDay, int endDayOfWeek, int endTime)
        : this(offset, name, startMonth, startDay, startDayOfWeek, startTime,
            endMonth, endDay, endDayOfWeek, endTime, 3600000)
    {
    }

    public SimpleTimeZone(int offset, string name, int startMonth, int startDay,
        int startDayOfWeek, int startTime, int endMonth, int endDay, int endDayOfWeek,
        int endTime, int daylightSavings)
        : this(offset, name)
    {
        if (daylightSavings <= 0)
            Jvm.Throw<IllegalArgumentException>();
        dstSavings = daylightSavings;

        setStartRule(startMonth, startDay, startDayOfWeek, startTime);
        setEndRule(endMonth, endDay, endDayOfWeek, endTime);
    }

    public SimpleTimeZone(int offset, string name, int startMonth,
        int startDay, int startDayOfWeek, int startTime, int startTimeMode,
        int endMonth, int endDay, int endDayOfWeek, int endTime,
        int endTimeMode, int daylightSavings)
        : this(offset, name)
    {
        if (daylightSavings <= 0)
            Jvm.Throw<IllegalArgumentException>();
        dstSavings = daylightSavings;

        setStartRule(startMonth, startDay, startDayOfWeek, startTime);
        setEndRule(endMonth, endDay, endDayOfWeek, endTime);

        if (startTimeMode > UTC_TIME || startTimeMode < WALL_TIME)
            Jvm.Throw<IllegalArgumentException>();

        this.startTimeMode = startTimeMode;

        if (endTimeMode > UTC_TIME || endTimeMode < WALL_TIME)
            Jvm.Throw<IllegalArgumentException>();

        this.endTimeMode = endTimeMode;
    }

    public int getDSTSavings()
    {
        return dstSavings;
    }


    public override int getOffset(int era, int year, int month, int day, int dayOfWeek, int time)
    {
        if (era != GregorianCalendar.BC && era != GregorianCalendar.AD)
            Jvm.Throw<IllegalArgumentException>();
        checkRange(month, dayOfWeek, time);
        if (month != Calendar.FEBRUARY || day != 29 || !isLeapYear(year))
            checkDay(month, day);

        int previousMonth = month - 1;
        if (previousMonth < Calendar.JANUARY)
            previousMonth = Calendar.DECEMBER;

        if (!useDaylightTime() || era != GregorianCalendar.AD || year < startYear)
            return rawOffset;

        bool afterStart = false, beforeEnd = false;

        int ruleDay = 0, firstDayOfMonth = mod7(dayOfWeek - day);
        int ruleTime = startTime;
        if (startTimeMode == UTC_TIME) ruleTime += rawOffset;
        int startRuleMonth = startMonth;
        int prevStartMonth = startMonth - 1;
        if (prevStartMonth < Calendar.JANUARY) prevStartMonth = Calendar.DECEMBER;
        int daysInPrevStartMonth = GregorianCalendar.DaysInMonth[prevStartMonth];
        if (prevStartMonth == Calendar.FEBRUARY && isLeapYear(year)) daysInPrevStartMonth++;
        if (month == startMonth || (ruleTime < 0 && month == prevStartMonth && day == daysInPrevStartMonth) ||
            (ruleTime >= 86400000 && previousMonth == startMonth && day == 1))
        {
            int daysInStartMonth = -1;
            switch (startMode)
            {
                case DOM_MODE:
                    ruleDay = startDay;
                    break;
                case DOW_IN_MONTH_MODE:
                    if (startDay >= 0)
                    {
                        ruleDay = mod7(startDayOfWeek - firstDayOfMonth) + 1 +
                                  (startDay - 1) * 7;
                    }
                    else
                    {
                        daysInStartMonth = GregorianCalendar.DaysInMonth[startMonth];
                        if (startMonth == Calendar.FEBRUARY && isLeapYear(year)) daysInStartMonth++;
                        ruleDay = daysInStartMonth + 1 +
                                  mod7(startDayOfWeek - (firstDayOfMonth + daysInStartMonth)) +
                                  startDay * 7;
                    }

                    break;
                case DOW_GE_DOM_MODE:
                    ruleDay = startDay +
                              mod7(startDayOfWeek - (firstDayOfMonth + startDay - 1));
                    break;
                case DOW_LE_DOM_MODE:
                    ruleDay = startDay +
                              mod7(startDayOfWeek - (firstDayOfMonth + startDay - 1));
                    if (ruleDay != startDay) ruleDay -= 7;
                    break;
            }

            if (ruleTime < 0)
            {
                ruleDay--;
                ruleTime += 86400000;
                if (ruleDay < 1)
                {
                    if (--startRuleMonth < Calendar.JANUARY)
                        startRuleMonth = Calendar.DECEMBER;
                    ruleDay = daysInPrevStartMonth;
                }
            }

            if (ruleTime >= 86400000)
            {
                ruleDay++;
                ruleTime -= 86400000;
                if (daysInStartMonth == -1)
                {
                    daysInStartMonth = GregorianCalendar.DaysInMonth[startMonth];
                    if (startMonth == Calendar.FEBRUARY && isLeapYear(year)) daysInStartMonth++;
                }

                if (ruleDay > daysInStartMonth)
                {
                    if (++startRuleMonth > Calendar.DECEMBER)
                        startRuleMonth = Calendar.JANUARY;
                    ruleDay = 1;
                }
            }

            if (month == startRuleMonth)
            {
                if (day > ruleDay || day == ruleDay && time >= ruleTime)
                {
                    afterStart = true;
                }
            }
        }

        ruleTime = endTime;
        if (endTimeMode == WALL_TIME) ruleTime -= dstSavings;
        else if (endTimeMode == UTC_TIME) ruleTime += rawOffset;
        int endRuleMonth = endMonth;
        int prevEndMonth = endMonth - 1;
        if (prevEndMonth < Calendar.JANUARY) prevEndMonth = Calendar.DECEMBER;
        int daysInPrevEndMonth = GregorianCalendar.DaysInMonth[prevEndMonth];
        if (prevEndMonth == Calendar.FEBRUARY && isLeapYear(year)) daysInPrevEndMonth++;
        if (month == endMonth || (ruleTime < 0 && month == prevEndMonth && day == daysInPrevEndMonth) ||
            (ruleTime > 86400000 && previousMonth == endMonth && day == 1))
        {
            int daysInEndMonth = -1;
            switch (endMode)
            {
                case DOM_MODE:
                    ruleDay = endDay;
                    break;
                case DOW_IN_MONTH_MODE:
                    if (endDay >= 0)
                    {
                        ruleDay = mod7(endDayOfWeek - firstDayOfMonth) + 1 +
                                  (endDay - 1) * 7;
                    }
                    else
                    {
                        daysInEndMonth = GregorianCalendar.DaysInMonth[endMonth];
                        if (endMonth == Calendar.FEBRUARY && isLeapYear(year)) daysInEndMonth++;
                        ruleDay = daysInEndMonth + 1 +
                                  mod7(endDayOfWeek - (firstDayOfMonth + daysInEndMonth)) +
                                  endDay * 7;
                    }

                    break;
                case DOW_GE_DOM_MODE:
                    ruleDay = endDay +
                              mod7(endDayOfWeek - (firstDayOfMonth + endDay - 1));
                    break;
                case DOW_LE_DOM_MODE:
                    ruleDay = endDay +
                              mod7(endDayOfWeek - (firstDayOfMonth + endDay - 1));
                    if (ruleDay != endDay) ruleDay -= 7;
                    break;
            }

            if (ruleTime < 0)
            {
                ruleDay--;
                ruleTime += 86400000;
                if (ruleDay < 1)
                {
                    if (--endRuleMonth < Calendar.JANUARY)
                        endRuleMonth = Calendar.DECEMBER;
                    ruleDay = daysInPrevEndMonth;
                }
            }

            if (ruleTime >= 86400000)
            {
                ruleDay++;
                ruleTime -= 86400000;
                if (daysInEndMonth == -1)
                {
                    daysInEndMonth = GregorianCalendar.DaysInMonth[endMonth];
                    if (endMonth == Calendar.FEBRUARY && isLeapYear(year)) daysInEndMonth++;
                }

                if (ruleDay > daysInEndMonth)
                {
                    if (++endRuleMonth > Calendar.DECEMBER)
                        endRuleMonth = Calendar.JANUARY;
                    ruleDay = 1;
                }
            }

            if (month == endRuleMonth)
            {
                if (day < ruleDay || day == ruleDay && time < ruleTime)
                {
                    beforeEnd = true;
                }
            }
        }

        if (endRuleMonth >= startRuleMonth)
        {
            if ((afterStart || month > startRuleMonth) && (beforeEnd || month < endRuleMonth))
                return rawOffset + dstSavings;
        }
        else
        {
            if ((afterStart || month > startRuleMonth || month <= endRuleMonth) &&
                (beforeEnd || month < endRuleMonth || month >= startRuleMonth))
                return rawOffset + dstSavings;
        }

        return rawOffset;
    }

    public override int getRawOffset()
    {
        return rawOffset;
    }

    private bool isLeapYear(int year)
    {
        if (year > 1582)
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        else
            return year % 4 == 0;
    }

    private int mod7(int num1)
    {
        int rem = num1 % 7;
        return (num1 < 0 && rem < 0) ? 7 + rem : rem;
    }

    private void checkRange(int month, int dayOfWeek, int time)
    {
        if (month < Calendar.JANUARY || month > Calendar.DECEMBER)
            Jvm.Throw<IllegalArgumentException>();
        if (dayOfWeek < Calendar.SUNDAY || dayOfWeek > Calendar.SATURDAY)
            Jvm.Throw<IllegalArgumentException>();
        if (time < 0 || time >= 24 * 3600000)
            Jvm.Throw<IllegalArgumentException>();
    }

    private void checkDay(int month, int day)
    {
        if (day <= 0 || day > GregorianCalendar.DaysInMonth[month])
            Jvm.Throw<IllegalArgumentException>();
    }

    private void setEndMode()
    {
        if (endDayOfWeek == 0) endMode = DOM_MODE;
        else if (endDayOfWeek < 0)
        {
            endDayOfWeek = -endDayOfWeek;
            if (endDay < 0)
            {
                endDay = -endDay;
                endMode = DOW_LE_DOM_MODE;
            }
            else endMode = DOW_GE_DOM_MODE;
        }
        else endMode = DOW_IN_MONTH_MODE;

        useDaylight = startDay != 0 && endDay != 0;
        if (endDay != 0)
        {
            checkRange(endMonth, endMode == DOM_MODE ? 1 : endDayOfWeek, endTime);
            if (endMode != DOW_IN_MONTH_MODE)
                checkDay(endMonth, endDay);
            else
            {
                if (endDay < -5 || endDay > 5)
                    Jvm.Throw<IllegalArgumentException>();
            }
        }

        if (endMode != DOM_MODE) endDayOfWeek--;
    }

    public void setEndRule(int month, int dayOfMonth, int time)
    {
        endMonth = month;
        endDay = dayOfMonth;
        endDayOfWeek = 0; // Initialize this value for hasSameRules()
        endTime = time;
        endTimeMode = WALL_TIME;
        setEndMode();
    }

    public void setEndRule(int month, int day, int dayOfWeek, int time)
    {
        endMonth = month;
        endDay = day;
        endDayOfWeek = dayOfWeek;
        endTime = time;
        endTimeMode = WALL_TIME;
        setEndMode();
    }

    public void setEndRule(int month, int day, int dayOfWeek, int time, bool after)
    {
        endMonth = month;
        endDay = after ? day : -day;
        endDayOfWeek = -dayOfWeek;
        endTime = time;
        endTimeMode = WALL_TIME;
        setEndMode();
    }

    private void setStartMode()
    {
        if (startDayOfWeek == 0) startMode = DOM_MODE;
        else if (startDayOfWeek < 0)
        {
            startDayOfWeek = -startDayOfWeek;
            if (startDay < 0)
            {
                startDay = -startDay;
                startMode = DOW_LE_DOM_MODE;
            }
            else startMode = DOW_GE_DOM_MODE;
        }
        else startMode = DOW_IN_MONTH_MODE;

        useDaylight = startDay != 0 && endDay != 0;
        if (startDay != 0)
        {
            checkRange(startMonth, startMode == DOM_MODE ? 1 : startDayOfWeek, startTime);
            if (startMode != DOW_IN_MONTH_MODE)
                checkDay(startMonth, startDay);
            else
            {
                if (startDay < -5 || startDay > 5)
                    Jvm.Throw<IllegalArgumentException>();
            }
        }

        if (startMode != DOM_MODE) startDayOfWeek--;
    }

    public void setStartRule(int month, int dayOfMonth, int time)
    {
        startMonth = month;
        startDay = dayOfMonth;
        startDayOfWeek = 0; // Initialize this value for hasSameRules()
        startTime = time;
        startTimeMode = WALL_TIME;
        setStartMode();
    }

    public void setStartRule(int month, int day, int dayOfWeek, int time)
    {
        startMonth = month;
        startDay = day;
        startDayOfWeek = dayOfWeek;
        startTime = time;
        startTimeMode = WALL_TIME;
        setStartMode();
    }

    public void setStartRule(int month, int day, int dayOfWeek, int time, bool after)
    {
        startMonth = month;
        startDay = after ? day : -day;
        startDayOfWeek = -dayOfWeek;
        startTime = time;
        startTimeMode = WALL_TIME;
        setStartMode();
    }

    public override bool useDaylightTime()
    {
        return useDaylight;
    }

    [return: String]
    public override Reference getID()
    {
        return Jvm.InternalizeString(ID);
    }
}