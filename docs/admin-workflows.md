# Admin phục vụ công việc — 29/10/2026

Người dùng dừng chỉnh storefront và yêu cầu admin độc lập phong cách trưng bày, ưu tiên nhập liệu.
Không đổi luồng khách, API hay quyền Owner/Operator. Chưa commit/push.

## Đã triển khai
- Shell trung tính trắng/xám, sans-serif dễ đọc, bảng/form cỡ chữ rõ; actions gần dữ liệu, responsive.
- Sản phẩm: trang riêng theo bước thông tin → SKU/giá → ảnh → công bố; tự gợi slug, nhóm trường tùy chọn,
  sửa SKU bằng form, phiên bản dữ liệu giữ theo API. Không công bố khi thiếu ảnh/SKU hoặc có nội dung chưa lưu.
- Nội dung: chọn/upload ảnh trực quan thay nhập ID; sắp thứ tự, xem trước, phân biệt lưu nháp/công bố,
  Operator chỉ nháp/upload. Publish dùng phiên bản đã lưu, không âm thầm bỏ thay đổi form.
- Đơn: lọc theo việc cần làm, tìm mã/phone, gọi khách từ chi tiết; form nhập đơn thủ công chọn SKU bằng tên,
  chỉnh thông tin pending; confirm/cancel/handover và COD vẫn là các thao tác có xác nhận rõ.
- Dashboard là việc cần làm và đường tắt, không banner quảng cáo. Modal admin rộng hơn và nút/form thuận tiện.

## Bảo trì
Admin-specific CSS được scope .admin-shell/.login-page để storefront giữ nguyên. Hooks/session/UI helpers
tách khỏi admin.tsx để module nhập liệu không tạo vòng phụ thuộc. Không lưu recipient/password vào localStorage.
Image picker chỉ /api/media và Owner assets API, không raw R2/presigned/secrets; giữ input5MiB và quota backend.
Các mutation dùng cookie/CSRF/version/idempotency hiện có, không tự replay 409 hoặc HTTP không rõ kết quả.
Không tạo/chỉnh dữ liệu business thật để demo. UI fixture riêng không phải bằng chứng SQL/R2/Telegram thật.

Kiểm chứng/checklist cuối cập nhật tại frontend-progress.md.

## Cách dùng nhanh
1. Sản phẩm → Tạo sản phẩm: nhập tên (tự gợi đường dẫn), mô tả ngắn, lưu nháp. Các trường dài nằm trong
   mục mở rộng. Tạo xong chuyển URL sang ID để có thể quay lại sửa.
2. Bước Phân loại & giá → Thêm phân loại, nhập SKU/quy cách/gram/giá. Có nút Sửa cho SKU hiện có.
3. Bước Hình ảnh → chọn ảnh đã tải (Owner), hoặc upload/kéo thả ảnh (Owner/Operator). Gỡ chỉ bỏ liên kết.
4. Bước Công bố kiểm tra active + SKU active + ảnh + thông tin đã lưu. Tồn kho nhập ở module Kho riêng.
5. Đơn hàng: tab Chờ gọi/Chờ chuẩn bị/Đang chuẩn bị/Đang giao; tìm mã hoặc phone. Chi tiết có link gọi và
   sửa người nhận khi pending. Sau gọi mới Xác nhận. Thu COD là thao tác Owner riêng sau bàn giao/hoàn tất.
6. Tạo đơn thủ công: chọn nguồn, người nhận, tìm SKU theo tên/mã, thêm quantity. Giá do backend tính lại;
   không tự giữ tồn. Lần gửi chưa rõ kết quả phải kiểm tra danh sách, không tạo đơn thay thế ngay.
7. Nội dung: chọn khu vực → nhập/chọn ảnh/xếp thứ tự → Lưu bản nháp → Owner công bố bản đã lưu.
   Preview chỉ kiểm tra nội dung, không mô phỏng chính xác mọi bố cục storefront.

## Phạm vi kiểm chứng và phần còn lại
Build + 7 tests đạt. Đã thao tác browser ở UI fixtures với luồng tạo nháp/SKU/chọn ảnh/công bố,
CMS lưu/công bố, tạo COD2×37000=74000, sửa địa chỉ pending giữ nguyên tổng tiền; không dùng dữ liệu thật.
Sản phẩm/nội dung đo viewport320/390/768/1280 không tràn; menu và thẻ đơn mobile đã kiểm tra390.
Chưa test đăng nhập thật, permission Operator trong browser, upload thật/R2, lỗi đồng thời409 thật hoặc SQL writes lượt này.
Backend API/quyền/quota không thay đổi. Void receipt, batch cost/block, phiếu nâng cao, gym resolution còn theo backlog cũ.
Sau mở UI thật cần nghiệm thu với Owner tự nhập credentials; không gửi bí mật vào chat/docs/Git.

## Login + chẩn đoán 409 — 29/10/2026
- Sửa lỗi client bỏ qua ProblemDetails.title (middleware business gửi title, không detail); helper lib/problem.ts hỗ trợ code/detail/validation/title và fallback, 12 frontend tests đạt, build đạt. Không đổi backend status/rules.
- Media delete error trước đây nằm sau native modal, nay hiển thị trong modal; ảnh dưới24h khóa nút, hiển thị mốc chờ tối thiểu và nút tải lại. Backend vẫn kiểm tra thời gian grace cấu hình, references và quota.
- DeleteRecord có link tới sản phẩm/trưng bày và tải lại khi VERSION_CONFLICT; không auto replay DELETE. 409 vẫn là phản hồi đúng khi có liên kết.
- SQL chỉ đọc số đếm trên DB đang dùng: product1 có1 batch+1 order; batch1 có1 balance+1 documentline; media1 khoảng18 giờ, chưa đủ24h. Không xóa/sửa dữ liệu thật và không gọi raw R2. Không lưu PII/secrets.
- Login form logo/căn giữa/sans/input16px/hiện mật khẩu/fieldset busy; giữ Secure cookie, CSRF và auth endpoint cũ. Không tạo/reset account.
- Browser fixture xác minh product409 hiện title+link; media409 hiện ngay trong modal; logout vào login, desktop và390px không overflow. Không đăng nhập Owner thật, không xóa sản phẩm/lô/ảnh thật. Không chạy lại backend tests vì chỉ đổi frontend.
- Preview admin-login-revised.jpg. Chưa commit/push; dừng fixture sau test, giữ server người dùng. Dòng React DevTools là thông báo phát triển, không là lỗi.
