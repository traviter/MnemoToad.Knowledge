using Microsoft.EntityFrameworkCore;

namespace MnemoToad.Knowledge.Data.DbUtil;

public class EntityCollectionSynchronizer<TEntity, TValue> where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet;
    private readonly Func<TEntity, string> _keySelector;
    private readonly Func<Guid, string, TEntity> _createBlank;
    private readonly Action<TEntity, TValue> _applyValue;

    public EntityCollectionSynchronizer(
        DbSet<TEntity> dbSet,
        Func<TEntity, string> keySelector,
        Func<Guid, string, TEntity> createBlank,
        Action<TEntity, TValue> applyValue)
    {
        _dbSet = dbSet;
        _keySelector = keySelector;
        _createBlank = createBlank;
        _applyValue = applyValue;
    }

    public List<TEntity> Sync(Guid parentId, IReadOnlyDictionary<string, TValue> desired, List<TEntity> currentRows)
    {
        var toRemove = currentRows.Where(r => !desired.ContainsKey(_keySelector(r))).ToList();
        var toAdd = new List<TEntity>();
        var result = new List<TEntity>();

        foreach (var (key, value) in desired)
        {
            var existingRow = currentRows.FirstOrDefault(r => _keySelector(r) == key);
            var target = existingRow ?? _createBlank(parentId, key);
            _applyValue(target, value);
            if (existingRow is null)
                toAdd.Add(target);
            result.Add(target);
        }

        _dbSet.RemoveRange(toRemove);
        _dbSet.AddRange(toAdd);

        return result;
    }
}
