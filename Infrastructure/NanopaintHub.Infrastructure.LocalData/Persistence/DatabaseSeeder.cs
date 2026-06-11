namespace NanopaintHub.Infrastructure.LocalData.Persistence;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Domain.Enums;

/// <summary>
/// Migra a base de dados e insere dados mínimos na primeira execução (2 PCs sem servidor).
/// Gravações usam <see cref="IUnitOfWork"/> para manter o padrão transacional dos repositórios.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NanopaintHubDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        await EnsureDefaultTemplatesAsync(db, cancellationToken);

        if (await db.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var category = new ProductCategory
        {
            Name = "Tintas industriais",
            Description = "Categoria de exemplo"
        };
        db.Categories.Add(category);

        var product = new Product
        {
            Code = "InkPR02NP",
            Name = "InkPR02NP®",
            Description = "InkPR02NP® is a piezoresistive ink that changes its electrical resistance when subjected to mechanical deformation. It is designed for face-to-face printing on flexible substrates and sensor applications.",
            CategoryId = category.Id,
            FeaturesJson = """
                [
                  "Carbon-based",
                  "Piezoresistive",
                  "Face-to-face printing",
                  "Flexible",
                  "Strong variation of electrical resistance",
                  "Stretchable"
                ]
                """,
            LiquidPropertiesJson = """
                {
                  "Appearance": "Black",
                  "Physical form": "Paste",
                  "Cure": "Air dry / heat cure",
                  "Solid content": "65 %",
                  "Viscosity": "80–100 KU",
                  "Max particle diameter": "< 10 µm"
                }
                """,
            MechanicalPropertiesJson = """
                {
                  "Processing": "Screen printing",
                  "Printing equipment": "Manual / semi-automatic",
                  "Mesh": "43T–90T",
                  "Stencil": "Polyester / stainless steel",
                  "Squeegee": "70–90 Shore A",
                  "Cure": "80 °C, 30 min",
                  "Clean-up": "Solvent recommended in SDS",
                  "Substrates": "PET, PI, TPU, textiles",
                  "Storage": "5–25 °C, sealed",
                  "Shelf-life": "12 months"
                }
                """,
            FunctionalPropertiesJson = "{}",
            TdsSensorSpecsJson = """
                {
                  "forceUnit": "N",
                  "sensitivityVolts": 0.001,
                  "sensitivityPerUnit": 100,
                  "responseTimeSec": 0.001,
                  "recoveryTimeSec": 0.002,
                  "repeatabilityPercent": 0.5,
                  "restResistanceOhms": 1000,
                  "operatingTempMinC": -40,
                  "operatingTempMaxC": 85,
                  "rangeMin": 0,
                  "rangeMax": 500,
                  "hysteresisPercent": 1.2,
                  "charts": {
                    "linearity": [{"x":0,"y":0},{"x":100,"y":99.5},{"x":500,"y":498}],
                    "resistanceVsForce": [{"x":0,"y":1200},{"x":250,"y":800},{"x":500,"y":600}],
                    "hysteresis": {
                      "percent": 1.2,
                      "loading": [{"x":0,"y":0},{"x":500,"y":500}],
                      "unloading": [{"x":500,"y":495},{"x":0,"y":0}]
                    }
                  }
                }
                """,
            RevisionDate = DateTime.UtcNow.Date,
            DocumentVersion = 1
        };
        db.Products.Add(product);

        var supplier = new Supplier
        {
            Name = "Fornecedor exemplo",
            Country = "PT",
            TechnicalContact = "quimica@exemplo.pt"
        };
        db.Suppliers.Add(supplier);

        var component = new SupplierComponent
        {
            SupplierId = supplier.Id,
            ComponentName = "Resina acrílica",
            CasNumber = "000-00-0",
            SdsProperties = """{"FrasesH":["H317"],"FrasesP":["P280"]}"""
        };
        db.SupplierComponents.Add(component);

        db.TestBatches.Add(new TestBatch
        {
            ProductId = product.Id,
            BatchNumber = "LT-2025-001",
            TestValues = """
                {
                  "batchNumber": "LT-2025-001",
                  "specOverrides": { "repeatabilityPercent": 0.35 },
                  "chartOverrides": {
                    "linearity": [{"x":0,"y":0},{"x":100,"y":99.8},{"x":500,"y":499.2}]
                  }
                }
                """,
            CurveGraphPath = string.Empty,
            ManufactureDate = DateTime.UtcNow.Date,
            SyncStatus = SyncStatus.LocalOnly
        });

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Garante que os templates padrão existem e usam sempre o HTML premium mais recente dos assets.
    /// </summary>
    private static async Task EnsureDefaultTemplatesAsync(
        NanopaintHubDbContext db,
        CancellationToken cancellationToken)
    {
        var tdsHtml = await DefaultTemplateAssetLoader.LoadTdsHtmlAsync(cancellationToken);
        var sdsHtml = await DefaultTemplateAssetLoader.LoadSdsHtmlAsync(cancellationToken);

        await UpsertDefaultTemplateAsync(db, "TDS padrão", "TDS", tdsHtml, cancellationToken);
        await UpsertDefaultTemplateAsync(db, "SDS padrão", "SDS", sdsHtml, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertDefaultTemplateAsync(
        NanopaintHubDbContext db,
        string templateName,
        string documentType,
        string htmlStructure,
        CancellationToken cancellationToken)
    {
        var existing = await db.DocumentTemplates.FirstOrDefaultAsync(
            t => t.TemplateName == templateName && t.DocumentType == documentType,
            cancellationToken);

        if (existing is null)
        {
            db.DocumentTemplates.Add(new DocumentTemplate
            {
                TemplateName = templateName,
                DocumentType = documentType,
                LetterheadPdfPath = string.Empty,
                TopMargin = 24,
                BottomMargin = 24,
                HtmlStructure = htmlStructure
            });
            return;
        }

        existing.HtmlStructure = htmlStructure;
        existing.MarkAsModified();
    }
}
