namespace MnemoToad.Knowledge.Data.PathResolution;

public interface IPathExpressionParser
{
    bool TryParse(string path, out PathExpression? expression);
}
