using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Application.Features.SupplierProducts;
using NanopaintHub.Domain.Entities;
using Xunit;

namespace NanopaintHub.UnitTests.UseCases;

public class SaveSupplierProductUseCaseTests
{
    private readonly Mock<ISupplierProductRepository> _supplierProductRepositoryMock;
    private readonly Mock<IEchaSubstanceRepository> _echaSubstanceRepositoryMock;
    private readonly Mock<ISdsPdfExtractorService> _sdsPdfExtractorServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly SaveSupplierProductUseCase _useCase;

    public SaveSupplierProductUseCaseTests()
    {
        _supplierProductRepositoryMock = new Mock<ISupplierProductRepository>();
        _echaSubstanceRepositoryMock = new Mock<IEchaSubstanceRepository>();
        _sdsPdfExtractorServiceMock = new Mock<ISdsPdfExtractorService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _useCase = new SaveSupplierProductUseCase(
            _supplierProductRepositoryMock.Object,
            _echaSubstanceRepositoryMock.Object,
            _sdsPdfExtractorServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSdsFile_ShouldAddProduct_WhenProductIsNew()
    {
        // Arrange
        var newProduct = new SupplierProduct
        {
            Name = "Test Material",
            ProductCode = "TM-001"
        };
        var uiCompositions = new List<SupplierProductComposition>();
        
        // Act
        await _useCase.ExecuteAsync(newProduct, null, uiCompositions, true);

        // Assert
        _supplierProductRepositoryMock.Verify(r => r.AddAsync(newProduct, It.IsAny<CancellationToken>()), Times.Once);
        _supplierProductRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SupplierProduct>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSdsFile_ShouldUpdateProduct_WhenProductExists()
    {
        // Arrange
        var existingProduct = new SupplierProduct
        {
            Name = "Existing Material",
            ProductCode = "EM-001"
        };
        // Use reflection to set Id since it is protected
        typeof(NanopaintHub.Domain.Common.Entity).GetProperty("Id")?.SetValue(existingProduct, Guid.NewGuid());
        
        var uiCompositions = new List<SupplierProductComposition>();

        // Act
        await _useCase.ExecuteAsync(existingProduct, null, uiCompositions, false);

        // Assert
        _supplierProductRepositoryMock.Verify(r => r.UpdateAsync(existingProduct, It.IsAny<CancellationToken>()), Times.Once);
        _supplierProductRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SupplierProduct>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithSdsFile_ShouldExtractDataAndSyncSubstances()
    {
        // Arrange
        var newProduct = new SupplierProduct { Name = "Material with SDS" };
        var sdsPath = "fake/path/to/sds.pdf";
        var uiCompositions = new List<SupplierProductComposition>();

        var extractionResult = new NanopaintHub.Application.Contracts.SdsExtractionResult
        {
            CasNumbers = new List<NanopaintHub.Application.Contracts.ExtractedCasData>
            {
                new NanopaintHub.Application.Contracts.ExtractedCasData
                {
                    CasNumber = "123-45-6",
                    ParsedPercentage = 50.0,
                    RawPercentageText = "50-60%",
                    ExtractedHazards = new List<string> { "H302" }
                }
            }
        };

        _sdsPdfExtractorServiceMock
            .Setup(s => s.ExtractSdsDataAsync(sdsPath))
            .ReturnsAsync(extractionResult);

        // Simulate that the substance doesn't exist yet in the database
        _echaSubstanceRepositoryMock
            .Setup(r => r.GetByCasAsync("123-45-6", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EchaSubstance?)null);

        // Act
        await _useCase.ExecuteAsync(newProduct, sdsPath, uiCompositions, true);

        // Assert
        // Verify that the substance was created and hazards were added
        _echaSubstanceRepositoryMock.Verify(r => r.AddAsync(It.Is<EchaSubstance>(s => 
            s.CasNumber == "123-45-6" && s.Hazards.Any(h => h.PhraseCode == "H302")
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Verify that an intermediate commit happened to persist the ECHA substances
        // so that they have IDs before attaching to compositions.
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Exactly(3)); 

        // Verify that the new composition was added to the product
        Assert.Single(newProduct.Compositions);
        var addedComp = newProduct.Compositions.First();
        Assert.Equal(50.0, addedComp.Percentage);
        Assert.Equal("50-60%", addedComp.PercentageText);
        Assert.False(addedComp.IsManuallyEdited);
    }
}
