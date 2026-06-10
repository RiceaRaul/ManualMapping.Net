using System.Linq.Expressions;
using ManualMapping.Abstractions;
using ManualMapping.Reflection;

namespace ManualMapping.Converters;

/// <summary>
/// A bidirectional converter where both directions auto-map fields with
/// matching name and assignment-compatible type via reflection.
///
/// Override <see cref="AutoTypeConverter{TSrc,TDest}.CustomExpression"/>
/// and <see cref="CustomReverseExpression"/> to bind fields that need
/// custom logic in the respective direction. Explicit bindings always win.
/// </summary>
public abstract class AutoBidirectionalConverter<TSrc, TDest>
    : AutoTypeConverter<TSrc, TDest>, IBidirectionalConverter<TSrc, TDest>
{
    private readonly Lazy<Expression<Func<TDest, TSrc>>> _builtReverse;
    private readonly Lazy<Func<TDest, TSrc>> _compiledReverse;

    protected AutoBidirectionalConverter()
    {
        _builtReverse = new Lazy<Expression<Func<TDest, TSrc>>>(
            () => AutoMapExpressionBuilder.Build<TDest, TSrc>(
                CustomReverseExpression(),
                IgnoredReverseProperties().ToHashSet(StringComparer.Ordinal) is { Count: > 0 } s ? s : null));
        _compiledReverse = new Lazy<Func<TDest, TSrc>>(() => AsReverseExpression().Compile());
    }

    /// <summary>
    /// Override to bind fields that cannot be auto-mapped in the reverse
    /// direction. Any property not bound here is auto-mapped from a
    /// same-named, assignment-compatible destination property.
    /// </summary>
    protected virtual IEnumerable<string> IgnoredReverseProperties() => [];

    protected virtual Expression<Func<TDest, TSrc>>? CustomReverseExpression() => null;

    public Expression<Func<TDest, TSrc>> AsReverseExpression() => _builtReverse.Value;

    public virtual TSrc ConvertBack(TDest source) => _compiledReverse.Value(source);
}
