using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;
using NanopaintHub.Application.DTOs;

namespace NanopaintHub.Application.Services;

public class SubstanceSearchService : ISubstanceSearchService
{
    private readonly IEchaSubstanceRepository _repository;
    private readonly ISupplierProductRepository _supplierProductRepository;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private CancellationTokenSource? _cts;

    public SubstanceSearchService(
        IEchaSubstanceRepository repository,
        ISupplierProductRepository supplierProductRepository)
    {
        _repository = repository;
        _supplierProductRepository = supplierProductRepository;
    }

    public async Task<List<FederatedSearchResult>> SearchAsync(string query)
    {
        // Cancel the previous ongoing search
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            // Debounce to prevent rapid sequential queries
            await Task.Delay(400, token);

            if (token.IsCancellationRequested) return new List<FederatedSearchResult>();

            // Ensure only one database query executes at a time
            await _semaphore.WaitAsync(token);
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                {
                    return new List<FederatedSearchResult>();
                }

                var casTask = _repository.SearchByCasAsync(query, 10, token);
                var nameTask = _repository.SearchByNameAsync(query, 10, token);
                var supplierTask = _supplierProductRepository.SearchByNameAsync(query, 10, false, token);
                
                await Task.WhenAll(casTask, nameTask, supplierTask);

                var echaResults = casTask.Result
                    .Concat(nameTask.Result)
                    .DistinctBy(e => e.Id)
                    .Take(15)
                    .Select(e => new FederatedSearchResult 
                    {
                        DisplayName = e.ChemicalName,
                        Description = $"CAS: {e.CasNumber} | EC: {e.EcNumber}",
                        EchaSubstance = e
                    });

                var supplierResults = supplierTask.Result
                    .Select(sp => new FederatedSearchResult
                    {
                        DisplayName = sp.Name,
                        Description = $"Matéria-Prima de: {sp.Supplier?.Name ?? "Desconhecido"}",
                        SupplierProduct = sp
                    });

                return supplierResults.Concat(echaResults).ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            return new List<FederatedSearchResult>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            return new List<FederatedSearchResult>();
        }
    }
}
