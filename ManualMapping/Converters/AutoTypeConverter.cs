using System.Linq.Expressions;
using ManualMapping.Reflection;

namespace ManualMapping.Converters;

/// <summary>
/// A converter that auto-maps destination properties from source properties
/// with the same name and an assignment-compatible type, via reflection.
///
/// Override <see cref="CustomExpression"/> to specify bindings for fields
/// that need custom logic — those bindings always win over auto-mapped ones.
/// Return <c>null</c> to auto-map every matching field.
///
/// The final expression is a single <c>MemberInit</c>, so ProjectTo still
/// translates to SQL (no per-call reflection).
/// </summary>
public abstract class AutoTypeConverter<TSrc, TDest> : TypeConverter<TSrc, TDest>
{
    private readonly Lazy<Expression<Func<TSrc, TDest>>> _built;

    protected AutoTypeConverter()
        => _built = new Lazy<Expression<Func<TSrc, TDest>>>(
            () => AutoMapExpressionBuilder.Build<TSrc, TDest>(CustomExpression()));

    /// <summary>
    /// Override to bind fields that cannot be auto-mapped.
    /// Any property not bound here is auto-mapped from a same-named,
    /// assignment-compatible source property.
    /// </summary>
    protected virtual Expression<Func<TSrc, TDest>>? CustomExpression() => null;

    public sealed override Expression<Func<TSrc, TDest>> AsExpression() => _built.Value;
}
