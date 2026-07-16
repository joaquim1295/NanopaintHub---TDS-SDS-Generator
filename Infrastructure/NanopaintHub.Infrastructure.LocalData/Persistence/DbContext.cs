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
    public DbSet<SupplierProduct> SupplierProducts { get; set; } = null!;
    public DbSet<SupplierProductComposition> SupplierProductCompositions { get; set; } = null!;
    public DbSet<DocumentRelease> DocumentReleases { get; set; } = null!;

    // SDS Database (ECHA / CLP)
    public DbSet<EchaSubstance> EchaSubstances { get; set; } = null!;
    public DbSet<ClpPhrase> ClpPhrases { get; set; } = null!;
    public DbSet<SubstanceHazard> SubstanceHazards { get; set; } = null!;
    public DbSet<ProductComposition> ProductCompositions { get; set; } = null!;

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

        modelBuilder.Entity<Product>()
            .Property(p => p.PrecautionaryCodesJson)
            .HasDefaultValue("[]");

        modelBuilder.Entity<TestBatch>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(tb => tb.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DocumentRelease>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(dr => dr.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // SDS Config
        modelBuilder.Entity<EchaSubstance>()
            .HasIndex(e => e.ChemicalName);
            
        modelBuilder.Entity<EchaSubstance>()
            .HasIndex(e => e.CasNumber);

        modelBuilder.Entity<SubstanceHazard>()
            .HasKey(sh => new { sh.SubstanceId, sh.PhraseCode });

        modelBuilder.Entity<SubstanceHazard>()
            .HasOne(sh => sh.Substance)
            .WithMany(s => s.Hazards)
            .HasForeignKey(sh => sh.SubstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SubstanceHazard>()
            .HasOne(sh => sh.Phrase)
            .WithMany()
            .HasForeignKey(sh => sh.PhraseCode)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductComposition>()
            .HasOne(pc => pc.Product)
            .WithMany(p => p.Compositions)
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductComposition>()
            .HasOne(pc => pc.EchaSubstance)
            .WithMany()
            .HasForeignKey(pc => pc.EchaSubstanceId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<ProductComposition>()
            .HasOne(pc => pc.SupplierProduct)
            .WithMany()
            .HasForeignKey(pc => pc.SupplierProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SupplierProduct>()
            .HasOne(sp => sp.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(sp => sp.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SupplierProductComposition>()
            .HasOne(spc => spc.SupplierProduct)
            .WithMany(sp => sp.Compositions)
            .HasForeignKey(spc => spc.SupplierProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SupplierProductComposition>()
            .HasOne(spc => spc.EchaSubstance)
            .WithMany()
            .HasForeignKey(spc => spc.EchaSubstanceId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<ProductComposition>()
            .Property(pc => pc.ExtraMetadataJson)
            .HasDefaultValue("{}");
    }
}
