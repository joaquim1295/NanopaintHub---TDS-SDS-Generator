using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NanopaintHub.Infrastructure.LocalData.Persistence;
using NanopaintHub_WinUIClient.ViewModels;

class Program
{
    static async Task Main()
    {
        var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NanopaintHub");
        var connectionString = $"Data Source={Path.Combine(dataDir, "nanopaint.db")}";
        
        var services = new ServiceCollection();
        services.AddLogging(c => c.AddConsole());
        services.AddNanopaintHubPersistence(connectionString);
        services.AddTransient<ProductEditorViewModel>();
        
        var provider = services.BuildServiceProvider();
        
        var db = provider.GetRequiredService<NanopaintHubDbContext>();
        var product = await db.Products.FirstOrDefaultAsync();
        if (product == null) {
            Console.WriteLine("No product found!");
            return;
        }
        
        Console.WriteLine($"Found Product: {product.Name}");
        
        var vm = provider.GetRequiredService<ProductEditorViewModel>();
        await vm.InitializeAsync(product.Id);
        
        // Simulate ImportPdfAsync
        var supplierRepo = provider.GetRequiredService<NanopaintHub.Application.Contracts.ISupplierRepository>();
        var supplierProductRepo = provider.GetRequiredService<NanopaintHub.Application.Contracts.ISupplierProductRepository>();
        var echaRepo = provider.GetRequiredService<NanopaintHub.Application.Contracts.IEchaSubstanceRepository>();
        
        var supplier = new NanopaintHub.Domain.Entities.Supplier { Name = "Simulated Supplier" };
        await supplierRepo.AddAsync(supplier);
        
        var supplierProduct = new NanopaintHub.Domain.Entities.SupplierProduct
        {
            Name = "Simulated Diluent",
            SupplierId = supplier.Id,
            Supplier = supplier
        };
        
        var sub = await echaRepo.GetByCasAsync("112-34-5");
        if (sub == null) {
            sub = new NanopaintHub.Domain.Entities.EchaSubstance { CasNumber = "112-34-5", ChemicalName = "diethylene glycol monobutyl ether" };
            await echaRepo.AddAsync(sub);
        }
        
        supplierProduct.Compositions.Add(new NanopaintHub.Domain.Entities.SupplierProductComposition
        {
            EchaSubstanceId = sub.Id,
            EchaSubstance = sub,
            SupplierProductId = supplierProduct.Id,
            Percentage = 40
        });
        
        await supplierProductRepo.AddAsync(supplierProduct);
        
        Console.WriteLine("Executing AddCompositionCommand...");
        vm.AddCompositionCommand.Execute(new NanopaintHub.Application.DTOs.FederatedSearchResult
        {
            SupplierProduct = supplierProduct
        });
        
        Console.WriteLine("Executing SaveAsync()...");
        try 
        {
            await vm.SaveAsync();
            Console.WriteLine("SaveAsync completed without throwing!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SaveAsync THREW AN EXCEPTION: {ex}");
        }
        
        // Re-check
        var dbCheck = provider.GetRequiredService<NanopaintHubDbContext>();
        var spCount = await dbCheck.SupplierProducts.CountAsync();
        var pcCount = await dbCheck.ProductCompositions.CountAsync();
        Console.WriteLine($"SupplierProducts in DB: {spCount}");
        Console.WriteLine($"ProductCompositions in DB: {pcCount}");
    }
}
