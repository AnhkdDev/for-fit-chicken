[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot '..\ForFitChicken.API'
$chatIdText = Read-Host 'Nhap Chat ID nhom (giu dau am)'
[long]$chatId = 0
if (-not [long]::TryParse($chatIdText, [ref]$chatId) -or $chatId -eq 0) {
    throw 'Chat ID phai la so nguyen khac 0.'
}
$secret = Read-Host 'Dan Bot Token hien tai (an ky tu)' -AsSecureString
$payload = $null
$token = $null
try {
    $token = ([Net.NetworkCredential]::new('', $secret)).Password
    if ($token -notmatch '^\d+:[A-Za-z0-9_-]+$') {
        throw 'Token khong dung dinh dang. Hay lay token hien tai tu BotFather.'
    }
    $payload = @{
        'Telegram:Enabled' = 'true'
        'Telegram:BotToken' = $token
        'Telegram:TargetChatId' = $chatId.ToString([Globalization.CultureInfo]::InvariantCulture)
    } | ConvertTo-Json -Compress
    # Pass JSON on stdin: no token in CLI arguments, temporary files or output.
    $payload | & dotnet user-secrets set --project $projectPath | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Khong luu duoc User Secrets.' }

    # Environment variables take precedence over User Secrets in this terminal.
    foreach ($key in @('Telegram__Enabled', 'Telegram__BotToken', 'Telegram__TargetChatId')) {
        [Environment]::SetEnvironmentVariable($key, $null, 'Process')
    }
    Write-Host 'Da luu Telegram vao User Secrets. Khoi dong lai API trong Development.'
}
finally {
    $secret.Dispose()
    $token = $null
    $payload = $null
}
