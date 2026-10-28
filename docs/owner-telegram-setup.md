# Thiết lập Owner thật và Telegram trên máy development

Cập nhật 27/10/2026. Hướng dẫn này không tạo sẵn tài khoản, không chứa password/token thật,
không chứng minh Telegram đã gửi tin. Thao tác bí mật do người dùng nhập trực tiếp trên máy.

## 1. Owner là tài khoản của ứng dụng

Owner không phải tài khoản SQL Server, Telegram hoặc ChatGPT. Chọn một username để đăng nhập
trang quản trị, ví dụ `forfit.owner`, tên hiển thị tùy ý. Tự đặt password riêng dài 12–128 ký tự,
có chữ hoa, chữ thường, số và ký tự đặc biệt. Không gửi password vào chat.

Trong PowerShell:

```powershell
cd D:\TaiLieu\UcGaNauCham\backend\ForFitChicken
.\scripts\bootstrap-owner.ps1
```

Script hỏi username, tên hiển thị và password (ẩn khi nhập). Thành công in `Owner created successfully.`
Tài khoản nằm trong ForFitChicken_Development trên KDADEPTRAI\KDADEPTRAI.
`OwnerAlreadyExists` nghĩa Owner đã tồn tại, không chạy lại để reset và không xóa DB.

Kiểm tra bằng Swagger sau khi chạy API HTTPS:

1. GET `/api/admin/auth/csrf`, Execute, copy `requestToken`.
2. POST `/api/admin/auth/login`, điền token vào header X-CSRF-TOKEN và body username/password.
3. GET `/api/admin/auth/me` phải trả profile có role Owner.
4. Sau login gọi GET csrf lần nữa; các thao tác ghi tiếp theo dùng token mới.

## 2. Tạo bot và nhóm

1. Mở [BotFather chính thức](https://t.me/BotFather), gửi `/newbot` và làm theo hướng dẫn.
2. Chọn tên hiển thị và username bot duy nhất; BotFather cấp API token. Giữ token trên máy,
   không đưa vào chat, Git, ảnh chụp hoặc URL trình duyệt.
3. Tạo/chọn nhóm Telegram nội bộ, thêm bot vào nhóm và cho phép bot gửi tin nhắn.
   Không cần cấp toàn quyền admin chỉ để gửi thông báo trong một nhóm thông thường cho phép bot gửi.
4. Trong nhóm gửi lệnh nhắm đúng bot, ví dụ `/setup@ten_bot_cua_ban`.
   Bot chưa cần trả lời; lệnh giúp getUpdates thấy chat trong chế độ privacy mặc định.

Nguồn: [BotFather](https://core.telegram.org/bots/features#botfather),
[privacy và lệnh gửi riêng cho bot](https://core.telegram.org/bots/faq#what-messages-will-my-bot-get).

## 3. Nhập token trên PowerShell và lấy chat ID

Trong cùng PowerShell ở thư mục solution:

```powershell
$telegramSecret = Read-Host 'Dan Bot Token vao day (an ky tu)' -AsSecureString
try {
    $env:Telegram__BotToken = ([Net.NetworkCredential]::new('', $telegramSecret)).Password
}
finally { $telegramSecret.Dispose() }

try {
    $telegramUpdates = Invoke-RestMethod -Method Post -Uri (
        'https://api.telegram.org/bot' + $env:Telegram__BotToken + '/getUpdates'
    ) -ErrorAction Stop
    $telegramUpdates.result |
        ForEach-Object { $_.message.chat } |
        Where-Object { $_.type -in @('group', 'supergroup') } |
        Select-Object id, title -Unique
}
catch {
    Write-Host 'Khong lay duoc chat ID. Kiem tra token, ket noi va webhook. Khong chia se loi chua URL/token.'
}
```

Chỉ xem bảng `id,title`, không in biến token hoặc toàn bộ updates chứa dữ liệu nhóm.
Chọn `id` đúng tên nhóm và giữ nguyên dấu âm (nếu có); không dùng username bot làm chat ID.
Nếu bảng trống, gửi lại lệnh `/setup@username_bot` trong nhóm rồi chạy lại phần getUpdates.
Bot mới không cần webhook; getUpdates không dùng được khi bot đang cấu hình webhook hoặc có
consumer getUpdates khác. Không tự xóa của bot đang phục vụ hệ thống khác; dùng bot riêng cho app.

Nguồn: [Telegram getUpdates](https://core.telegram.org/bots/api#getupdates).

## 4. Bật cấu hình và chạy API

```powershell
$env:Telegram__TargetChatId = Read-Host 'Nhap id nhom vua lay (giu nguyen dau am)'
$env:Telegram__Enabled = 'true'
dotnet run --project ForFitChicken.API --launch-profile https
```

Đây là đúng ba khóa backend đọc. Cần chạy API từ chính cửa sổ đã đặt biến; đóng cửa sổ thì cấu hình
process hết hiệu lực. Chạy bằng Visual Studio đã mở trước đó không tự nhận các biến vừa đặt trong PowerShell.
Backend kiểm tra cấu hình lúc khởi động; thiếu/sai token hoặc ID 0 sẽ bị từ chối.
Không lưu token vào appsettings.json/launchSettings.json. Secret cho hosting được cấu hình riêng khi deploy.

Mở [Swagger local HTTPS](https://localhost:7010/swagger). Sau khi có Product/SKU/ảnh/MAIN/Batch/tồn,
tạo một đơn được chỉ định rõ là thử nghiệm trong môi trường dùng thử để kiểm tra thông báo thực tế.
Đơn tạo mới khi Telegram đã bật sẽ enqueue job; đơn tạo lúc Telegram tắt không được tự bổ sung job.
Xem GET `/api/admin/notifications` (Owner) để phân biệt Pending/Processing/Sent/Failed.
Sent chỉ nghĩa Telegram chấp nhận tin; nhóm cần kiểm tra bằng mắt để xác nhận đã nhận đúng nhóm.
Không chạy smoke-test.ps1 để kiểm tra bot thật: script đó luôn tắt Telegram và dùng database tạm.

Đến thời điểm viết hướng dẫn: chưa có Owner/config Telegram thật được cung cấp hoặc tự tạo bởi Codex.

## 5. Luu Telegram mot lan bang User Secrets (28/10/2026)

API da co UserSecretsId; WebApplication.CreateBuilder tu doc User Secrets khi environment la Development.
Chay tu thu muc backend/ForFitChicken:

```powershell
.\scripts\setup-telegram.ps1
```

Nhap Chat ID nhom da xac minh trong Telegram, sau do dan token hien tai tai loi nhac an ky tu.
Khong thay noi dung script bang token. Script truyen JSON qua stdin cho dotnet user-secrets,
khong ghi token vao repo/CLI arguments/file tam; chi cap nhat ba khoa Telegram va giu cac secrets khac.
Script xoa ba bien Telegram process hien tai sau khi luu thanh cong de tranh ghi de User Secrets.
Bien Telegram da dat o cap User/Machine (neu co) van uu tien cao hon, can tu kiem tra rieng.

Sau thong bao luu thanh cong, restart API:

```powershell
dotnet run --project ForFitChicken.API --launch-profile https
```

Visual Studio chay profile HTTPS/Development cung doc cau hinh nay tren cung Windows user.
Khong can nhap token moi lan chay. Backend van phai dang chay moi gui duoc thong bao.
User Secrets nam ngoai repo trong Windows user profile, KHONG ma hoa va chi danh cho development.
Khong dung `dotnet user-secrets list` trong dau ra chia se vi no in token.
Khong dung secrets.json nay lam cau hinh production; deploy can cau hinh secrets rieng.
Script chi luu cau hinh, khong goi Telegram va khong chung minh gui tin that.
Neu token tung bi lo, thu hoi va nhap token moi nhat truoc khi su dung.

Nguon: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0
