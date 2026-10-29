# Media security — 29/10/2026

Đang triển khai, chưa coi các mục dưới đây là đã kiểm thử/đã deploy.

Quyết định: Owner + Operator upload; private R2; đầu vào 5 MiB; output WebP; tổng bucket
9.000.000.000 byte không reset. SQL lưu quota, reservation và ngân sách thao tác; giữ chỗ trước HTTP.
Không tự trả quota khi timeout/crash. Trước upload cloud đầu tiên cần đối soát đầy đủ bucket.
Khóa bucket/scope không cho đổi endpoint/bucket âm thầm. Dọn orphan chỉ khi không có MediaAsset,
đủ thời gian chờ và xóa cloud thành công. Không xóa dữ liệu nghiệp vụ/test cũ trong checkpoint này.

Mặc định mới (có thể cấu hình): 10 upload/phút/tài khoản; 100 lần/ngày và 200 MB đầu vào/ngày
toàn hệ thống; 2 tác vụ xử lý ảnh/process và 2 upload cloud active toàn DB; cache public trong RAM
64 MiB có TTL; ngân sách cloud 100.000 Class A và 1.000.000 Class B/tháng UTC, SDK không tự retry.
Các giới hạn app không bảo đảm invoice bằng 0 và không kiểm soát khóa bị lộ/upload trực tiếp.

Cần hoàn thành: code + migration mới, test cạnh tranh/failure/reservation/reconcile/ảnh thật,
áp migration development không xóa DB; đối soát bucket thật theo cấu hình người dùng;
cập nhật API/OpenAPI và runbook chuyển VPS/backup/secrets. Không commit/push nếu chưa yêu cầu.

Checkpoint code/test ban đầu:
- Đã có MediaPolicyService (Application), 3 bảng mới và migration AddMediaGovernance. Migration cũ giữ nguyên.
- R2 adapter SDK retry=0; mỗi PUT/GET/LIST tính ngân sách SQL trước HTTP. Unknown giữ byte qua restart/tháng.
- Ảnh tối ưu dùng SkiaSharp 4.154.0 + Linux native assets cùng phiên bản (MIT), không dùng ImageSharp.
  Nhánh ImageSharp 3.1 bị NuGet cảnh báo; đã loại bỏ, không giữ dependency có lỗ hổng hay yêu cầu license key.
- Đối soát lease SQL, snapshot đầy đủ bucket, khóa upload trong lúc scan; scope hash ngăn đổi bucket âm thầm.
  Worker đối soát mỗi 6 giờ, kiểm tra mỗi 5 phút; upload từ chối nếu snapshot cũ quá 24 giờ.
- Lượt test ban đầu: 70/70 đạt, SQL enabled, 0 skipped. Đang bổ sung test HTTP và bù xóa khi metadata lỗi.
- Chưa áp migration database development, chưa chạy đối soát bucket thật với quota mới.

## Hoàn tất 29/10/2026
- 83/83 tests đạt, 0 skipped (SQL bật), Debug solution build 0 warnings/errors; audit NuGet cả 5 projects sạch theo advisory hiện tại.
- 14/14 smoke HTTPS đạt (GUID DB, Local media, Telegram off). Test lỗi ảnh mẫu đã sửa bằng PNG raster hợp lệ;
  có regression reject file chỉ đúng signature nhưng ảnh hỏng. OpenAPI mới 50 paths/61 operations.
- Migration 20261029012656_AddMediaGovernance đã áp development, không reset dữ liệu; EF không có pending model changes.
- Bucket thật baseline 318105 byte. Guarded live roundtrip WebP70 byte/read/cache/delete đạt, quota về 318105,
  uncertain0; không tạo metadata/nghiệp vụ hoặc gửi Telegram. SQL ledger/counter giữ lịch sử thử (không che usage).
- Mỗi node phải scan startup trước upload; scan lỗi fail closed toàn DB. Owner có asset library/unused deletion,
  giữ reference draft+published, grace24h, orphan cleanup chủ động. Metadata delete + object delete có kết quả riêng.
- Thêm DataProtection application name/key path và explicit proxy IP handling; cần đăng nhập/CSRF lại sau update nếu cookie cũ.
- Runbook/template VPS đã có; chưa triển khai ngoài máy Windows, chưa WAF/CDN/BudgetAlert thật. Không coi templates là nghiệm thu Linux.
- API test đã dừng để không chiếm cổng. Người dùng chạy F5 hoặc dotnet run, chỉ một instance.
- Chưa commit/push. Đọc HANDOFF mới và media-security.md, không dùng các dòng Đang triển khai ở trên như trạng thái hiện hành.

Git checkpoint 29/10/2026: người dùng đã yêu cầu lưu code theo nhóm commit. Không push remote.
Các dòng chưa commit bên trên là lịch sử trước checkpoint này; xem git log/status cho HEAD và thay đổi mới.
Frontend là bước tiếp theo, chưa bắt đầu. Không mang cấu hình User Secrets vào repository.
