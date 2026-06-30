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
        await SeedEchaDataAsync(db, cancellationToken);

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

        var pcymene = await db.EchaSubstances.FirstOrDefaultAsync(e => e.CasNumber == "99-87-6", cancellationToken);
        if (pcymene != null)
        {
            db.ProductCompositions.Add(new ProductComposition
            {
                ProductId = product.Id,
                EchaSubstanceId = pcymene.Id,
                Percentage = 75.0 // ~ 55-80%
            });
        }

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

    private static async Task SeedEchaDataAsync(NanopaintHubDbContext db, CancellationToken cancellationToken)
    {
        if (await db.ClpPhrases.AnyAsync(cancellationToken))
            return;

        // 1. Frases H Base (Mock inicial para testar)
        var phrases = new[]
        {
            new ClpPhrase { Code = "H225", Description = "Líquido e vapor facilmente inflamáveis.", PhraseType = "H" },
            new ClpPhrase { Code = "H226", Description = "Líquido e vapor inflamáveis.", PhraseType = "H" },
            new ClpPhrase { Code = "H304", Description = "Pode ser mortal por ingestão e penetração nas vias respiratórias.", PhraseType = "H" },
            new ClpPhrase { Code = "H315", Description = "Provoca irritação cutânea.", PhraseType = "H" },
            new ClpPhrase { Code = "H319", Description = "Provoca irritação ocular grave.", PhraseType = "H" },
            new ClpPhrase { Code = "H335", Description = "Pode provocar irritação das vias respiratórias.", PhraseType = "H" },
            new ClpPhrase { Code = "H336", Description = "Pode provocar sonolência ou vertigens.", PhraseType = "H" }
        };
        db.ClpPhrases.AddRange(phrases);

        // 2. Substâncias (Anexo VI Mock)
        var acetone = new EchaSubstance { ChemicalName = "Acetone", CasNumber = "67-64-1", EcNumber = "200-662-2" };
        var ethanol = new EchaSubstance { ChemicalName = "Ethanol", CasNumber = "64-17-5", EcNumber = "200-578-6" };
        var pcymene = new EchaSubstance { ChemicalName = "1-Isopropyl-4-methylbenzene (p-cymene)", CasNumber = "99-87-6", EcNumber = "202-796-7" };
        
        db.EchaSubstances.AddRange(acetone, ethanol, pcymene);
        
        // 3. Associações H
        db.SubstanceHazards.AddRange(
            new SubstanceHazard { Substance = acetone, PhraseCode = "H225" },
            new SubstanceHazard { Substance = acetone, PhraseCode = "H319" },
            new SubstanceHazard { Substance = acetone, PhraseCode = "H336" },
            
            new SubstanceHazard { Substance = ethanol, PhraseCode = "H225" },
            new SubstanceHazard { Substance = ethanol, PhraseCode = "H319" },

            new SubstanceHazard { Substance = pcymene, PhraseCode = "H226" },
            new SubstanceHazard { Substance = pcymene, PhraseCode = "H304" },
            new SubstanceHazard { Substance = pcymene, PhraseCode = "H315" },
            new SubstanceHazard { Substance = pcymene, PhraseCode = "H319" },
            new SubstanceHazard { Substance = pcymene, PhraseCode = "H335" }
        );

        await db.SaveChangesAsync(cancellationToken);
    }
}