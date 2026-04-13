using System.Linq.Expressions;

namespace ManualMapping.Abstractions;

public interface IMapper
{
    TDest Map<TSrc, TDest>(TSrc source);
    TDest Map<TDest>(object source);
    IQueryable<TDest> ProjectTo<TSrc, TDest>(IQueryable<TSrc> source);
    LambdaExpression GetProjectionExpression(Type srcType, Type destType);
}
