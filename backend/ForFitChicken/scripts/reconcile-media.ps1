[CmdletBinding()]
param([string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$apiRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\ForFitChicken.API'))
Push-Location $apiRoot
try {
    # Direct CLI mode does not start HTTP listeners or Telegram workers.
    & dotnet run --project . --configuration $Configuration --no-build --no-launch-profile -- --environment Development --reconcile-media --Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command Warning
    if ($LASTEXITCODE -ne 0) { throw 'Media reconciliation failed. Verify config/database/network; do not print secrets.' }
}
finally { Pop-Location }
