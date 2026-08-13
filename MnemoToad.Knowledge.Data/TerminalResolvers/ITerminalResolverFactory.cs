using MnemoToad.Knowledge.Data.PathResolution;

namespace MnemoToad.Knowledge.Data.TerminalResolvers;

public interface ITerminalResolverFactory
{
    ITerminalResolver GetResolver(PathTerminalKind kind);
}
