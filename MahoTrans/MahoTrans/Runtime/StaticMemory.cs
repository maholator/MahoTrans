// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using java.io;
using javax.microedition.lcdui;
using MahoTrans.Native;
using Boolean = java.lang.Boolean;

namespace MahoTrans.Runtime;

/// <summary>
///     Object where java static fields are stored. Used only for native code.
/// </summary>
public class StaticMemory
{
    public Reference RuntimeInstance;

    [NativeStatic("out", typeof(PrintStream), typeof(java.lang.System))]
    public Reference OutStream;

    [NativeStatic("err", typeof(PrintStream), typeof(java.lang.System))]
    public Reference ErrStream;

    [NativeStatic("SELECT_COMMAND", typeof(Command), typeof(List))]
    public Reference ListSelectCommand;

    [NativeStatic("DISMISS_COMMAND", typeof(Command), typeof(Alert))]
    public Reference AlertDismissCommand;

    [NativeStatic("ALARM", typeof(AlertType), typeof(AlertType))]
    public Reference AlarmAlertType;

    [NativeStatic("CONFIRMATION", typeof(AlertType), typeof(AlertType))]
    public Reference ConfirmationAlertType;

    [NativeStatic("ERROR", typeof(AlertType), typeof(AlertType))]
    public Reference ErrorAlertType;

    [NativeStatic("INFO", typeof(AlertType), typeof(AlertType))]
    public Reference InfoAlertType;

    [NativeStatic("WARNING", typeof(AlertType), typeof(AlertType))]
    public Reference WarningAlertType;

    public Reference Graphics3DInstance;

    public Dictionary<string, Reference> OpenedRecordStores = null!;

    public Reference DefaultTimeZone;
    public Reference GmtTimeZone;
    public Dictionary<string, Reference> AvailableZones = null!;

    [NativeStatic("TRUE", typeof(Boolean), typeof(Boolean))]
    public Reference True;

    [NativeStatic("FALSE", typeof(Boolean), typeof(Boolean))]
    public Reference False;

    /// <summary>
    ///     Gets all references in static memory. Used by GC.
    /// </summary>
    /// <returns>List with all stored references.</returns>
    public List<Reference> GetAll()
    {
        var list = new List<Reference>(1024);

        list.Add(RuntimeInstance);

        list.Add(OutStream);
        list.Add(ErrStream);

        list.Add(ListSelectCommand);
        list.Add(AlertDismissCommand);

        list.Add(AlarmAlertType);
        list.Add(ConfirmationAlertType);
        list.Add(ErrorAlertType);
        list.Add(InfoAlertType);
        list.Add(WarningAlertType);

        list.Add(Graphics3DInstance);

        if (OpenedRecordStores != null!)
            list.AddRange(OpenedRecordStores.Values);

        list.Add(DefaultTimeZone);
        list.Add(GmtTimeZone);
        if (AvailableZones != null!)
            list.AddRange(AvailableZones.Values);

        list.Add(True);
        list.Add(False);

        return list;
    }

    private static Slot[]? _fieldsCache;

    public static Slot[] Fields
    {
        get
        {
            _fieldsCache ??= typeof(StaticMemory).GetFields().SelectMany(x =>
            {
                var attr = x.GetCustomAttribute<NativeStaticAttribute>();
                if (attr == null)
                    return Enumerable.Empty<Slot>();

                return new[] { new Slot(x, attr.OwnerType) };
            }).ToArray();

            return _fieldsCache;
        }
    }

    public struct Slot
    {
        public FieldInfo Field;
        public Type Owner;

        public Slot(FieldInfo field, Type owner)
        {
            Field = field;
            Owner = owner;
        }
    }
}
