# Bắt đầu tiếp tục dự án từ đây

Cập nhật 27/10/2026. Đây là điểm vào cho phiên ChatGPT/Codex mới. Không cần lịch sử chat để tiếp tục.

## Mục tiêu và quyết định đã chốt

- Nhóm 4 người tự sản xuất ức gà nấu chậm, một người làm dev. MVP bán thành phẩm, không quản lý nguyên liệu/công thức.
- Website ưu tiên giới thiệu thương hiệu, có catalog/giỏ hàng; guest checkout COD, nhóm gọi xác nhận thủ công.
- Hà Nội, nhóm tự giao ngoài app. Chưa tính phí giao, không tích hợp thanh toán/QR/ngân hàng.
- Đặt web tạo PendingConfirmation, không giữ hàng. Chỉ xác nhận mới giữ hàng kho chính; giao hàng mới trừ tồn.
- Gym ký gửi, nhóm kiểm đếm khi muốn. Không bắt nhân viên gym ghi từng lượt bán. Tồn gym là snapshot có thời điểm;
  chênh lệch không mặc định là bán. Tách hoàn toàn hàng gym khỏi lượng bán online.
- Telegram báo đơn mới; chưa có bot token/chat ID thật. Không tự tạo hoặc ghi bí mật vào repo.
- Clean Architecture: API/Application/Domain/Infrastructure/UnitTest, ASP.NET Core 8, EF Core 8, SQL Server.
- Unit of Work bắt buộc; rate limiting đã có. Chưa dùng Redis/RabbitMQ cho MVP.
- Server thực tế `KDADEPTRAI\KDADEPTRAI`, Windows Authentication; DB `ForFitChicken_Development`.
  Bản LocalDB đã xóa theo yêu cầu, không chuyển lại LocalDB.
- Auth dùng cookie HttpOnly/Secure/SameSite Strict, cùng origin HTTPS và CSRF. Owner/Operator;
  không có đăng ký khách. Chưa tạo tài khoản thật. Script bootstrap hỏi mật khẩu, không có default password.

## Trạng thái source hiện tại

- Commit nền gần nhất: `c3bc15f` (9 commit theo nhóm đã tạo, chưa push).
- Auth và các module backend MVP đã triển khai trong working tree, chưa commit; phải giữ lại, không reset hoặc xóa.
- InitialSchema đã áp dụng: 22 bảng nghiệp vụ + 7 Identity, không tạo OperatingExpenses.
- Mốc trước nghiệp vụ: 39 tests; sau tích hợp nghiệp vụ đã đạt 56/56. Xem tiến độ để biết lượt kiểm tra cuối cùng.
  SQL tests dùng database tạm riêng, không xóa DB development. Không có account/product/order test trong DB thật.
- CatalogService, InventoryService, OrderService, GymService, ContentReportService nằm Application/Commerce;
  ICommerceStore provider-neutral, EF implementation CommerceStore nằm Infrastructure/Persistence.
- API controller nằm API/Controllers/CommerceControllers.cs; worker/sender trong Infrastructure/Notifications.
- Telegram mặc định tắt và chưa gửi thật; người dùng phải cung cấp bí mật ngoài repo. Media lưu ngoài DB, thư mục App_Data được ignore.
- Không có pending model changes, không cần migration mới cho lượt này. Nullable enum mặc định sai đã sửa trong Domain.
- Lượt chạy thử sau đó: 56 tests và 14 checkpoint qua HTTPS/Kestrel thật đạt. Đã sửa route parameter
  `action` (reserved MVC) thành `command` và thêm regression HTTP chuyển trạng thái đơn.
  Xem [báo cáo smoke](smoke-test-report.md). Không có dữ liệu thử trong DB development.
- Tác vụ hiện tại: người dùng đã yêu cầu triển khai xuyên suốt backend MVP và cập nhật docs liên tục,
  không dừng xin xác nhận sau từng module. Chỉ hỏi nếu quyết định nghiệp vụ thật sự không thể suy ra.

## Tài liệu phải đọc và cập nhật

1. [Tiến độ toàn MVP](mvp-backend-progress.md): trạng thái thật, phần đang làm, bước tiếp theo, kiểm thử cuối.
2. [Nghiệp vụ](mvp-requirements.md), [schema](database-schema.dbml), [ghi chú triển khai schema](database/implementation.md).
3. [Auth API](admin-auth-api.md), [tiến độ auth](auth-progress.md).
   Hướng dẫn người dùng nhập thông tin thật: [Owner và Telegram](owner-telegram-setup.md).
4. [API nghiệp vụ](mvp-backend-api.md), [OpenAPI](openapi/mvp.json): dùng cho frontend/Antigravity, có quy tắc và giới hạn.

## Chạy và kiểm tra

Từ `backend/ForFitChicken`:

```powershell
dotnet tool restore
dotnet restore ForFitChicken.sln
dotnet build ForFitChicken.sln --no-restore
$env:FORFITCHICKEN_RUN_SQL_TESTS = '1'
dotnet test ForFitChicken.sln --no-restore
dotnet run --project ForFitChicken.API --launch-profile https
```

Swagger `https://localhost:7010/swagger`; health `/health`. Tạo Owner thật bằng
`scripts/bootstrap-owner.ps1` khi người dùng tự nhập thông tin. Không chạy bootstrap bằng mật khẩu tự chọn hộ.
Migration không tự chạy lúc khởi động. Không sửa migration đã áp dụng; nếu model đổi phải tạo migration mới.

Xuất lại hợp đồng OpenAPI khi đổi API: chạy API Development, rồi `scripts/export-openapi.ps1`
(mặc định đọc Swagger HTTP localhost:5195, không cần gửi mật khẩu/cookie).

Tự động chạy lại một giao dịch end-to-end qua HTTP thật: `scripts/smoke-test.ps1` sau build.
Script tự migrate database GUID riêng, bootstrap Owner thử với password ngẫu nhiên, khởi động API HTTPS
ở port local trống, chạy COD/gym, lưu report rồi dọn đúng process/database/media của nó. Không gửi Telegram thật.
PowerShell 7 và dev HTTPS certificate cần có sẵn. Script không thay thế kiểm thử giao diện/UAT kinh doanh.

## Bước tiếp theo sau backend

1. Người dùng chọn và tạo Owner thật bằng script, không đặt hộ mật khẩu.
2. Người dùng cung cấp/configure Telegram thật ngoài repo rồi kiểm tra gửi tin trong nhóm.
3. Chuẩn bị ảnh/câu chuyện/SKU/giá/hạn dùng thật, nhập bằng API quản trị; không seed dữ liệu thật tự ý.
4. Kết nối React landing page/admin với OpenAPI, giữ HTTPS cùng origin và CSRF.
5. UAT các luồng bằng dữ liệu dùng thử có kiểm soát; production hosting, backup, key ring, runtime LTS upgrade
   và account Operator/reset password vẫn cần một giai đoạn riêng, không được khẳng định đã triển khai.

Đổi tài khoản ChatGPT trên cùng workspace: yêu cầu phiên mới đọc AGENTS.md và HANDOFF.md.
Nếu đổi máy/clone repo khác, phải mang theo toàn bộ working tree hoặc commit/push trước;
các file chưa commit không tự xuất hiện từ remote.

## Quy tắc bàn giao

Sau mỗi mốc: ghi file/code đã xong, kiểm thử đã chạy và lỗi còn lại vào mvp-backend-progress.md.
Không đánh dấu hoàn thành dựa vào code mới viết chưa build/test. Không khẳng định Telegram đã gửi thật nếu chưa
có cấu hình và kết quả từ dịch vụ thật. Không commit/push thay đổi mới nếu người dùng chưa yêu cầu.

## Bo sung 28/10/2026 — Telegram development
API da bat User Secrets trong Development bang UserSecretsId. Chay scripts/setup-telegram.ps1 mot lan
(tu solution) de nhap token an va chat ID; xem docs/owner-telegram-setup.md muc 5.
Chua luu token that bang Codex, chua kiem chung gui tin that. Khong doc/in User Secrets ra chat.

## Trang thai xac minh 28/10/2026
User Secrets Telegram da duoc nguoi dung luu va Codex kiem tra metadata (enabled/token present/group match),
API Development khoi dong, SQL worker hoat dong, health 200 va Swagger 43 paths.
Chua xac minh tin Telegram thuc te: goi Telegram tu moi truong Codex chua thanh cong; khong in secrets.
Trang thai nay thay the ghi chu cu 'chua luu cau hinh that'. Can test don hang that co kiem soat truoc frontend.

## Nghiem thu Telegram that 28/10/2026
Da tao don thu FC-f23427afcb1a4b8098b2b05756537e8a qua Application ManualAsync (Source Other),
1 goi/1000 VND; NotificationJob Sent lan 1, TelegramMessageId=5 qua sender that vao nhom da cau hinh.
Thay the trang thai cu 'chua gui Telegram that'. Nguoi dung chua xac nhan hien thi tren dien thoai.
DB development con du lieu TEST: product nhap Id1, batch Id1, 1 goi ton GIA LAP va don pending khong giu hang.
Khong tinh la hang that, khong giao/thu tien; xem checkpoint progress de tiep tuc don dep qua nghiep vu.
Day khong phai test HTTP guest checkout, khong thay the kiem thu UI/frontend.

## Moc moi nhat 28/10/2026 — truoc frontend
Nguoi dung da xac nhan nhan thong bao Telegram that. Config User Secrets da luu tren may.
Codex se truc tiep lam frontend React thay Antigravity; frontend CHUA bat dau.
Tiep theo: xu ly du lieu test qua nghiep vu, kiem tra khung frontend/reference, tao docs/frontend-progress.md,
frontend-architecture.md va frontend-api-integration.md; moc dau la login Owner that -> admin -> logout.
Kiem tra truoc commit: 56 tests dat, 0 fail/skip (SQL enabled). Nguoi dung yeu cau nhieu commit co prefix,
commit toan bo source/docs hien tai, khong push. Cac commit moi thay the c3bc15f lam moc tiep tuc;
chay git log de xem baseline moi nhat. Ghi chu 'chua commit/chua nhan Telegram' ben tren la lich su.
