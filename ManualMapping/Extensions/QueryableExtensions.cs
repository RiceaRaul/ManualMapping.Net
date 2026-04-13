using System.Linq.Expressions;
using ManualMapping.Abstractions;

namespace ManualMapping.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<TDest> ProjectTo<TDest>(
        this IQueryable source, IMapper mapper)
    {
        var srcType = source.ElementType;
        var expr = mapper.GetProjectionExpression(srcType, typeof(TDest));

        return source.Provider.CreateQuery<TDest>(
            Expression.Call(
                typeof(Queryable), nameof(Queryable.Select),
                [srcType, typeof(TDest)],
                source.Expression, expr));
    }
}
