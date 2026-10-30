# API quản lý nhân viên — Owner

Ngày 29/10/2026. Chỉ dành cho tài khoản có role Owner, dùng cookie HTTPS + X-CSRF-TOKEN như API admin hiện có.
Không tạo thêm role Admin; tất cả nhân viên được tạo bởi API này có duy nhất role Operator.

| Method | Đường dẫn | Input / hành vi |
|---|---|---|
| GET | /api/admin/employees?search=&active=true&page=1&size=20 | Tìm username/displayName, lọc IsActive; bỏ active để lấy tất cả. size tối đa100, lọc trước phân trang. |
| GET | /api/admin/employees/{id} | Chỉ tìm Operator không đồng thời có role Owner. |
| POST | /api/admin/employees | userName, displayName, password. Trả201 + Location và EmployeeView. |
| PUT | /api/admin/employees/{id} | version, displayName. Không đổi username hoặc role. |
| PUT | /api/admin/employees/{id}/status | version, isActive. false khóa hành chính; true mở và xóa khóa tạm do sai mật khẩu. Cả hai thu hồi cookie cũ. |
| POST | /api/admin/employees/{id}/password | version, password mới do Owner lựa chọn. Thu hồi mọi phiên cũ, không tự mở khóa tài khoản đang IsActive=false. |

GET trả EmployeeView: id, userName, displayName, isActive, isTemporarilyLocked, version.
List trả Page với items/total/pageNumber/pageSize. Không trả hash/security stamp/email/Identity internals/password.
Version opaque dùng lại nguyên chuỗi nhận từ server; sau mỗi cập nhật dùng version mới. Không tự retry mutation409.
Username3–100 ký tự ASCII chữ/số/dấu chấm/gạch dưới/gạch ngang, bắt đầu chữ/số; tên hiển thị tối đa150, không để trắng.
Password12–128 ký tự, có chữ hoa/thường/số/ký tự đặc biệt theo Identity policy hiện có. Không có mật khẩu mặc định.
Không đưa password thật vào chat/docs/Git/logs. Body password chỉ gửi HTTPS; audit không lưu body/password/hash.

## Quyền, lỗi và vòng đời
Anonymous401, Operator403. Owner không thể dùng API này để tạo/quản lý Owner, tự sửa chính mình hoặc nâng quyền nhân viên.
Không hỗ trợ xóa nhân viên để giữ actor lịch sử; dùng khóa IsActive. Không có flow email/quên mật khẩu/mời nhân viên.
EmployeeCreated/EmployeeNameChanged/EmployeeEnabled/EmployeeDisabled/EmployeePasswordReset ghi AuditLog bằng ID.
Tạo user + role + audit hoặc cập nhật + audit đều trong UoW transaction/SQL business lock; Identity dùng cùng DbContext.
401 ở request tiếp theo khi cookie bị thu hồi; request đã chạy trước khi khóa có thể hoàn tất. Không làm mất lịch sử kho/đơn.
Mở tài khoản trả isActive=true nhưng không phục hồi cookie cũ. Reset password không trả token hoặc mật khẩu lại trong response.

Codes: EMPLOYEE_NOT_FOUND404 (kể cả target Owner), USERNAME_EXISTS409, VERSION_CONFLICT409,
EMPLOYEE_VALIDATION_FAILED400 và validation400, SELF_CHANGE_FORBIDDEN400, CSRF_INVALID400.
isTemporarilyLocked là khóa brute-force tạm, phân biệt với isActive. POST tạo lại username trùng không reset mật khẩu.

## Triển khai
Không thêm migration: dùng IsActive, DisplayName, Identity hiện có. Rebuild/restart API để có routes mới.
Frontend quản lý tài khoản chưa triển khai trong lượt API này. Owner có thể thử Swagger sau khi đăng nhập và lấy CSRF mới.
Source Application/Authentication/EmployeeService.cs; Infrastructure/Identity/EmployeeAccounts.cs; API/Controllers/EmployeesController.cs.
Build/test dùng artifacts/employees để không đụng DLL Visual Studio. Không dừng server người dùng hoặc tạo tài khoản thật.
Kết quả kiểm chứng cập nhật trong HANDOFF/progress; không nhầm test GUID disposable với dữ liệu kinh doanh.

Kiểm chứng hoàn tất:14/14 AdminAuthTests đạt trên SQL database GUID disposable; test Employees bổ sung chạy lại đạt. Không password/account thật. Swagger live có route/schema sau rebuild; docs/openapi/mvp.json là snapshot cũ, cần xuất lại trong lần cập nhật OpenAPI riêng. Chưa thêm giao diện quản lý nhân viên.

## UI nhân viên 29/10
Owner có /quan-tri/nhan-vien, menu Nhân viên; Operator bị ẩn menu và chặn route. Danh sách mặc định active, search username/displayName, paging20; tạo Operator, sửa tên, khóa/mở khóa, reset password với xác nhận. Mật khẩu nhập lại, hiện/ẩn, không lưu localStorage hoặc tự điền mặc định; unmount đóng modal xóa khỏi memory. Conflict409 khóa submit, cho GET lại version và bỏ form cũ, không replay mutation. API employee mới phải rebuild/restart backend. Không thêm migration/UI quyền Owner khác không đổi. Build frontend kiểm chứng trong lượt này; chưa live UAT/tài khoản thật, chưa UI fixture cho employees. Chưa commit/push. Đọc employees-api.md cho contract.
