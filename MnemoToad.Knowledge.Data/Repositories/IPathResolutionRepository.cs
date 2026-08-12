using MnemoToad.Knowledge.Data.PathResolution;

namespace MnemoToad.Knowledge.Data.Repositories;

public interface IPathResolutionRepository
{
    Task<ResolvedPath> ResolveAsync(PathResolutionQuery query);
    Task<List<ResolvedPath>> ResolveAsync(IReadOnlyList<PathResolutionQuery> queries);
}
