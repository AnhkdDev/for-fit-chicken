[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot '..\ForFitChicken.API'
$ownerName = Read-Host 'Owner username (letters/numbers, max 100)'
$ownerDisplay = Read-Host 'Display name (max 150)'
$ownerPassword = Read-Host 'Password (12-128 characters, upper/lowercase, number, symbol)' -AsSecureString
$keys = @('BootstrapOwner__UserName', 'BootstrapOwner__DisplayName', 'BootstrapOwner__Password')
$previous = @{}
foreach ($key in $keys) { $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process') }

try {
    [Environment]::SetEnvironmentVariable($keys[0], $ownerName, 'Process')
    [Environment]::SetEnvironmentVariable($keys[1], $ownerDisplay, 'Process')
    [Environment]::SetEnvironmentVariable($keys[2], ([Net.NetworkCredential]::new('', $ownerPassword)).Password, 'Process')
    # No credentials in command arguments, tracked settings, or printed output.
    & dotnet run --project $projectPath --no-launch-profile -- --environment Development --bootstrap-owner
    if ($LASTEXITCODE -ne 0) { throw 'Owner setup failed. See the error codes above; no existing Owner is reset.' }
}
finally {
    foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process') }
    $ownerPassword.Dispose()
}
