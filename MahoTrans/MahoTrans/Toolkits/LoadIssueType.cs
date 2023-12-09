namespace MahoTrans.Toolkits;

public enum LoadIssueType
{
    /// <summary>
    /// There is an access to class field or method, this class is going to be casted into or instantiated, but this class is not available.
    /// </summary>
    MissingClassAccess,

    /// <summary>
    /// There is an access to existing class' method, but this method is not available.
    /// </summary>
    MissingMethodAccess,

    /// <summary>
    /// There is an access to existing class' field, but this field is not available.
    /// </summary>
    MissingFieldAccess,

    /// <summary>
    /// Constant has unexpected type.
    /// </summary>
    BrokenConstant,

    /// <summary>
    /// Jar package has no manifest.
    /// </summary>
    NoMetaInf,

    InvalidClassMagicCode,

    MissingClassSuper,

    MissingClassField,

    LocalVariableIndexOutOfBounds,

    MultitypeLocalVariable,

}