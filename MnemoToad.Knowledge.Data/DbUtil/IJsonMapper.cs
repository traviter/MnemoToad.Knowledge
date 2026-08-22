using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.DbUtil;

public interface IJsonMapper<T>
{
    JsonObject ToJson(T item);
}
