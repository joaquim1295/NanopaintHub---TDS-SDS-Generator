param (
    [switch]$WatchEventViewer
)

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host " WinUI 3 AutoSuggestBox Crash Audit Harness " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

if ($WatchEventViewer) {
    Write-Host "Monitoring Windows Application Event Log for recent Application Errors (EventID 1000)..." -ForegroundColor Yellow
    Write-Host "Look for exceptions like 0xc0000005 (Access Violation) referencing combase.dll, Microsoft.ui.xaml.dll or similar." -ForegroundColor Yellow
    
    # Get the latest Application errors in the last hour
    $startTime = (Get-Date).AddHours(-1)
    $events = Get-WinEvent -FilterHashtable @{LogName='Application'; ID=1000; StartTime=$startTime} -ErrorAction SilentlyContinue

    if ($events) {
        foreach ($event in $events) {
            $msg = $event.Message
            if ($msg -match "NanopaintHub.WinUIClient.exe") {
                Write-Host "`n[!] CRASH DETECTED:" -ForegroundColor Red
                Write-Host "Time: $($event.TimeCreated)"
                Write-Host "Details:"
                Write-Host $msg -ForegroundColor Red
            }
        }
    } else {
        Write-Host "No crashes found in the last hour for NanopaintHub." -ForegroundColor Green
    }
} else {
    Write-Host "To run automated UI testing, tools like WinAppDriver or FlaUI are typically used."
    Write-Host "Since the user reported consistent crashes specifically when the AutoSuggestBox is typed into rapidly"
    Write-Host "and populates a dropdown over a complex visual tree (Expander), the diagnosis points to a known WinUI 3 issue."
    Write-Host "`nRecommendation: Run this script with '-WatchEventViewer' immediately after the application crashes"
    Write-Host "to capture the C++ Access Violation stack trace."
}
