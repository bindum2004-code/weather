# Check current status
netstat -ano | Select-String :5000

# Kill any processes using port 5000 (including Edge)
$ports = netstat -ano | Select-String :5000
foreach ($line in $ports) {
    if ($line -match '\s+(\d+)\s*$') {
        $processId = $matches[1]
        taskkill /PID $processId /F 2>$null
    }
}

# Kill any lingering dotnet processes
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Wait 60s for TIME_WAIT to fully expire
Start-Sleep 60

# Run Blazor app (defaults to http://localhost:5000)
dotnet run
