# Tiến độ đăng nhập và phân quyền quản trị

Cập nhật ngày 27/10/2026. Phạm vi: tài khoản nội bộ Owner/Operator; khách mua hàng không đăng ký.

## Quyết định triển khai

- React và API chạy cùng origin qua reverse proxy khi triển khai; development dùng proxy `/api`.
- Dùng cookie phiên HttpOnly/Secure, SameSite Strict. Không lưu token đăng nhập trong localStorage.
- Các POST đăng nhập/đăng xuất yêu cầu antiforgery token qua header X-CSRF-TOKEN.
- Kiểm tra trạng thái tài khoản, security stamp và role hiện tại mỗi request đã đăng nhập.
- Tạo Owner đầu tiên qua chế độ lệnh riêng, thông tin từ biến môi trường do script nhập bảo mật cung cấp; không có đăng ký công khai hoặc mật khẩu mặc định.
- Giới hạn đăng nhập theo IP đã có; bổ sung khóa tài khoản sau nhiều lần sai mật khẩu.

## Các mốc

- [x] Kiểm tra code nền và schema Identity hiện có; working tree sạch trước khi bắt đầu.
- [x] Chốt phương án phiên cookie và CSRF cho frontend cùng origin.
- [x] Abstraction Application, dịch vụ Identity và bootstrap Owner đã viết và kiểm thử.
- [x] API csrf/login/logout/me, policy Owner/nhân viên, CSRF và phản hồi lỗi đã viết và kiểm thử.
- [x] Lượt đầu: 35/35 tests đạt, gồm 9 tests auth mới và 26 tests nền cũ.
- [x] Kiểm chứng bổ sung bootstrap đồng thời, quyền bị gỡ, đầu vào không hợp lệ và cookie hết hạn/bị sửa.
- [x] Tài liệu tích hợp frontend và hướng dẫn thiết lập Owner: [admin-auth-api.md](admin-auth-api.md).
- [x] Nghiệm thu cuối: 39/39 tests đạt, 0 failed, 0 skipped; không lệch model/migration.

Chưa tạo tài khoản thật; không tự chọn mật khẩu cho nhóm. Chưa commit các thay đổi của giai đoạn này.

## Nhật ký

1. Đã thêm Application abstractions và triển khai Identity trong Infrastructure.
2. Bootstrap dùng khóa transaction trên role Owner để tránh hai tiến trình cùng tạo Owner đầu tiên.
3. Cookie hết hạn sau 8 giờ, không tự gia hạn; logout thu hồi mọi phiên bằng security stamp.
4. Đã thêm fallback policy bảo vệ endpoint mặc định; health và csrf/login được mở rõ ràng.
5. Đã build và kiểm thử HTTP với cookie thật, không dùng fake authentication để chứng minh luồng đăng nhập.
6. Lượt đầu đã đạt: login/cookie, CSRF, 401/403, role đổi tức thời, logout mọi phiên,
   vô hiệu hóa tài khoản, stamp thay đổi, lockout, rate limiting endpoint thật và bootstrap không reset Owner.
7. Đã bổ sung test hai tiến trình tạo Owner đồng thời; chỉ một tài khoản được tạo.
8. Kết quả cuối: 13 tests auth mới + 26 tests nền = 39 tests đạt trên SQL Server tạm riêng.
9. Lệnh CLI thiếu cấu hình trả exit 1/InvalidBootstrapInput đúng thiết kế; script PowerShell đã kiểm tra cú pháp.
   Script tương tác chưa được dùng để tạo tài khoản thật; việc tạo Owner bằng dịch vụ đã được kiểm thử trên DB tạm.
10. Model không thay đổi nên không cần migration mới. Tài liệu API và README đã đồng bộ.

## Việc tiếp theo

- Người dùng chạy `backend/ForFitChicken/scripts/bootstrap-owner.ps1` để tự chọn thông tin Owner.
- Tiếp tục API danh mục sản phẩm; gắn policy và CSRF cho thao tác quản trị, AllowAnonymous cho API công khai.
- Quản lý Operator, đổi/khôi phục mật khẩu và hạ tầng production chưa thuộc phần auth đã nghiệm thu này.
