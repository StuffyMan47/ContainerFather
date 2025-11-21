using ContainerFather.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ContainerFather.Infrastructure.DAL.DbContext;

public partial class AppDbContext(DbContextOptions<AppDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        ApplySnakeCaseNames(modelBuilder);
        DisableCascadeDelete(modelBuilder);
    }
    
    protected static void ApplySnakeCaseNames(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetDefaultTableName()!.ToLowerCaseWithUnderscore());

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.GetColumnName().ToLowerCaseWithUnderscore());
            }
        }
    }
    
    protected static void DisableCascadeDelete(ModelBuilder modelBuilder)
    {
        var cascadeFKs = modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetForeignKeys())
            .Where(fk => fk is { IsOwnership: false, DeleteBehavior: DeleteBehavior.Cascade });

        foreach (var fk in cascadeFKs)
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}