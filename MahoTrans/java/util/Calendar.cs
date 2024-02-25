// Copyright (c) Fyodor Ryzhov. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Native;
using MahoTrans.Runtime;
using Object = java.lang.Object;

namespace java.util;

public class Calendar : Object
{
    public bool areFieldsSet;

    [JavaIgnore] protected int[] fields = null!;

    [JavaIgnore] protected bool[] isSet = null!;

    bool isTimeSet;

    protected long time;

    protected int lastTimeFieldSet;
    [JavaType(typeof(TimeZone))] public Reference Zone;

    public const int
        JANUARY = 0,
        FEBRUARY = 1,
        MARCH = 2,
        APRIL = 3,
        MAY = 4,
        JUNE = 5,
        JULY = 6,
        AUGUST = 7,
        SEPTEMBER = 8,
        OCTOBER = 9,
        NOVEMBER = 10,
        DECEMBER = 11,
        SUNDAY = 1,
        MONDAY = 2,
        TUESDAY = 3,
        WEDNESDAY = 4,
        THURSDAY = 5,
        FRIDAY = 6,
        SATURDAY = 7;

    public const int
        YEAR = 1,
        MONTH = 2,
        DATE = 5,
        DAY_OF_MONTH = 5,
        DAY_OF_WEEK = 7,
        AM_PM = 9,
        HOUR = 10,
        HOUR_OF_DAY = 11,
        MINUTE = 12,
        SECOND = 13,
        MILLISECOND = 14,
        AM = 0,
        PM = 1;

    public const int FIELD_COUNT = 15;

    [InitMethod]
    public new void Init()
    {
        isSet = new bool[FIELD_COUNT];
        fields = new int[FIELD_COUNT];
        setTimeZone(TimeZone.getDefault());
        setTimeInMillis(lang.System.currentTimeMillis());
    }

    public bool after(Reference calendar)
    {
        if (Jvm.ResolveObject(calendar) is Calendar c)
        {
            return getTimeInMillis() > c.getTimeInMillis();
        }

        return false;
    }

    public bool before(Reference calendar)
    {
        if (Jvm.ResolveObject(calendar) is Calendar c)
        {
            return getTimeInMillis() < c.getTimeInMillis();
        }

        return false;
    }

    void complete()
    {
        if (!isTimeSet)
        {
            computeTime();
            isTimeSet = true;
        }

        if (areFieldsSet)
        {
            for (int i = 0; i < isSet.Length; i++)
                isSet[i] = true;
        }
        else
        {
            computeFields();
            areFieldsSet = true;
        }
    }

    public virtual void computeFields() => throw new AbstractJavaMethodCallError();

    public virtual void computeTime() => throw new AbstractJavaMethodCallError();

    public new bool equals(Reference obj)
    {
        if (This == obj) return true;
        var r = Jvm.ResolveObject(obj);
        if (r is Calendar cal)
        {
            return getTimeInMillis() == cal.getTimeInMillis() &&
                   Jvm.Resolve<TimeZone>(Zone).equals(cal.getTimeZone());
        }

        return false;
    }

    public int get(int field)
    {
        if (field >= DAY_OF_WEEK)
            complete();
        return fields[field];
    }

    [return: JavaType(typeof(Calendar))]
    public static Reference getInstance()
    {
        var c = Jvm.Allocate<GregorianCalendar>();
        c.Init();
        return c.This;
    }


    [return: JavaType(typeof(Calendar))]
    public static Reference getInstance([JavaType(typeof(TimeZone))] Reference timezone)
    {
        var c = Jvm.Allocate<GregorianCalendar>();
        c.Init(timezone);
        return c.This;
    }

    [return: JavaType(typeof(Date))]
    public Reference getTime()
    {
        var d = Jvm.Allocate<Date>();
        d.Init(getTimeInMillis());
        return d.This;
    }

    public long getTimeInMillis()
    {
        if (!isTimeSet)
        {
            computeTime();
            isTimeSet = true;
        }

        return time;
    }


    [return: JavaType(typeof(TimeZone))]
    public Reference getTimeZone()
    {
        return Zone;
    }


    public new int hashCode()
    {
        long t = getTimeInMillis();
        return Jvm.Resolve<TimeZone>(Zone).hashCode() + (int)(t >> 32) + (int)t;
    }

    public void set(int field, int value)
    {
        if (field == HOUR || field == HOUR_OF_DAY || fields[field] != value)
        {
            fields[field] = value;
            areFieldsSet = false;
            if (field == HOUR || field == HOUR_OF_DAY) lastTimeFieldSet = field;
            isSet[field] = true;
            isTimeSet = false;
        }
    }

    public void setTime([JavaType(typeof(Date))] Reference date)
    {
        setTimeInMillis(Jvm.Resolve<Date>(date).getTime());
    }

    protected void setTimeInMillis(long milliseconds)
    {
        time = milliseconds;
        isTimeSet = true;
        areFieldsSet = false;
        complete();
    }

    public void setTimeZone([JavaType(typeof(TimeZone))] Reference timezone)
    {
        Zone = timezone;
    }
}