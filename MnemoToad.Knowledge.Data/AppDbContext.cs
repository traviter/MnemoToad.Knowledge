using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MnemoToad.Knowledge.Data.Entities;
using Npgsql;
using System.Text.Json.Nodes;

namespace MnemoToad.Knowledge.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<NodeType> NodeType => Set<NodeType>();
    public DbSet<KnowledgeNode> KnowledgeNode => Set<KnowledgeNode>();
    public DbSet<RelationshipType> RelationshipType => Set<RelationshipType>();
    public DbSet<KnowledgeRelation> KnowledgeRelation => Set<KnowledgeRelation>();
    public DbSet<KnowledgeNodeAttribute> KnowledgeNodeAttribute => Set<KnowledgeNodeAttribute>();
    public DbSet<MediaAsset> MediaAsset => Set<MediaAsset>();
    public DbSet<KnowledgeNodeMedia> KnowledgeNodeMedia => Set<KnowledgeNodeMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var jsonValueComparer = new ValueComparer<JsonValue>(
            (a, b) => a.ToJsonString() == b.ToJsonString(),
            v => v.ToJsonString().GetHashCode(),
            v => JsonNode.Parse(v.ToJsonString())!.AsValue());

        modelBuilder.Entity<KnowledgeNodeAttribute>().HasKey(a => new { a.KnowledgeNodeId, a.Key });

        modelBuilder.Entity<KnowledgeNodeAttribute>()
            .Property(a => a.Value)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v.ToJsonString(),
                v => JsonNode.Parse(v)!.AsValue())
            .Metadata.SetValueComparer(jsonValueComparer);

        modelBuilder.Entity<KnowledgeNode>().Ignore(n => n.Attributes);

        var jsonObjectComparer = new ValueComparer<JsonObject?>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.ToJsonString() == b.ToJsonString()),
            v => v == null ? 0 : v.ToJsonString().GetHashCode(),
            v => v == null ? null : JsonNode.Parse(v.ToJsonString())!.AsObject());

        modelBuilder.Entity<KnowledgeNodeMedia>().HasKey(m => new { m.KnowledgeNodeId, m.Key });

        modelBuilder.Entity<KnowledgeNodeMedia>()
            .Property(m => m.Metadata)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : v.ToJsonString(),
                v => v == null ? null : JsonNode.Parse(v)!.AsObject())
            .Metadata.SetValueComparer(jsonObjectComparer);

        modelBuilder.Entity<KnowledgeNode>().Ignore(n => n.Media);
    }

    public Task<int> SaveChangesAsync() => SaveChangesAsync(CancellationToken.None);

    public async Task<int> ExecuteDeleteAsync<TEntity>(IQueryable<TEntity> query) where TEntity : class
    {
        try
        {
            return await query.ExecuteDeleteAsync();
        }
        catch (PostgresException ex)
        {
            throw new DbUpdateException(ex.Message, ex);
        }
    }
}
