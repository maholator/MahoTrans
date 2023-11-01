using System.Collections;

namespace MahoTrans.Utils;

public class CustomJsonEqualityComparer : IEqualityComparer
{
    public static readonly CustomJsonEqualityComparer Instance = new CustomJsonEqualityComparer();

    // Use ImmutableHashSet in later .net versions
    static readonly HashSet<string> naughtyTypes = new HashSet<string>
    {
        "System.Reflection.Emit.InternalAssemblyBuilder",
        "System.Reflection.Emit.InternalModuleBuilder"
    };

    static readonly IEqualityComparer baseComparer = EqualityComparer<object>.Default;

    static bool HasBrokenEquals(Type type)
    {
        return naughtyTypes.Contains(type.FullName);
    }

    #region IEqualityComparer Members

    public bool Equals(object x, object y)
    {
        // Check reference equality
        if ((object)x == y)
            return true;
        // Check null
        else if ((object)x == null || (object)y == null)
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
        return baseComparer.Equals(x, y);
    }

    public int GetHashCode(object obj)
    {
        return baseComparer.GetHashCode(obj);
    }

    #endregion
}