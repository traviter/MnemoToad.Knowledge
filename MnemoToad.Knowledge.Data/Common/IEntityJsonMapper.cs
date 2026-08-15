using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.Common;

public interface IEntityJsonMapper<TEntity>
{
    JsonObject ToJson(TEntity entity);

    void UpdateFromJson(JsonObject json, TEntity entity);
}
