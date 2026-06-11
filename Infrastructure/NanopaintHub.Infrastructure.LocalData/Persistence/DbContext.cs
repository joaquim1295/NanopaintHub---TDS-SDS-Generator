namespace NanopaintHub.Infrastructure.LocalData.Persistence;

using Microsoft.EntityFrameworkCore;
using NanopaintHub.Domain.Entities;

public class NanopaintHubDbContext : DbContext
{
    // 1. AS TABELAS (DbSets)
    public DbSet<ProductCategory> Categories { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<TestBatch> TestBatches { get; set; } = null!;
    public DbSet<DocumentTemplate> DocumentTemplates { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<SupplierComponent> SupplierComponents { get; set; } = null!;

    // O Construtor obrigatório do EF Core
    public NanopaintHubDbContext(DbContextOptions<NanopaintHubDbContext> options) 
        : base(options)
    {
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<Product>()
            .HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); 
        
        modelBuilder.Entity<Product>()
            .Property(p => p.LiquidPropertiesJson)
            .HasDefaultValue("{}");
            
        modelBuilder.Entity<Product>()
            .Property(p => p.MechanicalPropertiesJson)
            .HasDefaultValue("{}");
            
        modelBuilder.Entity<Product>()
            .Property(p => p.FunctionalPropertiesJson)
            .HasDefaultValue("{}");

        modelBuilder.Entity<Product>()
            .Property(p => p.TdsSensorSpecsJson)
            .HasDefaultValue("{}");

        modelBuilder.Entity<TestBatch>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(tb => tb.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SupplierComponent>()
            .HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(sc => sc.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}