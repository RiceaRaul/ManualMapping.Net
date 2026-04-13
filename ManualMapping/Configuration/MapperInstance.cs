using System.Linq.Expressions;
using ManualMapping.Abstractions;

namespace ManualMapping.Configuration;

internal sealed class MapperInstance : IMapper
{
    private readonly Dictionary<(Type, Type), Func<object, object>> _untypedDelegates;
    private readonly Dictionary<(Type, Type), Delegate>             _typedDelegates;
    private readonly Dictionary<(Type, Type), LambdaExpression>     _expressions;

    internal MapperInstance(
        Dictionary<(Type, Type), Func<object, object>> untypedDelegates,
        Dictionary<(Type, Type), Delegate>             typedDelegates,
        Dictionary<(Type, Type), LambdaExpression>     expressions)
    {
        _untypedDelegates = untypedDelegates;
        _typedDelegates   = typedDelegates;
        _expressions      = expressions;
    }

    // ── Typed path: zero boxing ──────────────────────────────

    public TDest Map<TSrc, TDest>(TSrc source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ResolveTyped<TSrc, TDest>()(source);
    }

    private Func<TSrc, TDest> ResolveTyped<TSrc, TDest>()
    {
        var key = (typeof(TSrc), typeof(TDest));
        if (!_typedDelegates.TryGetValue(key, out var del))
            throw new InvalidOperationException(
                $"No map registered for {key.Item1.Name} → {key.Item2.Name}.");
        return (Func<TSrc, TDest>)del;
    }

    // ── Untyped path: Map<TDest>(object) + collection detection ─

    public TDest Map<TDest>(object source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var destType = typeof(TDest);
        if (source is System.Collections.IEnumerable enumerable
            && destType != typeof(string)
            && TryGetEnumerableElementType(destType, out var destElementType))
        {
            var srcElementType = GetSourceElementType(enumerable);
            if (srcElementType != null)
                return (TDest)ResolveCollection(srcElementType, destElementType, destType, enumerable);
        }

        return (TDest)ResolveUntyped((source.GetType(), typeof(TDest)), source);
    }

    private object ResolveUntyped(in (Type, Type) key, object source)
    {
        if (!_untypedDelegates.TryGetValue(key, out var fn))
            throw new InvalidOperationException(
                $"No map registered for {key.Item1.Name} → {key.Item2.Name}.");
        return fn(source);
    }

    private static Type? GetSourceElementType(System.Collections.IEnumerable source)
    {
        foreach (var iface in source.GetType().GetInterfaces())
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        return null;
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }
        if (type.IsGenericType)
        {
            var genDef = type.GetGenericTypeDefinition();
            if (genDef == typeof(IEnumerable<>) || genDef == typeof(List<>)
                || genDef == typeof(IList<>) || genDef == typeof(ICollection<>)
                || genDef == typeof(IReadOnlyList<>) || genDef == typeof(IReadOnlyCollection<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
        }
        foreach (var iface in type.GetInterfaces())
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = iface.GetGenericArguments()[0];
                return true;
            }
        elementType = default!;
        return false;
    }

    private object ResolveCollection(
        Type srcElementType, Type destElementType, Type destCollectionType,
        System.Collections.IEnumerable source)
    {
        var key = (srcElementType, destElementType);
        if (!_untypedDelegates.TryGetValue(key, out var fn))
            throw new InvalidOperationException(
                $"No map registered for {srcElementType.Name} → {destElementType.Name}.");

        var listType = typeof(List<>).MakeGenericType(destElementType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
        foreach (var item in source)
            list.Add(fn(item!));

        if (destCollectionType.IsArray)
        {
            var array = Array.CreateInstance(destElementType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }
        return list;
    }

    // ── ProjectTo ────────────────────────────────────────────

    public IQueryable<TDest> ProjectTo<TSrc, TDest>(IQueryable<TSrc> source)
    {
        var key = (typeof(TSrc), typeof(TDest));
        if (!_expressions.TryGetValue(key, out var expr))
            throw new InvalidOperationException(
                $"ProjectTo not available for {key.Item1.Name} → {key.Item2.Name}. " +
                "Register with withProjectTo: true.");

        return source.Provider.CreateQuery<TDest>(
            Expression.Call(
                typeof(Queryable), nameof(Queryable.Select),
                [typeof(TSrc), typeof(TDest)],
                source.Expression, expr));
    }

    public LambdaExpression GetProjectionExpression(Type srcType, Type destType)
    {
        var key = (srcType, destType);
        if (!_expressions.TryGetValue(key, out var expr))
            throw new InvalidOperationException(
                $"ProjectTo not available for {srcType.Name} → {destType.Name}. " +
                "Register with withProjectTo: true.");
        return expr;
    }
}
