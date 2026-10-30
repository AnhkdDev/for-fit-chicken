#!/usr/bin/env python3
"""Run privately on VPS as root. Never prints supplied credentials; no network calls."""
import getpass
import json
import os
from pathlib import Path

def ask(label, secret=False):
    value = getpass.getpass(label + ': ') if secret else input(label + ': ').strip()
    if not value or '\n' in value or '\r' in value:
        raise SystemExit('Required value missing/invalid; nothing saved.')
    return value

if os.geteuid() != 0:
    raise SystemExit('Run as root on VPS.')
path = Path('/etc/forfit/appsettings.Production.json')
if path.exists():
    raise SystemExit('Configuration already exists; refusing to overwrite.')
os.umask(0o077)
path.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
print('Private phase1 setup. Same R2 bucket/endpoint as local. Telegram remains disabled until cutover.')
password = ask('forfit_runtime SQL password (hidden)', True)
endpoint = ask('Existing R2 S3 endpoint https://...')
bucket = ask('Existing R2 bucket name')
access = ask('R2 Access Key ID (hidden)', True)
secret = ask('R2 Secret Access Key (hidden)', True)
token = ask('Existing Telegram Bot Token (hidden)', True)
chat = int(ask('Existing Telegram group chat ID (negative number)'))
# SQL Server's self-signed certificate: encrypted private Docker staging only.
# Replace with verifiable SQL certificate/TrustServerCertificate=False before public release.
quoted_password = '"' + password.replace('"', '""') + '"'
config = {
    'ConnectionStrings': {'DefaultConnection': 'Server=novastay-db,1433;Database=ForFitChicken_Production;User ID=forfit_runtime;Password=' + quoted_password + ';Encrypt=True;TrustServerCertificate=True;Persist Security Info=False'},
    'Media': {'Provider': 'R2', 'R2': {'Endpoint': endpoint, 'BucketName': bucket, 'AccessKeyId': access, 'SecretAccessKey': secret}, 'Security': {'MaxStoredBytes': 9000000000}},
    'Telegram': {'Enabled': False, 'BotToken': token, 'TargetChatId': chat},
    'Logging': {'LogLevel': {'Default': 'Warning', 'Microsoft.AspNetCore': 'Warning', 'Microsoft.EntityFrameworkCore.Database.Command': 'Warning'}}
}
# UID1654 is app in this exact built API image. Parent stays root-only on host.
fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o400)
try:
    os.fchown(fd, 1654, 1654)
    with os.fdopen(fd, 'w', encoding='utf-8') as stream:
        stream.write(json.dumps(config, ensure_ascii=False, indent=2) + '\n')
except BaseException:
    raise SystemExit('Write failed. Inspect protected file privately; no credentials printed.')
print('Saved protected configuration. Do NOT cat/share it. No services started. Telegram disabled.')
