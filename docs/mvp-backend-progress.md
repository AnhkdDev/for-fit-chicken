# Tiến độ triển khai toàn backend MVP

Ngày 27/10/2026. Đã hoàn thành lượt triển khai API backend MVP và kiểm chứng dưới đây;
điểm bàn giao: [HANDOFF.md](HANDOFF.md). Chưa deploy hoặc gửi Telegram thật.

| Mốc | Trạng thái | Bằng chứng / bước tiếp |
| --- | --- | --- |
| Nền DB/UoW/rate limits | Xong | InitialSchema; lịch sử commit và README |
| Auth | Xong trong working tree | 39 tests đạt trước lượt này; xem auth-progress.md |
| Bàn giao và nhật ký | Xong | AGENTS.md, HANDOFF.md và file này |
| Catalog / SKU / ảnh | API xong, có kiểm thử | CatalogService; media bytes/storage và HTTP validation |
| Lô / kho / chứng từ | API xong, có kiểm thử | InventoryService; bảo vệ reserved, phiếu bất biến và idempotency |
| COD / giữ hàng / thu tiền | API xong, có kiểm thử | OrderService; cạnh tranh tồn, FEFO, release rollback, snapshots, COD |
| Telegram | Worker/adapter xong, test giả đạt | Job SQL cùng đơn, lease/fencing/retry; mặc định tắt, chưa gửi thật |
| Gym / kiểm đếm / chênh lệch | API xong, có kiểm thử | GymService; baseline/regular, snapshot/ledger, thiếu không tự thành bán |
| Nội dung / báo cáo | API xong, có kiểm thử | ContentReportService; draft/publish, phân biệt doanh số/tiền/cost |
| Kiểm thử / hợp đồng API cuối | Xong trong phạm vi lượt này | 56/56 đạt; OpenAPI và tài liệu frontend đã lưu |

## Quyết định kỹ thuật lượt này

- MVP ít người thao tác: dùng transaction-scoped SQL application lock để tuần tự hóa các thao tác ghi nghiệp vụ.
  Khóa lấy trong chính transaction Unit of Work; giữ nhiều bảng tồn/đơn/gym nhất quán. Không có HTTP ngoài trong transaction.
- Đổi bản ghi có rowversion yêu cầu version hiện tại; xung đột trả 409, không ghi đè âm thầm.
- Query có phân trang giới hạn; public không nhận recipient, ghi chú nội bộ, cost hoặc user IDs.
- Không đưa thêm hạ tầng phân tán vào lúc này.

## Nhật ký

- Mốc 0: kiểm tra working tree, giữ nguyên auth chưa commit. Tạo tài liệu bàn giao trước khi viết module mới.
- Mốc 1: đã viết contracts, ICommerceStore/CommerceStore, service Application cho 6 nhóm và controller HTTP.
  Telegram worker/sender và media local nằm Infrastructure. Đang build và bổ sung SQL/HTTP tests.
- Chứng từ và kiểm đếm chốt trực tiếp, dùng thời điểm server, không cho ghi lùi; mốc inventory tăng ít nhất 1ms
  giữa hai tác vụ để ledger không mất biến động ở ranh giới snapshot datetime2(3).
- Kiểm đếm lấy Version địa điểm gym; delivery/recall/reclassify làm đổi version, form cũ phải tải lại.

## Kiểm chứng mới nhất

Trước lượt này: 39/39 tests đạt. Kết quả cuối của lượt này: **56/56 đạt, 0 failed, 0 skipped** với SQL opt-in.

Lượt tích hợp đầu: 48 đạt / 2 lỗi trên tổng 50. Đã xác định và đang sửa:
- Nullable condition của StockDocumentLine bị khởi tạo Good khi endpoint null, làm CHECK xuất đơn thất bại.
- Metadata validation positional record đặt ở property không tương thích MVC; chuyển sang constructor parameter,
  đồng thời service Application kiểm tra cùng metadata khi gọi ngoài HTTP.

Lượt sau sửa: 50/50 đạt. Đang mở rộng kiểm thử báo cáo, Swagger, lease Telegram cạnh tranh,
media và HTTP quản trị. Chưa gọi Telegram thật. Đã bổ sung đơn thủ công Facebook/Zalo/Phone/Other
cho nhóm nhận đơn ngoài website, vẫn tính tiền server và giữ tồn chỉ khi xác nhận.

Lượt mở rộng: 55/55 đạt, gồm media, HTTP quản trị và báo cáo. Bổ sung test giả lập job-write failure
phát hiện lỗi ở test fixture (interceptor chưa được gắn), dẫn tới job dư làm hai test worker chạy sai job.
Đã sửa cách truyền DbContext options vào factory; test rollback đơn/job đạt riêng lẻ và sau đó cả 56 tests đạt.
Đã bổ sung schema filter để OpenAPI thể hiện constraints trên constructor record và CSRF header;
sửa filter bỏ qua constructor parameter hệ thống không có tên. Kiểm thử cuối cùng toàn bộ 56 tests đạt.

## Nghiệm thu cuối và bàn giao

- Build Debug: 0 warning, 0 error. 56 tests đạt (39 nền/auth + 17 commerce/media/Telegram).
- EF `has-pending-model-changes --no-build`: không thay đổi; không tạo/apply migration mới.
- OpenAPI cuối xuất từ API Development chạy thật: 43 paths / 54 operations, không phải từ test factory;
  không có probe routes. Constraints constructor record và header CSRF đã được kiểm tra.
- API development tạm để export đã dừng. Không có process app được cố ý để chạy nền.
- DB thật ForFitChicken_Development vẫn 0 accounts/products/orders/stock documents; chỉ InitialSchema đã có.
- Toàn bộ SQL integration tests dùng GUID databases riêng; file media test dùng temp riêng.
- Không có Telegram thật được gửi; không có credential hoặc account thật được tự tạo.
- Auth và MVP mới nằm trong working tree, **chưa commit/push**. Chuyển máy cần mang working tree hoặc commit/push theo yêu cầu người dùng.

## Bước tiếp theo cho phiên mới

1. Đọc AGENTS.md → HANDOFF.md → mvp-backend-api.md → docs/openapi/mvp.json.
2. Người dùng tự tạo Owner bằng scripts/bootstrap-owner.ps1; cấu hình bot/chat thật ngoài repo nếu muốn bật Telegram.
3. Nhập dữ liệu/ảnh thật và tích hợp React; thực hiện UAT có kiểm soát trước production.
4. Các phần ngoài phạm vi được liệt kê cuối mvp-backend-api.md; không nhầm test giả với nghiệm thu vận hành thật.

## Lượt chạy thử HTTPS thật theo yêu cầu người dùng — 27/10/2026

- Đã chạy lại 56 kiểm thử nền, đạt. Bổ sung scripts/smoke-test.ps1: Kestrel HTTPS thật,
  database GUID riêng, Owner thử với mật khẩu ngẫu nhiên, upload ảnh, COD và gym qua HTTP.
- Lượt smoke đầu phát hiện POST /api/admin/orders/{id}/actions/confirm trả 404 dù service tests đạt.
  Nguyên nhân: route parameter `action` là tên dành riêng trong MVC; đã đổi thành `command`, URL thao tác không đổi.
- Đã bổ sung regression HTTP confirm → prepare → handover → complete trong CommerceTests.
- Kết quả cuối: **56/56 tests đạt và 14/14 checkpoint smoke qua HTTPS thật đạt**.
  Report chi tiết: [smoke-test-report.md](smoke-test-report.md). Lần smoke cuối dùng database mới,
  Owner tạm, ảnh upload thật và thao tác hoàn toàn qua HTTP, không gọi service trực tiếp.
- Đã thử COD từ đặt → confirm → prepare → handover (gửi lặp) → complete → thu tiền 70000 VND;
  thêm confirm/cancel đơn khác, gym delivery → baseline → regular → xác minh → recall và kiểm tra tồn cách ly.
- Đã refresh OpenAPI với route parameter command; URL cụ thể /actions/confirm không thay đổi.
- Script đã dừng API, xóa đúng database GUID và thư mục media/log tạm; SQL kiểm tra không còn smoke DB.
  DB development vẫn 0 accounts/products/orders. Không gửi Telegram thật và không tạo Owner thật.

Chạy lại từ backend/ForFitChicken: `dotnet build ForFitChicken.sln`, sau đó `.\scripts\smoke-test.ps1`.
Script cần PowerShell 7, SQL Server instance hiện tại và dev HTTPS certificate có sẵn;
chỉ bỏ kiểm tra trust certificate cho request HTTPS localhost của luồng test. Không dùng script làm seed dữ liệu kinh doanh.

## Hướng dẫn cấu hình thật — 27/10/2026

Đã thêm [owner-telegram-setup.md](owner-telegram-setup.md): chọn username/password Owner,
bootstrap bằng script có sẵn, tạo bot qua BotFather, lấy nhóm qua getUpdates mà không đưa token
vào URL trình duyệt, đặt ba biến môi trường đúng với options backend và chạy API cùng cửa sổ.
Đã kiểm tra script/options/launch profile và tài liệu chính thức Telegram. Chỉ viết hướng dẫn;
chưa nhận mật khẩu/token, chưa tạo Owner thật hoặc gửi Telegram thật trong lượt này.

## Checkpoint 28/10/2026 — User Secrets Telegram
- Them UserSecretsId vao API va scripts/setup-telegram.ps1: nhap token an, JSON stdin cho Secret Manager,
  khong token trong repo/arguments/file tam. Giu secrets khac; xoa override process sau khi luu thanh cong.
- Nguoi dung da xac minh getMe va tim duoc chat nhom; chua xac nhan gui thong bao don hang that.
- Codex khong luu token that: can nguoi dung chay script nhap token hien tai tren may.
- Huong dan luu mot lan va chay lai trong docs/owner-telegram-setup.md muc 5. Chua commit.
- Kiem tra: PowerShell parser OK; API build output rieng artifacts/user-secrets-check thanh cong 0 warnings/errors. Build mac dinh bi API dang chay (PID 37544) khoa file; khong dung process cua nguoi dung. Chua chay script voi token that, chua test gui Telegram.

## Xac minh User Secrets 28/10/2026
- Da kiem tra file User Secrets ton tai: Telegram Enabled=true, token co gia tri/dung dinh dang,
  chat ID khop nhom nguoi dung da xac minh. Khong in/ghi token vao docs.
- Chay API profile https thanh cong trong Development; worker claim SQL NotificationJobs thanh cong.
- HTTP /health tra 200, Swagger that co 43 paths. API dang chay tai https://localhost:7010 (phien Codex).
- Goi Telegram getMe/getChat tu moi truong Codex khong xac minh duoc (network/API), nen KHONG ket luan
  token con hieu luc/quyen gui nhom hoac tin that da gui. Khong gui tin thu va khong tao don/du lieu kinh doanh.
- Buoc tiep: nghiem thu don thu duoc chi dinh ro voi du lieu catalog/kho san sang, kiem tra job va tin trong nhom;
  sau do tich hop frontend theo API contract. Secret da luu that tren may, thay the ghi chu cu chua luu cau hinh.

## 28/10/2026 — Da gui thong bao Telegram THAT theo yeu cau nguoi dung
- DB development ban dau khong co product/variant/balance/order. Tao qua Application services voi UoW/SQL lock,
  dung Owner hien co, khong tao account moi va khong lay password: MAIN (neu chua co), product nhap
  TEST TELEGRAM - KHONG BAN (ProductId=1), SKU TEST_TELEGRAM_20261028, batch TEST_TELEGRAM_20261028 (BatchId=1),
  receipt 1 goi GIA LAP. San pham IsPublished=false, khong hien tren catalog public.
- Tao dung 1 don thu thu cong Source=Other (KHONG phai HTTP guest checkout):
  FC-f23427afcb1a4b8098b2b05756537e8a, 1 goi, 1000 VND, PendingConfirmation.
  Ten/ghi chu TEST TELEGRAM - KHONG GIAO HANG; dien thoai gia 0000000000; khong lien he/khong thu tien.
- NotificationJob duoc tao cung transaction don; dispatcher that gui qua TelegramSender that, HTTP ngoai transaction.
  Ket qua Sent, AttemptCount=1, TelegramMessageId=5, LastError=null. Day la gui THAT, khong fake sender.
- Nguoi dung can kiem tra tin trong nhom For Fit Chicken; Sent chi chung minh Telegram chap nhan,
  khong chung minh notification tren dien thoai co am thanh hay nguoi dung da doc.
- Du lieu thu van duoc giu lai de xem: don PendingConfirmation khong giu/tru hang; 1 goi ton gia lap thuoc SKU nhap.
  Can huy don va ghi xuat huy/vo hieu SKU thu qua nghiep vu khi don dep, khong xoa lich su hoac tinh vao hang that.
- Cong cu mot lan nam artifacts/telegram-order-check (Git ignored), khong chua token; User Secrets doc tai runtime.
  Lan dau cong cu thieu IConfiguration DI va dung truoc khi ghi du lieu; da sua va chay thanh cong.

## 28/10/2026 — Checkpoint Git truoc frontend
- Nguoi dung xac nhan da nhan thong bao that trong nhom For Fit Chicken.
- Nguoi dung doi quyet dinh: Codex truc tiep xay frontend React, khong ban giao Antigravity.
  Chua trien khai frontend. Moc tiep theo: doc repo/frontend hien co, lap tai lieu frontend,
  tao nen React TypeScript/Vite va luong Owner login/cookie/CSRF/admin responsive; cap nhat docs lien tuc.
- Truoc frontend can xu ly du lieu test (huy don, xu ly ton gia lap, ngung SKU) bang nghiep vu, khong xoa lich su.
- Kiem tra truoc commit: 56/56 tests dat, 0 skipped voi FORFITCHICKEN_RUN_SQL_TESTS=1;
  build vao artifacts/precommit-tests de tranh API dang chay khoa output. Build solution -o co warning NETSDK1194;
  sau do chay test tren output da build. Khong khang dinh build solution nay 0 warnings.
- Rasoat file versionable khong thay mau token Telegram; secrets local, artifacts va media runtime khong commit.
- Nguoi dung yeu cau commit tat ca thanh nhieu commit theo nhom. Khong push remote trong luot nay.
