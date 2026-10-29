# Tiếp tục dự án ForFitChicken từ đây

Cập nhật **29/10/2026**. Đây là trạng thái hiện hành, thay cho các ghi chú lịch sử trong progress.
Đọc tiếp `docs/mvp-backend-progress.md`, `docs/mvp-backend-api.md`, `docs/media-security-progress.md`.
Giữ mọi thay đổi chưa commit. Không tự commit/push/drop development DB hoặc tạo/reset Owner.

## Quyết định đã chốt

- Nhóm 4 người, một dev, tự sản xuất thành phẩm ức gà. Marketing landing page + guest cart/COD;
  nhóm gọi xác nhận. Hà Nội, giao hàng ngoài app; chưa shipping fee/payment gateway/QR integration.
- Đơn pending chưa giữ hàng; confirm giữ FEFO; handover mới xuất; complete và thu COD là hai nghiệp vụ riêng.
- Gym ký gửi, startup giao/kiểm đếm khi muốn; không POS cho nhân viên gym. Tồn gym là snapshot có giờ,
  tách kho online; thiếu chưa rõ không tự tính là bán. Hoa hồng/ngân hàng gym ngoài MVP hiện tại.
- Clean Architecture đúng 5 project API/Application/Domain/Infrastructure/UnitTest, ASP.NET Core/EF 8.
  Dùng Unit of Work và transaction-scoped SQL business lock; HTTP luôn ngoài SQL transaction.
- Không Redis/RabbitMQ cho mốc này. Rate limiting có baseline/IP và policy từng nhóm API.
- Owner và Operator cùng upload ảnh (đã hủy ý định Owner-only). Owner riêng quyền báo cáo/giá vốn/thu tiền/
  công bố nội dung/monitor media/cleanup và vận hành thông báo.
- Cookie Secure/HttpOnly/Strict + CSRF; frontend/API cùng origin HTTPS. Khách không cần account.
- Codex sẽ làm React frontend trực tiếp, không Antigravity. Frontend CHƯA bắt đầu trong lượt nâng cấp media.

## Trạng thái code/Git

- Baseline đã tạo 20 commit trên main, mốc trước R2 `5fcbe14`; chạy git log/status để kiểm tra thực tế.
- R2, media security và VPS runbook được lưu thành các commit theo nhóm ở checkpoint 29/10/2026;
  chạy git log/status để xem HEAD hiện tại. **Chưa push remote**.
- Khung reference `CodeReverseASinTayBac/REA-Web` giữ local và Git ignored, không coi là frontend sản phẩm.
- Muốn đổi tài khoản Codex trên cùng máy: workspace vẫn còn. Muốn đổi máy: phải chuyển cả repository local
  hoặc push các commit sau khi người dùng yêu cầu; remote chưa nhận các commit mới. Kiểm tra và sao lưu thêm
  mọi thay đổi chưa commit phát sinh sau checkpoint. Credentials/DB/R2 không nằm trong Git.

## SQL, credentials và dữ liệu thật

- Server development `KDADEPTRAI\KDADEPTRAI`, Windows Authentication; DB `ForFitChicken_Development`.
  Không dùng LocalDB. Không xóa DB/viết lại migration đã áp dụng.
- Đã áp `20261026181658_InitialSchema` và `20261029012656_AddMediaGovernance`, 32 bảng không kể history
  (22 bảng nghiệp vụ + 3 vận hành media + 7 Identity). Migration không auto-run khi API startup.
- Owner thật đã được người dùng tạo. Không cần biết password trong chat, không tạo account mặc định.
- Telegram đã setup User Secrets, gửi thật và người dùng xác nhận nhận tin. R2 User Secrets cũng đã setup,
  adapter upload/read/delete thật và guarded WebP/cache/quota roundtrip thật đã đạt. Không in/commit secrets.
- DB còn dữ liệu TEST TELEGRAM cũ: product/SKU/batch Id1, 1 gói giả lập và pending order
  FC-f23427afcb1a4b8098b2b05756537e8a (1000 VND), không giữ hàng, không giao/thu tiền.
  Khi dọn dùng nghiệp vụ hủy/writeoff/ngừng SKU, không xóa lịch sử. Lượt media security không thay dữ liệu này.
- Bucket đối soát đang charged 318105 byte, initialized=true, uncertain=0 ở checkpoint live. Đây là số đo,
  không hardcode; phải xem Owner media storage/đối soát để biết trạng thái mới.

## Media hiện hành

- Private R2 qua backend. Mặc định appsettings Local, User Secrets máy này chọn R2.
- 5 MiB input PNG/JPEG/WebP -> decode thật, giới hạn 24 MP/10000 pixel/ảnh tĩnh -> auto-orient ->
  vừa 1920×1920 không upscale -> WebP quality80, không lưu gốc. SkiaSharp 4.154.0 + Linux native assets MIT.
- Tổng lưu trữ **9.000.000.000 byte không reset**; charged gồm stored + reservation/unknown + object ngoài app.
  SQL giữ chỗ trước PUT. Timeout/crash không trả dung lượng tự động. Xóa cloud xác nhận rồi mới hoàn quota.
- Mỗi node/VPS mới phải đối soát trước upload. Worker scan startup/mỗi 6h, retry check mỗi 5 phút;
  snapshot quá 24h hoặc scan lỗi thì chặn upload an toàn. Cùng bucket phải dùng cùng SQL ledger.
- Daily/user limits, upload concurrency, cloud A/B budgets SQL, public RAM cache, Owner pause/status/
  asset list/unused cleanup/orphan cleanup đã có. Không tự quét xóa mọi ảnh cũ. Không có hard invoice cap từ app.
- Chi tiết invariant/config/API: `docs/media-security.md`; setup: `docs/r2-storage-setup.md`;
  chuyển VPS/backup/secrets/native Linux: `docs/vps-maintenance-runbook.md` và deploy/*.example.
- API/DataProtection có application name cố định ForFitChicken; có thể phải login lại sau update.
  Production set persistent KeysPath. Chỉ trust explicit reverse-proxy IPs; không tin X-Forwarded-For tùy ý.

## Build, chạy, kiểm tra

Từ backend/ForFitChicken:

```powershell
dotnet restore
dotnet build ForFitChicken.sln
$env:FORFITCHICKEN_RUN_SQL_TESTS = '1'
dotnet test ForFitChicken.UnitTest
# Migrate khi cần (trước khi start code mới)
dotnet ef database update --project ForFitChicken.Infrastructure --startup-project ForFitChicken.API -- --environment Development
# Đối soát riêng, không mở HTTP hoặc start Telegram worker
.\scripts\reconcile-media.ps1
# Chạy đúng MỘT API instance; dừng VS/console cũ trước
dotnet run --project ForFitChicken.API --launch-profile https
```

HTTPS https://localhost:7010/swagger, HTTP 5195; đóng browser không dừng API. Không đổi port để che chạy trùng.
Nếu API giữ file build, dùng configuration riêng `MediaSafety` thay vì kill process không xác minh.
Scripts smoke/reconcile hỗ trợ `-Configuration MediaSafety`. Smoke luôn dùng database GUID/Local media/Telegram off.
Chạy DLL trực tiếp phải từ thư mục API/publish chứa appsettings, không chạy từ solution rồi thiếu connection string.

## Bằng chứng kiểm tra và giới hạn

- Trước media security: 61 tests. Sau nâng cấp **83/83 tests đạt, 0 skipped**, có SQL concurrency/failure tests, HTTP Owner/Operator+CSRF,
  image corruption/EXIF/WebP, bucket scope/lease/daily/monthly/cache/deletion; xem checkpoint cuối progress cho tổng cuối.
- Smoke HTTPS 14/14 đạt, có WebP thật, real cookie/CSRF, SQL GUID riêng; báo cáo docs/smoke-test-report.md.
  Đã sửa PNG sample cũ có signature nhưng raster hỏng; không hạ validation để cho qua.
- Real R2 quota/WebP roundtrip tạo ảnh tối ưu 70 byte, đọc/cache, xóa object thử và hoàn quota; không tạo MediaAsset,
  product/order hoặc gửi Telegram. SQL ledger/counter vận hành có lịch sử test để không che usage.
- NuGet audit sau cập nhật xUnit 2.9.3: cả 5 project không báo vulnerable packages theo nguồn hiện tại.
- OpenAPI mới được xuất từ API thật; test-only probe không xuất hiện. Swagger snapshot chỉ là hợp đồng, không là UI test.
- Đã kiểm tra Windows; **chưa deploy/chạy VPS Linux thật**, chưa cấu hình DNS/TLS/firewall/CDN/WAF/Budget Alert
  trên tài khoản Cloudflare. Templates là hướng dẫn, không phải các cấu hình ngoài hệ thống đã áp dụng.

## Việc tiếp theo

1. Xem checkpoint cuối media-security-progress về process/port, test count và Git status trước thay đổi.
2. Khi chuẩn bị production: cấu hình secrets ngoài repo, SQL backup/restore, key ring, proxy, firewall và
   Budget Alert theo runbook; test trên VPS đích và đối soát trước mở upload. Không xóa bucket khi hủy VPS.
3. Dọn dữ liệu TEST TELEGRAM bằng nghiệp vụ khi phù hợp. Không dọn ảnh thật đã được người dùng upload.
4. Bắt đầu frontend React TypeScript/Vite: kiểm tra repo/reference, tạo frontend docs; mốc đầu login Owner,
   API cookie/CSRF, responsive admin và logout; tiếp theo catalog/kho/đơn/gym/content/report rồi storefront.
