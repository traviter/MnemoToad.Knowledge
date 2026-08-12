using MnemoToad.Knowledge.Data.PathResolution;
using System.ComponentModel.DataAnnotations;

namespace MnemoToad.Knowledge.Api.Contracts;

public class ValidPathExpressionAttribute : ValidationAttribute
{
    private static readonly IPathExpressionParser Parser = new PathExpressionParser();

    public ValidPathExpressionAttribute() : base("Each path must be valid Path DSL syntax.") { }

    public override bool IsValid(object? value) =>
        value is not List<string> paths || paths.All(p => Parser.TryParse(p, out _));
}
