using System.Linq.Expressions;
using ManualMapping.Abstractions;

namespace ManualMapping.Converters;

public abstract class TypeConverter<TSrc, TDest> : ITypeConverter<TSrc, TDest>
{
    private readonly Lazy<Func<TSrc, TDest>> _compiled;

    protected TypeConverter()
        => _compiled = new Lazy<Func<TSrc, TDest>>(() => AsExpression().Compile());

    public abstract Expression<Func<TSrc, TDest>> AsExpression();

    /// <summary>
    /// Override this to use injected services in the mapping logic.
    /// By default, compiles and invokes AsExpression().
    /// When overridden, register with withProjectTo: false.
    /// </summary>
    public virtual TDest Convert(TSrc source) => _compiled.Value(source);
}
