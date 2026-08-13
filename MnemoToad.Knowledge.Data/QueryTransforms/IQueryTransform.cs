namespace MnemoToad.Knowledge.Data.QueryTransforms;

public interface IQueryTransform<TSource, TDestination>
{
    IQueryable<TDestination> Transform(IQueryable<TSource> source, string name);
}
