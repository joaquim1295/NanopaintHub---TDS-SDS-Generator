using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Infrastructure.LocalData.Persistence;
using NanopaintHub.Infrastructure.LocalData.Repositories;
using NanopaintHub.Infrastructure.Documents.Services;
using NanopaintHub.Application.Features.SdsGeneration;
using Xunit;
using Xunit.Abstractions;

namespace NanopaintHub.UnitTests.UseCases;

public class SdsComponentPersistenceTests
{
    private readonly ITestOutputHelper _output;

    public SdsComponentPersistenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task DiluentComponents_AreSavedCorrectly_AndTransportedToSds()
    {
        // 1. Setup in-memory SQLite database
        var options = new DbContextOptionsBuilder<NanopaintHubDbContext>()
            .UseSqlite("DataSource=:memory:") // In-memory
            .Options;

        using var db = new NanopaintHubDbContext(options);
        await db.Database.OpenConnectionAsync(); // keep connection open
        await db.Database.EnsureCreatedAsync();

        var productRepo = new ProductRepository(db);
        var echaRepo = new EchaSubstanceRepository(db);
        var supplierRepo = new SupplierRepository(db);
        var supplierProductRepo = new SupplierProductRepository(db);
        var templateRepo = new TemplateRepository(db);
        var unitOfWork = new UnitOfWork(db);

        // 2. Seeder: criar categoria, componente da ECHA, e Template SDS dummy
        var category = new ProductCategory { Name = "Tintas" };
        db.Categories.Add(category);

        var substance = new EchaSubstance { CasNumber = "112-34-5", ChemicalName = "diethylene glycol monobutyl ether" };
        await echaRepo.AddAsync(substance);

        var template = new DocumentTemplate 
        { 
            TemplateName = "Template Teste SDS", 
            DocumentType = "SDS", 
            HtmlStructure = "<html><body><h1>SDS</h1><ul>{{#each Components}}<li>{{ChemicalName}} - {{Percentage}} (CAS: {{CasNumber}})</li>{{/each}}</ul></body></html>" 
        };
        db.DocumentTemplates.Add(template);

        await unitOfWork.CommitAsync();

        // 3. Simular Importação de PDF de Fornecedor
        var supplier = new Supplier { Name = "BASF Test" };
        await supplierRepo.AddAsync(supplier);
        
        var supplierProduct = new SupplierProduct
        {
            Name = "Diluente Especial",
            Supplier = supplier,
            SupplierId = supplier.Id
        };
        
        // Simular o que o PdfSdsExtractor faz ao encontrar a substância
        supplierProduct.Compositions.Add(new SupplierProductComposition
        {
            EchaSubstanceId = substance.Id,
            EchaSubstance = substance,
            SupplierProductId = supplierProduct.Id,
            Percentage = 40.0 // O diluente é 40% diethylene glycol
        });
        
        await supplierProductRepo.AddAsync(supplierProduct);
        
        // Simular o utilizador a carregar em "Importar e Adicionar"
        // Criação da Tinta Final
        var product = new Product 
        { 
            Name = "Tinta Final Teste", 
            Code = "TF-001",
            CategoryId = category.Id
        };

        // Simular o comportamento correto do `ProductEditorViewModel.SaveAsync`
        // 1. Adicionar o Componente/Diluente na Tinta
        product.Compositions.Add(new ProductComposition
        {
            SupplierProductId = supplierProduct.Id,
            SupplierProduct = supplierProduct,
            ProductId = product.Id,
            Percentage = 20.0, // A Tinta leva 20% do Diluente
            ExtraMetadataJson = "{}"
        });

        // 2. Salvar sem chamar Update (para não rebentar com os novos IDs)
        await productRepo.AddAsync(product);
        await unitOfWork.CommitAsync();

        _output.WriteLine($"Produto guardado! ID: {product.Id}");

        // 4. Limpar o tracker para provar que a Query vem da DB real
        db.ChangeTracker.Clear();

        // 5. Simular a "Geração de PDF" (Use Case)
        var sdsGatherer = new SdsDataGatherer();
        // Não precisamos de Puppeteer para testar se os componentes aparecem no HTML.
        // O Mock não é preciso se não chamarmos o documentService (estamos só a extrair HTML para o teste)
        var htmlGenerado = await sdsGatherer.GatherHtmlAsync(
            await productRepo.GetByIdAsync(product.Id) ?? throw new Exception(), 
            template);

        _output.WriteLine("=== HTML DA SDS ===");
        _output.WriteLine(htmlGenerado);
        _output.WriteLine("===================");

        // 6. Assertions
        Assert.Contains("diethylene glycol monobutyl ether", htmlGenerado);
        Assert.Contains("112-34-5", htmlGenerado);
        // A percentagem real deve ser 20% * 40% = 8%
        Assert.Contains("8%", htmlGenerado);
    }
}
