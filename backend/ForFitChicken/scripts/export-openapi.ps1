[CmdletBinding()]
param([string]$BaseUrl = 'http://localhost:5195')

$ErrorActionPreference = 'Stop'
$target = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\docs\openapi\mvp.json'))
$response = Invoke-WebRequest -Uri ($BaseUrl.TrimEnd('/') + '/swagger/v1/swagger.json')
$schema = $response.Content | ConvertFrom-Json
if (-not $schema.openapi) { throw 'The response is not an OpenAPI document.' }
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
[IO.File]::WriteAllText($target, $response.Content)
Write-Output "Exported OpenAPI to $target"
