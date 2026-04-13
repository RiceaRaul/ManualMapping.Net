using System.Linq.Expressions;
using ManualMapping.Abstractions;
using ManualMapping.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace ManualMapping.Configuration;

public sealed class MapperConfiguration
{
    private readonly Dictionary<(Type, Type), Func<object, object>> _untypedDelegates = new();
    private readonly Dictionary<(Type, Type), Delegate>            _typedDelegates   = new();
    private readonly Dictionary<(Type, Type), LambdaExpression>    _expressions      = new();

    /// <summary>
    /// Registers a converter. If it implements IBidirectionalConverter,
    /// the reverse direction is registered automatically.
    /// </summary>
    public MapperConfiguration CreateMap<TSrc, TDest>(
        TypeConverter<TSrc, TDest> converter,
        bool withProjectTo = true)
    {
        var key = (typeof(TSrc), typeof(TDest));
        Func<TSrc, TDest> typed = converter.Convert;
        _typedDelegates[key]   = typed;
        _untypedDelegates[key] = src => converter.Convert((TSrc)src)!;
        if (withProjectTo)
            _expressions[key] = converter.AsExpression();

        if (converter is IBidirectionalConverter<TSrc, TDest> bidir)
        {
            var reverseKey = (typeof(TDest), typeof(TSrc));
            Func<TDest, TSrc> reverseTyped = bidir.ConvertBack;
            _typedDelegates[reverseKey]   = reverseTyped;
            _untypedDelegates[reverseKey] = src => bidir.ConvertBack((TDest)src)!;
            if (withProjectTo)
                _expressions[reverseKey] = bidir.AsReverseExpression();
        }

        return this;
    }

    /// <summary>
    /// Resolves TConverter from DI and registers it.
    /// Works for both TypeConverter and BidirectionalConverter.
    /// </summary>
    public MapperConfiguration CreateMap<TSrc, TDest, TConverter>(
        IServiceProvider sp,
        bool withProjectTo = true)
        where TConverter : TypeConverter<TSrc, TDest>
        => CreateMap(sp.GetRequiredService<TConverter>(), withProjectTo);

    public IMapper Build() => new MapperInstance(_untypedDelegates, _typedDelegates, _expressions);
}
