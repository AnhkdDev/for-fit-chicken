# API đăng nhập quản trị

Cập nhật 27/10/2026. Tiến độ: [auth-progress.md](auth-progress.md).
Đây là API tài khoản nội bộ; không có đăng ký tài khoản khách hoặc endpoint tạo Owner công khai.

## Phiên và quyền

React và API phải cùng origin qua proxy, dùng HTTPS. Cookie `__Host-ForFitChicken.Admin`
có HttpOnly, Secure, SameSite Strict, Path=/, không có Domain. Cookie phiên hết hạn sau 8 giờ,
không tự gia hạn; không có remember-me hoặc refresh token. Trình duyệt quản lý cookie;
frontend không đọc cookie hay lưu token đăng nhập vào localStorage.

Mỗi request mang cookie được kiểm tra tài khoản đang hoạt động, chưa bị lockout, security stamp
và role hiện tại trong database. Role đổi áp dụng ngay ở request kế tiếp; đổi stamp, vô hiệu hóa,
lockout hoặc mất mọi role quản trị làm phiên bị từ chối. Đăng xuất thu hồi **mọi phiên** của
tài khoản, không chỉ tab hiện tại. Một request đã được xác thực và đang chạy có thể hoàn thành.

Policy `AdminStaff` chấp nhận Owner/Operator; `OwnerOnly` chỉ Owner. Fallback policy bảo vệ
endpoint mới mặc định. API công khai tương lai phải gắn AllowAnonymous rõ ràng; API quản trị
phải chọn policy phù hợp. Chưa có API quản lý tài khoản hoặc API tài chính trong giai đoạn này.
Các route `auth-policy-probe` và `rate-limit-probe` chỉ tồn tại trong assembly test.

## Các endpoint

| Method / route | Quyền | Kết quả |
| --- | --- | --- |
| GET `/api/admin/auth/csrf` | Công khai | 200 `{ "requestToken": "..." }`, đặt cookie CSRF HttpOnly |
| POST `/api/admin/auth/login` | Công khai, cần CSRF | 200 profile và cookie đăng nhập |
| GET `/api/admin/auth/me` | AdminStaff | 200 profile hiện tại; 401 nếu phiên không hợp lệ |
| POST `/api/admin/auth/logout` | AdminStaff, cần CSRF | 204, xóa cookie và đổi security stamp thu hồi mọi phiên |

Login body:

```json
{ "userName": "your-owner-name", "password": "your-password" }
```

Username bắt buộc, tối đa 100 ký tự; password bắt buộc, tối đa 128 ký tự. Profile:

```json
{ "id": 1, "userName": "your-owner-name", "displayName": "Chủ nhóm", "roles": ["Owner"] }
```

Chỉ trả profile; không trả passwordHash, securityStamp hoặc trường bảo mật Identity.
Swagger Development có schema request/response. ID trả dạng số hiện dùng bigint SQL;
frontend không dùng ID nội bộ làm bí mật hay khóa xác thực.

## CSRF và trình tự frontend

Global MVC filter kiểm tra CSRF cho mọi method thay đổi dữ liệu. GET/HEAD/OPTIONS/TRACE không
cần token. Login cũng bắt buộc token để tránh login CSRF. Header cần gửi: `X-CSRF-TOKEN`.
Token gắn với danh tính: sau đăng nhập phải lấy token mới; token anonymous cũ không dùng logout.
Không retry thao tác thay đổi dữ liệu tự động chỉ vì gặp lỗi CSRF; lấy lại token rồi cho phép thử lại.

```javascript
// Cùng origin; không cấu hình CORS mở để dùng snippet này từ một domain khác.
async function getCsrfToken() {
  const response = await fetch('/api/admin/auth/csrf', { credentials: 'same-origin', cache: 'no-store' });
  if (!response.ok) throw new Error('Không lấy được CSRF token');
  return (await response.json()).requestToken;
}

async function login(userName, password) {
  const token = await getCsrfToken();
  const response = await fetch('/api/admin/auth/login', {
    method: 'POST', credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
    body: JSON.stringify({ userName, password })
  });
  if (!response.ok) throw await response.json();
  const profile = await response.json();
  const authenticatedCsrfToken = await getCsrfToken();
  return { profile, csrfToken: authenticatedCsrfToken }; // Giữ token CSRF trong bộ nhớ.
}
```

Reload trang: gọi `me` để phục hồi trạng thái đăng nhập và lấy CSRF token mới. Khi 401, xóa state
quản trị ở frontend và về trang đăng nhập. Khi logout, gửi CSRF token mới nhất, rồi xóa state
và token trong bộ nhớ. Không coi việc ẩn nút là kiểm soát quyền: server kiểm tra policy.

Development: chạy API profile `https` tại `https://localhost:7010`. React dev server cũng dùng
HTTPS và proxy `/api` về API, giữ nguyên origin phía trình duyệt; đừng đổi host thành 127.0.0.1
ở giữa luồng. HTTP profile phù hợp liveness, không phù hợp cookie Secure. Trên HTTPS localhost,
Swagger có thể giữ cookie, nhưng POST cần tự lấy token và gửi header; Swagger chưa có nút tự động
đăng nhập hay chèn CSRF token. Dùng frontend/fetch hoặc HTTP client có cookie jar để thử đầy đủ.

## Mã lỗi và giới hạn

Lỗi auth trả ProblemDetails `application/problem+json`, không redirect sang HTML:

| HTTP | code | Ý nghĩa |
| --- | --- | --- |
| 400 | CSRF_INVALID | Token thiếu, sai, hoặc không khớp danh tính/cookie |
| 400 | Không có code riêng | ValidationProblemDetails: thiếu/sai độ dài dữ liệu đầu vào |
| 401 | LOGIN_FAILED | Sai mật khẩu, không có tài khoản, bị khóa/vô hiệu hóa hoặc thiếu role quản trị; dùng chung thông báo |
| 401 | AUTH_REQUIRED | Chưa đăng nhập hoặc phiên đã hết hiệu lực |
| 403 | ACCESS_DENIED | Có phiên nhưng không đủ quyền |
| 409 | SESSION_REVOKE_FAILED | Cập nhật security stamp thất bại; thử đăng xuất lại |
| 429 | RATE_LIMIT_EXCEEDED | Vượt quota; xem Retry-After nếu có |

Login mặc định 5 request/IP/phút, global 120/IP/phút, admin 120/user/phút.
Identity khóa 15 phút sau 5 lần sai mật khẩu; login thành công reset bộ đếm lỗi.
Rate limiter và lockout là hai cơ chế khác nhau. Quota còn tính request bị CSRF/validation từ chối.
Không ghi request body login, password hay cookie vào log; chưa thêm request-body logging.

## Tạo Owner đầu tiên

Database development dùng `KDADEPTRAI\KDADEPTRAI`, Windows Authentication.
Migration đã có role Owner/Operator; bootstrap không tự migrate và không mở HTTP listener.
Từ thư mục `backend/ForFitChicken`, chạy:

```powershell
.\scripts\bootstrap-owner.ps1
```

Script hỏi username, tên hiển thị và password che ký tự. Password cần 12–128 ký tự, có chữ hoa,
chữ thường, số và ký tự đặc biệt. Username theo tập ký tự Identity mặc định (chữ ASCII, số,
`- . _ @ +`). Biến môi trường chỉ đặt trong process và được khôi phục khi lệnh kết thúc;
không đưa password vào tham số dòng lệnh hoặc file tracked. Không chạy bằng transcript ghi bí mật.

Backend cung cấp `--bootstrap-owner`; biến cấu hình `BootstrapOwner__UserName`,
`BootstrapOwner__DisplayName`, `BootstrapOwner__Password` cũng có thể được cấp từ môi trường
tin cậy. Không lưu chúng trong appsettings hoặc đưa vào lịch sử lệnh. CLI exit 0 khi tạo thành công,
exit 1 khi bị từ chối. OwnerAlreadyExists nghĩa là không tạo mới hoặc reset Owner hiện có.
Khóa UPDLOCK/HOLDLOCK và Unit of Work transaction ngăn hai tiến trình cùng tạo Owner đầu tiên.

Chưa chạy tạo tài khoản thật vì người dùng chưa chọn username/password. Không có mật khẩu mặc định.
Khôi phục tài khoản bị mất, đổi mật khẩu, tạo Operator và quản lý tài khoản là các chức năng tiếp theo.

## Triển khai

Reverse proxy phải giữ HTTPS phía ứng dụng hoặc cấu hình trusted forwarded headers trước redirect/auth;
không tin header forwarded từ client tùy ý. Lưu bền và bảo vệ Data Protection key ring khi triển khai
để cookie không mất hiệu lực ngoài ý muốn sau restart; nhiều API instance phải chia sẻ key ring và
application name. Chưa cấu hình hạ tầng production/CORS khác origin trong bước này.

Tham chiếu: [Microsoft — antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0),
[Microsoft — cookie ValidatePrincipal](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-8.0).
