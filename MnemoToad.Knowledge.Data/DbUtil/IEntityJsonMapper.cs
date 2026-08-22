using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data.DbUtil;

public interface IEntityJsonMapper<TEntity> : IJsonMapper<TEntity>
{
    void UpdateFromJson(TEntity entity, JsonObject json);
}
