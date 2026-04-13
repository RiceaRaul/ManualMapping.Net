using System.Linq.Expressions;

namespace ManualMapping.Abstractions;

public interface ITypeConverter<TSrc, TDest>
{
    Expression<Func<TSrc, TDest>> AsExpression();
    TDest Convert(TSrc source);
}

public interface IBidirectionalConverter<TSrc, TDest> : ITypeConverter<TSrc, TDest>
{
    Expression<Func<TDest, TSrc>> AsReverseExpression();
    TSrc ConvertBack(TDest source);
}
