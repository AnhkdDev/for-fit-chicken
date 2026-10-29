[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot '..\ForFitChicken.API'
$endpoint = (Read-Host 'S3 endpoint https://ACCOUNT_ID.r2.cloudflarestorage.com').Trim()
if ($endpoint -notmatch '^https://[a-fA-F0-9]{32}(\.(eu|us|fedramp))?\.r2\.cloudflarestorage\.com/?$') {
    throw 'S3 endpoint R2 khong hop le. Copy endpoint tu Cloudflare, khong dan URL bucket/public domain.'
}
$bucket = (Read-Host 'Bucket name (vi du forfit-media-dev)').Trim()
if ($bucket -notmatch '^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$') { throw 'Ten bucket khong hop le.' }
$access = $null
$secret = $null
$json = $null
try {
    $access = Read-Host 'Dan Access Key ID tai day' -AsSecureString
    $secret = Read-Host 'Dan Secret Access Key tai day' -AsSecureString
    $accessText = ([Net.NetworkCredential]::new('', $access)).Password
    $secretText = ([Net.NetworkCredential]::new('', $secret)).Password
    if ([string]::IsNullOrWhiteSpace($accessText) -or [string]::IsNullOrWhiteSpace($secretText)) { throw 'Thieu khoa R2.' }
    $json = @{
        'Media:Provider' = 'R2'
        'Media:R2:Endpoint' = $endpoint
        'Media:R2:BucketName' = $bucket
        'Media:R2:AccessKeyId' = $accessText
        'Media:R2:SecretAccessKey' = $secretText
    } | ConvertTo-Json -Compress
    $json | & dotnet user-secrets set --project $projectPath | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Khong luu duoc User Secrets.' }
    foreach ($key in @('Media__Provider', 'Media__R2__Endpoint', 'Media__R2__BucketName', 'Media__R2__AccessKeyId', 'Media__R2__SecretAccessKey')) {
        [Environment]::SetEnvironmentVariable($key, $null, 'Process')
    }
    Write-Host 'Da luu cau hinh R2. Restart API trong Development. Chua xac minh ket noi Cloudflare.'
}
finally {
    if ($null -ne $access) { $access.Dispose() }
    if ($null -ne $secret) { $secret.Dispose() }
    $json = $null
    $accessText = $null
    $secretText = $null
}
