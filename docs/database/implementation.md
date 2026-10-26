# Schema backend đã triển khai

Cập nhật 27/10/2026. Migration `20261026181658_InitialSchema` đã áp dụng vào
`ForFitChicken_Development` trên `KDADEPTRAI\KDADEPTRAI`. Phần ngày trong tên migration
là UTC. Không chạy migration tự động khi API khởi động.

## Phạm vi

- Domain có 22 entity nghiệp vụ và enum, không tham chiếu EF/Identity.
- Infrastructure chứa fluent configurations, DbContext và migration/snapshot.
- Identity dùng User/Role khóa long và đủ 7 bảng framework. Seed hai role Owner/Operator;
  không tạo tài khoản hoặc mật khẩu mặc định. Chưa có API đăng nhập hay cấu hình phiên/token.
- Không triển khai OperatingExpenses, ngân hàng/QR, hoa hồng hoặc phí vận chuyển.
- DbContext hiện lưu các thuộc tính và khóa scalar. Repository, DTO, validation và hành vi
  nghiệp vụ được bổ sung theo từng use case, không trả trực tiếp entity ra public API.

## Quy ước và các bổ sung từ ERD

- Tiền VND decimal(18,0); giá vốn decimal(18,4), null biểu thị chưa biết.
- Tiếng Việt nvarchar; mã và enum varchar. Enum có CHECK giới hạn tập giá trị.
- Collation Latin1_General_100_CI_AS_SC: không phân biệt hoa/thường, phân biệt dấu.
- Thời điểm datetime2(3), ứng dụng phải truyền UTC; ngày sản xuất dùng DateOnly/date.
- Rowversion cho các bản ghi có thể sửa; đây là token concurrency, không phải thời gian.
- Mọi FK, kể cả Identity, dùng NO ACTION. Các thao tác gỡ role/login/token cần xóa
  liên kết rõ ràng; không dựa vào cascade khi xóa tài khoản.
- OrderItems bổ sung NetWeightGramsSnapshot > 0 và unique OrderId/VariantId;
  LineTotal là computed persisted từ UnitPrice * Quantity.
- StockLocations bổ sung CreatedAtUtc/UpdatedAtUtc, đã đồng bộ DBML.
- ProductImages có FK kép đảm bảo SKU thuộc đúng sản phẩm. GymStockCounts có FK kép
  đảm bảo predecessor thuộc cùng gym; filtered unique giới hạn root và successor đã chốt.
- Filtered unique cho phân bổ Reserved, phiếu OrderIssue Posted và khoản COD chưa hủy.
- CHECK bảo vệ số lượng tồn, điều kiện hàng, endpoint di chuyển, metadata posting/void,
  cấu trúc JSON và trạng thái lease thông báo.

## Giới hạn cần xử lý ở Application

Schema không thay thế transaction nghiệp vụ. Các quy tắc sau chưa được triển khai:

- Chỉ dùng kho chính cho WarehouseBalances và đơn online; không dùng hàng gym để cam kết bán.
- Xác nhận đơn mới giữ hàng; phân bổ đúng SKU/lô Good chưa hết hạn hoặc khóa bán,
  tổng allocations khớp reserved, xử lý cạnh tranh và request lặp.
- Tổng đơn bằng tổng dòng, chuyển trạng thái hợp lệ, thu đủ tiền và quyền thao tác.
- Cấm sửa lịch sử đã chốt, SKU/quy cách đã sử dụng và append-only event/audit.
- Thứ tự thời gian kiểm đếm, khóa đồng bộ giữa kiểm đếm và biến động gym, đủ dòng kiểm đếm,
  tổng phân loại không vượt chênh lệch. Chênh lệch chưa rõ không được mặc định thành bán.
- Quy tắc giờ hết hạn theo nhãn và múi giờ Việt Nam; ngày sản xuất và expiry phải được
  kiểm tra ở dịch vụ nhập lô, không so sánh ngày local với UTC một cách máy móc.
- Chuẩn hóa mã/slug, timestamp/actor đáng tin cậy, DTO nội dung và tham chiếu ảnh trong JSON.
- Job Telegram cùng transaction tạo đơn; worker claim lease/gửi/retry và bảo vệ bí mật.

## Kiểm chứng

Toàn bộ 26 tests đạt: 9 schema/migration, 6 Unit of Work trên LocalDB và 11 HTTP/config.
Test schema chạy migration thật vào database tạm riêng, kiểm tra 29 bảng không tính
__EFMigrationsHistory, hai role, không cascade, stale rowversion, FK kép, CHECK và filtered
unique quan trọng. Fixture chỉ xóa database tạm đúng tên nó đã tạo.

`dotnet ef migrations has-pending-model-changes` xác nhận không lệch model/migration.
Đã kiểm tra database development có migration, 29 bảng và hai role; không có tài khoản.
Theo yêu cầu chuyển server, đã tạo lại schema trên `KDADEPTRAI\KDADEPTRAI`, chạy đạt
cả 26 tests trên server này, rồi xóa đúng database `ForFitChicken_Development` cũ ở LocalDB
sau khi xác minh không có dữ liệu nghiệp vụ. Cấu hình development và mặc định SQL tests
đều dùng server mới với Windows Authentication.
Chưa có API nghiệp vụ hoặc worker Telegram. Bước tiếp theo: xác thực và phân quyền nội bộ.

Hướng dẫn lệnh: [backend README](../../backend/ForFitChicken/README.md).
Khi đổi schema tiếp theo, tạo migration mới; không sửa InitialSchema đã áp dụng.
