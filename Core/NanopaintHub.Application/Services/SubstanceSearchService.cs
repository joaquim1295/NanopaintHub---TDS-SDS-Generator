using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub.Application.Services;

public class SubstanceSearchService : ISubstanceSearchService
{
    private readonly IEchaSubstanceRepository _repository;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private CancellationTokenSource? _cts;

    public SubstanceSearchService(IEchaSubstanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<EchaSubstance>> SearchAsync(string query)
    {
        // Cancel the previous ongoing search
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            // Debounce to prevent rapid sequential queries
            await Task.Delay(400, token);

            if (token.IsCancellationRequested) return new List<EchaSubstance>();

            // Ensure only one database query executes at a time
            await _semaphore.WaitAsync(token);
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                {
                    return new List<EchaSubstance>();
                }

                return await _repository.SearchByNameAsync(query, 10, token);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // The search was cancelled because the user typed another character
            return new List<EchaSubstance>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            return new List<EchaSubstance>();
        }
    }
}
