using System.Linq.Expressions;
using ManualMapping.Abstractions;

namespace ManualMapping.Converters;

public abstract class BidirectionalConverter<TSrc, TDest>
    : TypeConverter<TSrc, TDest>, IBidirectionalConverter<TSrc, TDest>
{
    private readonly Lazy<Func<TDest, TSrc>> _compiledReverse;

    protected BidirectionalConverter()
        => _compiledReverse = new Lazy<Func<TDest, TSrc>>(() => AsReverseExpression().Compile());

    public abstract Expression<Func<TDest, TSrc>> AsReverseExpression();
    public TSrc ConvertBack(TDest source) => _compiledReverse.Value(source);
}
