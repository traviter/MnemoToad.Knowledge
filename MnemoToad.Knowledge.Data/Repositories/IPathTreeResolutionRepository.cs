using MnemoToad.Knowledge.Data.PathResolution;

namespace MnemoToad.Knowledge.Data.Repositories;

public interface IPathTreeResolutionRepository
{
    Task<List<ResolvedNodeRow>> ResolveTreeAsync(IReadOnlyList<Guid> nodeIds, IReadOnlyList<string> paths);
}
