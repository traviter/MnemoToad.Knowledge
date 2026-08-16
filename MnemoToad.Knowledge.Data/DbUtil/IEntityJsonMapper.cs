using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.DbUtil;

public interface IEntityJsonMapper<TEntity>
{
    JsonObject ToJson(TEntity entity);

    void UpdateFromJson(TEntity entity, JsonObject json);
}
