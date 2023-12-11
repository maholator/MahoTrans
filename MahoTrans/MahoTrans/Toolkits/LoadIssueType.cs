using System.ComponentModel;

namespace MahoTrans.Toolkits;

public enum LoadIssueType
{
    [Description("Method doesn't return")] MethodWithoutReturn,

    [Description("Invalid local")] LocalVariableIndexOutOfBounds,

    /// <summary>
    /// There is an access to class field or method, this class is going to be casted into or instantiated, but this class is not available.
    /// </summary>
    [Description("Missing class access")] MissingClassAccess,

    /// <summary>
    /// There is an access to existing class' method, but this method is not available.
    /// </summary>
    [Description("Missing method access")] MissingMethodAccess,

    /// <summary>
    /// There is an access to existing class' field, but this field is not available.
    /// </summary>
    [Description("Missing field access")] MissingFieldAccess,

    [Description("Field of missing class")]
    MissingClassField,

    [Description("Missing class extension")]
    MissingClassSuper,

    /// <summary>
    /// Constant has unexpected type.
    /// </summary>
    [Description("Invalid constant")] InvalidConstant,

    /// <summary>
    /// Jar package has no manifest.
    /// </summary>
    [Description("Missing MANIFEST.MF")] NoMetaInf,

    [Description("Invalid magic")] InvalidClassMagicCode,

    [Description("Multitype local")] MultiTypeLocalVariable,
}