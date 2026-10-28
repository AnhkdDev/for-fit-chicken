[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$solutionRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$apiRoot = Join-Path $solutionRoot 'ForFitChicken.API'
$apiDll = Join-Path $apiRoot 'bin\Debug\net8.0\ForFitChicken.API.dll'
$reportPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\docs\smoke-test-report.md'))
$suffix = [Guid]::NewGuid().ToString('N')
$databaseName = 'ForFitChicken_SmokeTests_' + $suffix
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) $databaseName
$connection = "Server=KDADEPTRAI\KDADEPTRAI;Database=$databaseName;Integrated Security=true;Encrypt=true;TrustServerCertificate=true"
$keys = @('ConnectionStrings__DefaultConnection', 'BootstrapOwner__UserName', 'BootstrapOwner__DisplayName', 'BootstrapOwner__Password', 'Telegram__Enabled', 'Media__RootPath')
$previous = @{}
foreach ($key in $keys) { $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process') }
$checks = [Collections.Generic.List[string]]::new()
$serverProcess = $null
$databaseMayExist = $false
$failure = $null
$cleanupFailure = $null
$owner = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$guest = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$ownerToken = $null
$guestToken = $null
$baseUrl = $null

function Check([bool]$Condition, [string]$Label) {
    if (-not $Condition) { throw "Check failed: $Label" }
    $checks.Add($Label)
    Write-Output "PASS: $Label"
}

function Request([string]$Method, [string]$Path, $Body = $null, [bool]$GuestRequest = $false, [int]$Expected = 200, [string]$Key = $null) {
    $session = if ($GuestRequest) { $guest } else { $owner }
    $csrf = if ($GuestRequest) { $guestToken } else { $ownerToken }
    $headers = @{}
    if ($csrf) { $headers['X-CSRF-TOKEN'] = $csrf }
    if ($Key) { $headers['Idempotency-Key'] = $Key }
    $parameters = @{ Uri = $baseUrl + $Path; Method = $Method; WebSession = $session; Headers = $headers; SkipCertificateCheck = $true; TimeoutSec = 15; StatusCodeVariable = 'httpStatus'; SkipHttpErrorCheck = $true }
    if ($null -ne $Body) { $parameters.Body = ConvertTo-Json -InputObject $Body -Depth 20 -Compress; $parameters.ContentType = 'application/json' }
    $result = Invoke-RestMethod @parameters
    if ([int]$httpStatus -ne $Expected) { throw "Unexpected HTTP $httpStatus for $Method $Path (expected $Expected)." }
    return $result
}

try {
    if (-not (Test-Path -LiteralPath $apiDll)) { throw 'Build ForFitChicken.sln before running this script.' }
    New-Item -ItemType Directory -Path $tempRoot | Out-Null
    [Environment]::SetEnvironmentVariable('ConnectionStrings__DefaultConnection', $connection, 'Process')
    [Environment]::SetEnvironmentVariable('Telegram__Enabled', 'false', 'Process')
    [Environment]::SetEnvironmentVariable('Media__RootPath', (Join-Path $tempRoot 'media'), 'Process')
    Push-Location $solutionRoot
    try {
        $databaseMayExist = $true
        $migrationOutput = & dotnet ef database update --no-build --project ForFitChicken.Infrastructure --startup-project ForFitChicken.API --context ForFitChickenDbContext -- --environment Development --Logging:LogLevel:Default Warning 2>&1
        if ($LASTEXITCODE -ne 0) { throw 'Migration failed for the isolated smoke database.' }
        $userName = 'smoke-' + $suffix.Substring(0, 12)
        $password = 'Smoke-' + [Guid]::NewGuid().ToString('N') + '-aA7!'
        [Environment]::SetEnvironmentVariable('BootstrapOwner__UserName', $userName, 'Process')
        [Environment]::SetEnvironmentVariable('BootstrapOwner__DisplayName', 'Temporary smoke-test owner', 'Process')
        [Environment]::SetEnvironmentVariable('BootstrapOwner__Password', $password, 'Process')
        $bootstrapOutput = & dotnet $apiDll --environment Development --bootstrap-owner 2>&1
        if ($LASTEXITCODE -ne 0) { throw 'Bootstrap failed for the temporary smoke-test account.' }
        foreach ($key in @('BootstrapOwner__UserName', 'BootstrapOwner__DisplayName', 'BootstrapOwner__Password')) { [Environment]::SetEnvironmentVariable($key, $null, 'Process') }
    }
    finally { Pop-Location }

    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop()
    $baseUrl = "https://localhost:$port"
    $serverProcess = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @("`"$apiDll`"", '--environment', 'Development', '--urls', $baseUrl) -WorkingDirectory $apiRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $tempRoot 'api.stdout.log') -RedirectStandardError (Join-Path $tempRoot 'api.stderr.log')
    $ready = $false
    for ($i = 0; $i -lt 40; $i++) {
        if ($serverProcess.HasExited) { throw 'The temporary HTTPS API exited during startup.' }
        try { Invoke-WebRequest -Uri "$baseUrl/health" -SkipCertificateCheck -TimeoutSec 1 | Out-Null; $ready = $true; break } catch { Start-Sleep -Milliseconds 200 }
    }
    Check $ready 'Real HTTPS API started and health returned 200'
    $schema = Request GET '/swagger/v1/swagger.json'
    Check ($null -ne $schema.openapi) 'Live Swagger/OpenAPI is available'
    $ownerToken = (Request GET '/api/csrf').requestToken
    $loginProfile = Request POST '/api/admin/auth/login' @{ userName = $userName; password = $password }
    $password = $null
    Check ($loginProfile.roles -contains 'Owner') 'Owner login succeeded with real cookie and CSRF'
    $ownerToken = (Request GET '/api/csrf').requestToken
    $warehouse = Request POST '/api/admin/inventory/warehouse'
    $productInput = @{ name = 'SMOKE TEST - chicken'; slug = 'smoke-' + $suffix; shortDescription = 'Temporary test data'; description = 'Temporary story'; ingredients = $null; nutritionText = $null; usageInstructions = $null; storageInstructions = $null; isActive = $true; isPublished = $false; isFeatured = $false; sortOrder = 0; version = $null }
    $product = Request POST '/api/admin/products' $productInput
    $variant = Request POST "/api/admin/products/$($product.id)/variants" @{ skuCode = 'SMOKE-' + $suffix; label = '200g'; netWeightGrams = 200; sellingPrice = 35000; isActive = $true; sortOrder = 0; version = $null }
    $imagePath = Join-Path $tempRoot 'sample.png'
    [IO.File]::WriteAllBytes($imagePath, [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jz1kAAAAASUVORK5CYII='))
    $asset = Invoke-RestMethod -Uri "$baseUrl/api/media" -Method Post -WebSession $owner -Headers @{ 'X-CSRF-TOKEN' = $ownerToken } -Form @{ file = Get-Item -LiteralPath $imagePath } -SkipCertificateCheck
    $null = Request POST "/api/admin/products/$($product.id)/images" @{ version = $product.version; image = @{ assetId = $asset.id; variantId = $null; altText = 'Smoke-test image'; sortOrder = 0 } }
    $product = Request GET "/api/admin/products/$($product.id)"
    $productInput.version = $product.version; $productInput.isPublished = $true
    $product = Request PUT "/api/admin/products/$($product.id)" $productInput
    Check $product.isPublished 'Product, SKU, real image upload and publication succeeded'
    $batch = Request POST '/api/admin/inventory/batches' @{ variantId = $variant.id; batchCode = 'B-' + $suffix; manufacturedOn = [DateTime]::UtcNow.AddHours(7).ToString('yyyy-MM-dd'); expiresAtUtc = [DateTime]::UtcNow.AddDays(3).ToString('O'); notes = 'Temporary test batch' }
    $null = Request POST '/api/admin/inventory/documents' @{ operationKey = [Guid]::NewGuid().ToString(); documentType = 'ProductionReceipt'; reason = 'Smoke receipt'; lines = @(@{ batchId = $batch.id; quantity = 20; fromLocationId = $null; fromCondition = $null; toLocationId = $warehouse; toCondition = 'Good'; note = $null }) }
    $guestToken = (Request GET '/api/csrf' -GuestRequest $true).requestToken
    $cart = @{ recipientName = 'SMOKE TEST CUSTOMER'; recipientPhone = '0900000000'; recipientAddress = 'Ha Noi - temporary smoke data'; customerNote = $null; items = @(@{ variantId = $variant.id; quantity = 2 }) }
    $key = [Guid]::NewGuid().ToString()
    $requested = Request POST '/api/orders' $cart -GuestRequest $true -Expected 201 -Key $key
    $replayed = Request POST '/api/orders' $cart -GuestRequest $true -Expected 201 -Key $key
    $order = (Request GET "/api/admin/orders?search=$($requested.orderNumber)").items[0]
    $balance = (Request GET '/api/admin/inventory/balances').items | Where-Object { $_.batchId -eq $batch.id -and $_.condition -eq 'Good' }
    Check ($requested.orderNumber -eq $replayed.orderNumber -and $order.status -eq 'PendingConfirmation' -and $balance.reserved -eq 0) 'Guest checkout is idempotent and does not reserve stock'
    $order = Request POST "/api/admin/orders/$($order.id)/actions/confirm" @{ version = $order.version; reason = $null }
    $balance = (Request GET '/api/admin/inventory/balances').items | Where-Object { $_.batchId -eq $batch.id -and $_.condition -eq 'Good' }
    Check ($balance.onHand -eq 20 -and $balance.reserved -eq 2) 'Confirmation reserves exactly two packages'
    $order = Request POST "/api/admin/orders/$($order.id)/actions/prepare" @{ version = $order.version; reason = $null }
    $order = Request POST "/api/admin/orders/$($order.id)/actions/handover" @{ version = $order.version; reason = $null }
    $order = Request POST "/api/admin/orders/$($order.id)/actions/handover" @{ version = $order.version; reason = $null }
    $balance = (Request GET '/api/admin/inventory/balances').items | Where-Object { $_.batchId -eq $batch.id -and $_.condition -eq 'Good' }
    Check ($balance.onHand -eq 18 -and $balance.reserved -eq 0) 'Handover decrements stock once even when repeated'
    $order = Request POST "/api/admin/orders/$($order.id)/actions/complete" @{ version = $order.version; reason = $null }
    $receipt = Request POST "/api/admin/orders/$($order.id)/receipts" @{ version = $order.version; receivedAtUtc = [DateTime]::UtcNow.ToString('O'); reference = 'SMOKE CASH' }
    $from = [Uri]::EscapeDataString([DateTime]::UtcNow.Date.ToString('O')); $to = [Uri]::EscapeDataString([DateTime]::UtcNow.Date.AddDays(1).ToString('O'))
    $report = Request GET "/api/admin/reports/sales?fromUtc=$from&toUtc=$to"
    Check ($receipt.amount -eq 70000 -and $report.ordersCompleted -eq 1 -and $report.codReceived -eq 70000) 'Completed COD order and cash/report values match 70000 VND'
    $cart.items[0].quantity = 3
    $cancelRequest = Request POST '/api/orders' $cart -GuestRequest $true -Expected 201 -Key ([Guid]::NewGuid().ToString())
    $cancelOrder = (Request GET "/api/admin/orders?search=$($cancelRequest.orderNumber)").items[0]
    $cancelOrder = Request POST "/api/admin/orders/$($cancelOrder.id)/actions/confirm" @{ version = $cancelOrder.version; reason = $null }
    $null = Request POST "/api/admin/orders/$($cancelOrder.id)/actions/cancel" @{ version = $cancelOrder.version; reason = 'Smoke cancellation' }
    $balance = (Request GET '/api/admin/inventory/balances').items | Where-Object { $_.batchId -eq $batch.id -and $_.condition -eq 'Good' }
    Check ($balance.onHand -eq 18 -and $balance.reserved -eq 0) 'Cancellation releases reservation without reducing physical stock'
    $gym = Request POST '/api/admin/gyms' @{ gymCode = 'G-' + $suffix; name = 'SMOKE TEST GYM'; publicAddress = 'Ha Noi'; publicPhone = $null; publicDescription = $null; internalContactName = $null; internalPhone = $null; internalNotes = 'Temporary private note'; coverAssetId = $null; isActive = $true; isPublished = $true; version = $null }
    $null = Request POST '/api/admin/inventory/documents' @{ operationKey = [Guid]::NewGuid().ToString(); documentType = 'GymDelivery'; reason = 'Smoke consignment'; lines = @(@{ batchId = $batch.id; quantity = 4; fromLocationId = $warehouse; fromCondition = 'Good'; toLocationId = $gym.locationId; toCondition = 'Good'; note = $null }) }
    $stock = Request GET "/api/admin/gyms/$($gym.id)/stock"
    $baseline = Request POST "/api/admin/gyms/$($gym.id)/counts" @{ version = $stock.version; lines = @(@{ batchId = $batch.id; actualGood = 4; actualQuarantine = 0; actualDamaged = 0; note = $null }); note = 'Smoke baseline' }
    $stock = Request GET "/api/admin/gyms/$($gym.id)/stock"
    $count = Request POST "/api/admin/gyms/$($gym.id)/counts" @{ version = $stock.version; lines = @(@{ batchId = $batch.id; actualGood = 2; actualQuarantine = 0; actualDamaged = 0; note = $null }); note = 'Smoke regular count' }
    $line = $count.lines[0]
    $resolutions = @(Request GET "/api/admin/gyms/count-lines/$($line.id)/resolutions")
    Check ($baseline.kind -eq 'OpeningBaseline' -and $line.expectedGood -eq 4 -and $line.actualGood -eq 2 -and $resolutions.Count -eq 0) 'Gym shortage remains unresolved instead of becoming automatic sales'
    $resolution = Request POST "/api/admin/gyms/count-lines/$($line.id)/resolutions" @{ version = $line.version; direction = 'Missing'; category = 'Sale'; quantity = 1; reason = 'Verified by smoke scenario'; evidenceReference = 'Test-only evidence'; operationKey = [Guid]::NewGuid().ToString() }
    Check ($resolution.quantity -eq 1 -and $resolution.category -eq 'Sale') 'Explicit gym sale verification succeeds'
    $null = Request POST '/api/admin/inventory/documents' @{ operationKey = [Guid]::NewGuid().ToString(); documentType = 'GymRecall'; reason = 'Smoke recall'; lines = @(@{ batchId = $batch.id; quantity = 1; fromLocationId = $gym.locationId; fromCondition = 'Good'; toLocationId = $warehouse; toCondition = 'Quarantine'; note = $null }) }
    $stock = Request GET "/api/admin/gyms/$($gym.id)/stock"
    $quarantine = (Request GET '/api/admin/inventory/balances').items | Where-Object { $_.batchId -eq $batch.id -and $_.condition -eq 'Quarantine' }
    Check ($stock.isRealtime -eq $false -and $stock.lastCountLines[0].actualGood -eq 2 -and $stock.bookEstimate[0].good -eq 1 -and $quarantine.onHand -eq 1 -and $quarantine.available -eq 0) 'Recall enters Quarantine and does not overwrite the counted gym snapshot'
    $cart.items[0].quantity = 15
    $null = Request POST '/api/orders' $cart -GuestRequest $true -Expected 409 -Key ([Guid]::NewGuid().ToString())
    Check $true 'Checkout cannot borrow consignment or Quarantine stock'
    $null = Request POST '/api/admin/auth/logout' -Expected 204
    $null = Request GET '/api/admin/auth/me' -Expected 401
    Check $true 'Logout revokes the live admin session'
    $openApiPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\docs\openapi\mvp.json'))
    $openApiResponse = Invoke-WebRequest -Uri "$baseUrl/swagger/v1/swagger.json" -SkipCertificateCheck
    [IO.File]::WriteAllText($openApiPath, $openApiResponse.Content)
}
catch { $failure = $_.Exception.Message }
finally {
    try {
        if ($null -ne $serverProcess -and -not $serverProcess.HasExited) { Stop-Process -Id $serverProcess.Id -Force; $serverProcess.WaitForExit() }
        if ($databaseMayExist) {
            if ($databaseName -notmatch '^ForFitChicken_SmokeTests_[a-f0-9]{32}$') { throw 'Unsafe test database name; cleanup refused.' }
            $master = [System.Data.SqlClient.SqlConnection]::new('Server=KDADEPTRAI\KDADEPTRAI;Database=master;Integrated Security=true;Encrypt=true;TrustServerCertificate=true')
            try {
                $master.Open(); $command = $master.CreateCommand()
                $command.CommandText = "IF DB_ID(N'$databaseName') IS NOT NULL BEGIN ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$databaseName]; END;"
                $null = $command.ExecuteNonQuery()
            } finally { $master.Dispose() }
        }
        if (Test-Path -LiteralPath $tempRoot) {
            # Only known files in this generated test directory, no recursive deletion.
            $mediaRoot = Join-Path $tempRoot 'media'
            if (Test-Path -LiteralPath $mediaRoot) {
                foreach ($file in Get-ChildItem -LiteralPath $mediaRoot -File) { Remove-Item -LiteralPath $file.FullName }
                [IO.Directory]::Delete($mediaRoot)
            }
            foreach ($name in @('sample.png', 'api.stdout.log', 'api.stderr.log')) {
                $path = Join-Path $tempRoot $name
                if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
            }
            [IO.Directory]::Delete($tempRoot)
        }
    } catch { $cleanupFailure = $_.Exception.Message }
    foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process') }
    $password = $null
    $date = [DateTimeOffset]::UtcNow.ToOffset([TimeSpan]::FromHours(7)).ToString('yyyy-MM-dd HH:mm:ss zzz')
    $status = if ($failure -or $cleanupFailure) { 'FAILED' } else { 'PASSED' }
    $lines = @('# Live HTTPS smoke-test report', '', "Date: $date", "Result: $status", '', 'Real Kestrel HTTPS API, real Identity cookie/CSRF and isolated SQL Server database.', 'Server: KDADEPTRAI\KDADEPTRAI. No development/business data was created or modified.', '', '## Checks', '')
    $lines += $checks | ForEach-Object { '- PASS: ' + $_ }
    $lines += @('', '## Scope and cleanup', '', '- Temporary Owner password was randomly generated and never written to this report or source.', '- Telegram was explicitly disabled; no real message was sent.', '- API process stopped; only the generated smoke database and its own media/temp files were cleaned up.', '- This is automated API smoke testing, not frontend/browser UAT.')
    if ($failure) { $lines += @('', ('Failure: ' + $failure)) }
    if ($cleanupFailure) { $lines += @('', ('Cleanup requires attention: ' + $cleanupFailure)) }
    [IO.File]::WriteAllLines($reportPath, $lines)
}
if ($failure) { throw $failure }
if ($cleanupFailure) { throw $cleanupFailure }
Write-Output "Smoke test passed: $($checks.Count) checks. Report: $reportPath"
