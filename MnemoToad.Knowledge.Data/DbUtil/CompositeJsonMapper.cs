using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.DbUtil;

public class CompositeJsonMapper<T> : IJsonMapper<IEnumerable<T>>
{
    private readonly IJsonMapper<T> _mapper;

    public CompositeJsonMapper(IJsonMapper<T> mapper)
    {
        _mapper = mapper;
    }

    public JsonObject ToJson(IEnumerable<T> items)
    {
        var result = new JsonObject();
        foreach (var item in items)
        {
            var (key, value) = _mapper.ToJson(item).Single();
            result[key] = value?.DeepClone();
        }
        return result;
    }
}
