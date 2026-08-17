namespace MnemoToad.Knowledge.Data.PathResolution;

public class PathTrieNode
{
    public List<(PathTerminalKind Kind, string Name, string OriginalPath)> Terminals { get; } = [];
    public Dictionary<PathEdge, PathTrieNode> Children { get; } = [];

    public static PathTrieNode Build(IReadOnlyDictionary<string, PathExpression> expressions)
    {
        var root = new PathTrieNode();
        foreach (var (path, expression) in expressions)
        {
            var node = root;
            foreach (var edge in expression.Edges)
            {
                if (!node.Children.TryGetValue(edge, out var child))
                {
                    child = new PathTrieNode();
                    node.Children[edge] = child;
                }
                node = child;
            }
            node.Terminals.Add((expression.TerminalKind, expression.TerminalName, path));
        }
        return root;
    }
}
