using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Data.Common;
using MnemoToad.Knowledge.Data.DbUtil;
using MnemoToad.Knowledge.Data.Entities;
using MnemoToad.Knowledge.Data.PathResolution;
using MnemoToad.Knowledge.Data.TerminalResolvers;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.Repositories;

public class PathTreeResolutionRepository : IPathTreeResolutionRepository
{
    private readonly IAppDbContext _db;
    private readonly IPathExpressionParser _parser;
    private readonly ITerminalResolverFactory _terminalResolverFactory;
    private readonly IQueryTransform<KnowledgeNode, KnowledgeNode> _forwardEdgeQueryTransform;
    private readonly IQueryTransform<KnowledgeNode, KnowledgeNode> _backwardEdgeQueryTransform;

    public PathTreeResolutionRepository(IAppDbContext db, IPathExpressionParser parser, ITerminalResolverFactory terminalResolverFactory,
        IQueryTransform<KnowledgeNode, KnowledgeNode> forwardEdgeQueryTransform,
        IQueryTransform<KnowledgeNode, KnowledgeNode> backwardEdgeQueryTransform)
    {
        _db = db;
        _parser = parser;
        _terminalResolverFactory = terminalResolverFactory;
        _forwardEdgeQueryTransform = forwardEdgeQueryTransform;
        _backwardEdgeQueryTransform = backwardEdgeQueryTransform;
    }

    public async Task<List<ResolvedNodeRow>> ResolveTreeAsync(IReadOnlyList<Guid> nodeIds, IReadOnlyList<string> paths)
    {
        var expressions = new Dictionary<string, PathExpression>();
        foreach (var path in paths)
        {
            if (!_parser.TryParse(path, out var expression))
                throw new InvalidOperationException($"Path '{path}' failed to parse after passing request validation.");
            expressions[path] = expression!;
        }

        var trie = PathTrieNode.Build(expressions);
        var rows = new List<ResolvedNodeRow>();
        foreach (var nodeId in nodeIds)
            foreach (var fragment in await EvaluateNodeAsync(trie, nodeId))
                rows.Add(new ResolvedNodeRow(nodeId, fragment.Properties, fragment.Errors.Count > 0 ? fragment.Errors : null));
        return rows;
    }

    // Every KnowledgeNode-scoped query below runs against the same scoped IAppDbContext, which is
    // not safe for concurrent use, so this walk is deliberately sequential (no Task.WhenAll).
    private async Task<List<RowFragment>> EvaluateNodeAsync(PathTrieNode trieNode, Guid currentNodeId)
    {
        var perEdgeChildFragments = new List<List<RowFragment>>();
        foreach (var (edgeKey, childTrie) in trieNode.Children)
        {
            var matchedIds = await TraverseEdgeAsync(edgeKey, currentNodeId);

            var childFragments = new List<RowFragment>();
            foreach (var id in matchedIds)
                childFragments.AddRange(await EvaluateNodeAsync(childTrie, id));

            // Traversing an edge that matches nothing (directly, or because everything downstream
            // of it came up empty) kills this node's entire contribution - an empty factor in a
            // cartesian product makes the whole product empty. Short-circuits remaining sibling
            // edges and this node's own terminal resolution, since the node is already known to die.
            if (childFragments.Count == 0)
                return [];

            perEdgeChildFragments.Add(childFragments);
        }

        var terminalFragment = await ResolveTerminalsAsync(trieNode, currentNodeId);

        if (perEdgeChildFragments.Count == 0)
            return [terminalFragment];

        IEnumerable<RowFragment> combined = [RowFragment.Empty];
        foreach (var list in perEdgeChildFragments)
            combined = combined.SelectMany(a => list.Select(b => Merge(a, b)));

        return combined.Select(f => Merge(terminalFragment, f)).ToList();
    }

    private async Task<List<Guid>> TraverseEdgeAsync(PathEdge edge, Guid currentNodeId)
    {
        var source = _db.KnowledgeNode.AsNoTracking().Where(n => n.Id == currentNodeId);
        var transform = edge.Direction == PathEdgeDirection.Forward ? _forwardEdgeQueryTransform : _backwardEdgeQueryTransform;
        return await transform.Transform(source, edge.Name).Select(n => n.Id).ToListAsync();
    }

    // A terminal always contributes exactly one outcome (a value or an error) - it never fans out
    // and never produces zero outcomes, unlike traversing an edge.
    private async Task<RowFragment> ResolveTerminalsAsync(PathTrieNode trieNode, Guid currentNodeId)
    {
        var properties = new Dictionary<string, JsonNode?>();
        var errors = new Dictionary<string, string>();
        var source = _db.KnowledgeNode.AsNoTracking().Where(n => n.Id == currentNodeId);
        foreach (var (kind, name, originalPath) in trieNode.Terminals)
        {
            var result = await _terminalResolverFactory.GetResolver(kind).ResolveAsync(source, name);
            switch (result)
            {
                case Result<JsonNode>.Success success:
                    properties[originalPath] = success.Value;
                    break;
                case Result<JsonNode>.Failure failure:
                    errors[originalPath] = failure.Message;
                    break;
                default:
                    throw new UnreachableException();
            }
        }
        return new RowFragment(properties, errors);
    }

    private static RowFragment Merge(RowFragment a, RowFragment b)
    {
        var properties = new Dictionary<string, JsonNode?>(a.Properties);
        foreach (var (key, value) in b.Properties)
            properties[key] = value;
        var errors = new Dictionary<string, string>(a.Errors);
        foreach (var (key, value) in b.Errors)
            errors[key] = value;
        return new RowFragment(properties, errors);
    }

    private sealed record RowFragment(Dictionary<string, JsonNode?> Properties, Dictionary<string, string> Errors)
    {
        public static RowFragment Empty => new([], []);
    }
}
