$ErrorActionPreference = 'Stop'
$frontendRoot = Split-Path -Parent $PSScriptRoot
$certDirectory = Join-Path $frontendRoot '.certs'
New-Item -ItemType Directory -Force -Path $certDirectory | Out-Null
dotnet dev-certs https --export-path (Join-Path $certDirectory 'localhost.pem') --format Pem --no-password
if ($LASTEXITCODE -ne 0) { throw 'Không xuất được HTTPS development certificate.' }
Write-Host 'Đã xuất certificate development vào .certs (Git ignored). Không chia sẻ thư mục này.'
Write-Host 'Nếu máy chưa tin certificate: chạy dotnet dev-certs https --trust và tự xác nhận hộp thoại hệ thống.'
