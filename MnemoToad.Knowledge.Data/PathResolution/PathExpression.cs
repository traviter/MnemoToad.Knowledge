namespace MnemoToad.Knowledge.Data.PathResolution;

public enum PathTerminalKind { Column, Attribute, Media }

public enum PathEdgeDirection { Forward, Backward }

public record PathEdge(string Name, PathEdgeDirection Direction);

public record PathExpression(IReadOnlyList<PathEdge> Edges, PathTerminalKind TerminalKind, string TerminalName);
