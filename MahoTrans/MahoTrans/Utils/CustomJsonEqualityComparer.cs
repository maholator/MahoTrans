using System.Collections;

namespace MahoTrans.Utils;

public class CustomJsonEqualityComparer : IEqualityComparer
{
    public static readonly CustomJsonEqualityComparer Instance = new CustomJsonEqualityComparer();

    // Use ImmutableHashSet in later .net versions
    private static readonly HashSet<string> NaughtyTypes = new HashSet<string>
    {
        "System.Reflection.Emit.InternalAssemblyBuilder",
        "System.Reflection.Emit.InternalModuleBuilder"
    };

    private static readonly IEqualityComparer BaseComparer = EqualityComparer<object>.Default;

    static bool HasBrokenEquals(Type type)
    {
        return NaughtyTypes.Contains(type.FullName!);
    }

    #region IEqualityComparer Members

    public new bool Equals(object? x, object? y)
    {
        // Check reference equality
        if (ReferenceEquals(x, y))
            return true;
        // Check null
        if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
            return false;

        var xType = x.GetType();
        if (xType != y.GetType())
            // Types should be identical.
            // Note this check alone might be sufficient to fix the problem.
            return false;

        if (xType.IsClass && !xType.IsPrimitive) // IsPrimitive check for performance
        {
            if (HasBrokenEquals(xType))
            {
                // These naughty types should ONLY be compared via reference equality -- which we have already done.
                // So return false
                return false;
            }
        }

        return BaseComparer.Equals(x, y);
    }

    public int GetHashCode(object obj)
    {
        return BaseComparer.GetHashCode(obj);
    }

    #endregion
}