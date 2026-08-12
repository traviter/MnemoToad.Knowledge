namespace MnemoToad.Knowledge.Data.PathResolution;

public enum PathTerminalKind { Column, Attribute, Media }

public record PathExpression(IReadOnlyList<string> Hops, PathTerminalKind TerminalKind, string TerminalName);
