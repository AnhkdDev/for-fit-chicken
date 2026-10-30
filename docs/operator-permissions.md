# Phân quyền Operator — 29/10/2026

Quyết định người dùng: Operator quản lý sản phẩm, lô/kho và gym; được nhập/xuất và giao/kiểm đếm ký gửi.
Các module khác chỉ Owner. Giữ2 role, không thêm role hoặc migration. Đây là checkpoint backend, chưa thu gọn frontend.

| Nhóm API | Operator | Owner |
|---|---|---|
| auth/csrf/login/me/logout | Có | Có |
| admin/products đọc/tạo nháp/sửa/SKU/ảnh | Có | Có |
| đổi IsPublished, IsFeatured, tắt IsActive của sản phẩm đang công bố | 403 | Có |
| xóa sản phẩm/lô đủ điều kiện | 403 | Có |
| inventory locations/batches/balances/documents đọc | Có | Có |
| tạo lô, khóa/mở bán lô, nhập/xuất/transfer/ghi nhận hỏng/thu hồi hợp lệ | Có | Có |
| Adjustment, correction phiếu, giải phóng quarantine, khởi tạo MAIN | 403 | Có |
| giá vốn GET/PUT | 403 | Có |
| gym thông tin/stock/counts; kiểm đếm Regular | Có | Có |
| kiểm đếm mở đầu (baseline) | 403 | Có |
| gym resolutions đọc/xác nhận/hủy xác nhận | 403 | Có |
| admin/orders toàn bộ (cả đọc và manual create) | 403 | Có |
| admin/content toàn bộ (cả nháp/preview data/trưng bày) | 403 | Có |
| employees/reports/notifications/admin media quota/assets/cleanup | 403 | Có |
| media upload phục vụ sản phẩm/gym và GET ảnh | Có như cũ | Có |

Public products/gyms/content/checkout vẫn dành khách không đăng nhập; không được nhầm với quyền admin orders.
DTO kho không chứa giá vốn hoặc người nhận đơn; snapshot gym không phải tồn thời gian thực. Không tự tính thiếu thành bán.
Gym thông tin/công bố địa điểm vẫn nằm trong quyền quản lý gym; CMS/banner/home display không thuộc quyền Operator.
Owner tiếp tục có tất cả quyền trước đó. Chặn backend thực sự khi gọi trực tiếp URL, không phụ thuộc menu.

## Source
- CommerceControllers.cs: orders/content Owner, gym resolutions Owner; catalog Save truyền Owner flag từ principal.
- CatalogService.SaveAsync kiểm tra trạng thái công bố/nổi bật/ẩn trong cùng transaction trước thay đổi dữ liệu.
  Các caller HTTP phải truyền Owner flag; default legacy cho direct service callers được giữ để tương thích.
- InventoryService.PostAsync: kiểm tra Adjustment/correction trước idempotency lookup/replay để không vượt quyền khi dùng operationKey cũ.
- Fallback authorization chuyển Owner cho endpoint không khai báo rõ. Staff vẫn chỉ Owner/Operator, chỉ dùng module được cho phép.
- Cookie/Secure/CSRF/rate limit/UoW/business lock/9GB quota không thay đổi. Không cấp role mới/reset counters.

## Frontend tiếp theo
UI hiện còn menu/trang không phù hợp cho Operator: dashboard/orders/content có thể nhận403 sau restart.
Lượt này người dùng yêu cầu backend. Cần cập nhật landing Operator về sản phẩm, menu chỉ sản phẩm/kho/gym,
ẩn công bố/nổi bật sản phẩm và xác nhận chênh lệch, không gọi private CMS ở preview. Staff upload vẫn phục vụ form sản phẩm/gym.
Giữ frontend còn lại Owner như cũ. Đánh dấu backlog ở frontend-progress, không coi UI đã đồng bộ.

## Kiểm chứng
Build và nhóm HTTP auth + commerce SQL trên DB GUID disposable, không viết dữ liệu kinh doanh thật.
Test mới đăng nhập Owner/Operator với cookie thật in-memory HTTPS; GET allowed200/blocked403,
POST orders/content/gym-resolve/media-maintenance403, Operator tạo nháp200 và công bố403, Adjustment403.
Fallback probe test phân biệt endpoint explicit Staff với fallback mới Owner. Không khởi động listener trên7010/5195.
Kết quả cuối ghi tại HANDOFF/progress. Chưa commit/push; rebuild/restart API để áp dụng, không cần migration.

Kết quả chốt29/10:34/34 tests AdminAuthTests + CommerceTests SQL-enabled đạt (bao gồm ma trận quyền mới). Factory HTTPS in-memory đã dispose, không có API test listener để dừng; không dừng Visual Studio của người dùng. Không live UAT/tạo account thật/xóa DB, không đổi frontend lượt này. Chưa commit/push.

## Operator frontend đồng bộ29/10
Menu Operator chỉ san-pham/kho/phong-gym; index redirect sang sản phẩm. Routes đơn/nội dung/trưng bày/thư viện/nhân viên/báo cáo/thông báo bị chặn mà không mount component, tránh gọi API cấm. Product Operator ẩn featured/publish step, khóa active khi product đang publish; vẫn form ảnh upload phục vụ sản phẩm. Owner giữ nguyên chức năng.
Build và14/14 Node tests đạt (gồm2 permission tests cho menu/subroutes). Không thử account Operator thật hoặc browser fixture lượt này; backend34 tests trước đó là bằng chứng phân quyền server, không phải UI UAT. Không chạy listener/test DB, không commit/push. Cần backend mới và reload frontend để nghiệm thu role thật.
