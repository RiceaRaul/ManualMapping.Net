using System.Linq.Expressions;
using System.Reflection;

namespace ManualMapping.Reflection;

/// <summary>
/// Builds a <c>src =&gt; new TDest { ... }</c> expression by merging an optional
/// user-supplied custom expression with reflection-based bindings for any
/// destination property that has a source property of the same name and an
/// assignment-compatible type. Explicit bindings from the custom expression
/// always win over reflected ones.
/// </summary>
internal static class AutoMapExpressionBuilder
{
    public static Expression<Func<TSrc, TDest>> Build<TSrc, TDest>(
        Expression<Func<TSrc, TDest>>? customExpression,
        IReadOnlySet<string>? ignoredProperties = null)
    {
        ParameterExpression srcParam;
        List<MemberBinding> bindings;
        HashSet<string> explicitlyMapped;

        if (customExpression is not null)
        {
            if (customExpression.Body is not MemberInitExpression memberInit
                || memberInit.NewExpression.Arguments.Count != 0)
            {
                throw new InvalidOperationException(
                    $"Custom expression for {typeof(TSrc).Name} → {typeof(TDest).Name} must be of the form " +
                    $"`src => new {typeof(TDest).Name} {{ ... }}` using the parameterless constructor.");
            }

            srcParam = customExpression.Parameters[0];
            bindings = [.. memberInit.Bindings];
            explicitlyMapped = new HashSet<string>(
                memberInit.Bindings.Select(b => b.Member.Name),
                StringComparer.Ordinal);
        }
        else
        {
            srcParam = Expression.Parameter(typeof(TSrc), "src");
            bindings = [];
            explicitlyMapped = new HashSet<string>(StringComparer.Ordinal);
        }

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        var srcProps = typeof(TSrc).GetProperties(flags);

        foreach (var destProp in typeof(TDest).GetProperties(flags))
        {
            if (!destProp.CanWrite) continue;
            if (explicitlyMapped.Contains(destProp.Name)) continue;
            if (ignoredProperties is not null && ignoredProperties.Contains(destProp.Name)) continue;

            var srcProp = Array.Find(srcProps, p => p.Name == destProp.Name);
            if (srcProp is null || !srcProp.CanRead) continue;

            if (!TryBuildAssignment(srcParam, srcProp, destProp, out var value))
                continue;

            bindings.Add(Expression.Bind(destProp, value));
        }

        var newExpr = Expression.New(typeof(TDest));
        var body = Expression.MemberInit(newExpr, bindings);
        return Expression.Lambda<Func<TSrc, TDest>>(body, srcParam);
    }

    private static bool TryBuildAssignment(
        ParameterExpression srcParam,
        PropertyInfo srcProp,
        PropertyInfo destProp,
        out Expression value)
    {
        Expression access = Expression.Property(srcParam, srcProp);
        var srcType = srcProp.PropertyType;
        var destType = destProp.PropertyType;

        if (destType.IsAssignableFrom(srcType))
        {
            value = srcType == destType ? access : Expression.Convert(access, destType);
            return true;
        }

        // T → Nullable<T>
        var destUnderlying = Nullable.GetUnderlyingType(destType);
        if (destUnderlying is not null && destUnderlying == srcType)
        {
            value = Expression.Convert(access, destType);
            return true;
        }

        value = null!;
        return false;
    }
}
