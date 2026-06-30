using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NanopaintHub.Application.Contracts;
using NanopaintHub.Domain.Entities;

namespace NanopaintHub_WinUIClient.ViewModels;

public partial class SearchDiagnosticsViewModel : ObservableObject
{
    private readonly ISubstanceSearchService _searchService;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private bool _isSearching;

    public ObservableCollection<string> DiagnosticLogs { get; } = new();
    public ObservableCollection<EchaSubstance> Results { get; } = new();

    public SearchDiagnosticsViewModel(ISubstanceSearchService searchService)
    {
        _searchService = searchService;
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
    }

    private void Log(string message)
    {
        var time = DateTime.Now.ToString("HH:mm:ss.fff");
        var log = $"[{time}] {message}";
        Debug.WriteLine(log);
        _dispatcherQueue.TryEnqueue(() => DiagnosticLogs.Add(log));
    }

    [RelayCommand]
    private async Task RunTestAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        DiagnosticLogs.Clear();
        Results.Clear();
        IsSearching = true;

        try
        {
            Log($"Starting search test for: '{SearchQuery}'");
            var stopwatch = Stopwatch.StartNew();

            Log("Invoking ISubstanceSearchService.SearchAsync...");
            var data = await _searchService.SearchAsync(SearchQuery);
            
            stopwatch.Stop();
            Log($"Search returned {data.Count} results in {stopwatch.ElapsedMilliseconds} ms.");

            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var item in data)
                {
                    Results.Add(item);
                }
                Log("Results successfully bound to the UI ObservableCollection.");
            });
        }
        catch (Exception ex)
        {
            Log($"CRITICAL EXCEPTION: {ex.GetType().Name}");
            Log(ex.Message);
            Log(ex.StackTrace ?? string.Empty);
        }
        finally
        {
            IsSearching = false;
            Log("Search test concluded.");
        }
    }
}
