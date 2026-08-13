using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.QueryTransforms;

namespace MnemoToad.Knowledge.Data.TerminalResolvers;

public class TerminalResolverFactory : ITerminalResolverFactory
{
    private readonly IReadOnlyDictionary<PathTerminalKind, ITerminalResolver> _resolvers;

    public TerminalResolverFactory(IAppDbContext db)
    {
        _resolvers = new Dictionary<PathTerminalKind, ITerminalResolver>
        {
            [PathTerminalKind.Column] = new ColumnTerminalResolver(),
            [PathTerminalKind.Attribute] = new AttributeTerminalResolver(new AttributeQueryTransform(db)),
            [PathTerminalKind.Media] = new MediaTerminalResolver(new MediaQueryTransform(db)),
        };
    }

    public ITerminalResolver GetResolver(PathTerminalKind kind) =>
        _resolvers.TryGetValue(kind, out var resolver)
            ? resolver
            : throw new InvalidOperationException($"No terminal resolver registered for '{kind}'.");
}
