using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MnemoToad.Knowledge.Data.PathResolution;

public class PathExpressionParser : IPathExpressionParser
{
    private static readonly Regex FullPattern = new(@"^(?:[<>][^<>_.#]+)*(?<marker>[_.#])(?<name>[^<>_.#]+)$", RegexOptions.Compiled);
    private static readonly Regex EdgePattern = new(@"(?<dir>[<>])(?<name>[^<>_.#]+)", RegexOptions.Compiled);

    public bool TryParse(string path, out PathExpression? expression)
    {
        var match = FullPattern.Match(path);
        if (!match.Success)
        {
            expression = null;
            return false;
        }

        var edges = EdgePattern.Matches(path)
            .Select(m => new PathEdge(m.Groups["name"].Value, m.Groups["dir"].Value == ">" ? PathEdgeDirection.Forward : PathEdgeDirection.Backward))
            .ToList();
        var kind = match.Groups["marker"].Value switch
        {
            "_" => PathTerminalKind.Column,
            "." => PathTerminalKind.Attribute,
            "#" => PathTerminalKind.Media,
            _ => throw new UnreachableException()
        };

        expression = new PathExpression(edges, kind, match.Groups["name"].Value);
        return true;
    }
}
