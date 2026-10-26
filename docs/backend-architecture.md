# Yêu cầu kiến trúc backend: Unit of Work và rate limiting

Ngày 27/10/2026. Người dùng yêu cầu áp dụng Unit of Work và rate limiting cho API.
Đã triển khai nền IUnitOfWork/EfUnitOfWork, global limiter và named policies ngày 27/10/2026.
Đã bổ sung entity, EF configurations, Identity schema và InitialSchema; migration đã áp dụng
trên SQL Server development `KDADEPTRAI\KDADEPTRAI` (đã chuyển từ LocalDB theo yêu cầu).
Tổng 26 kiểm thử đạt trên server mới (15 SQL, 11 HTTP/configuration).
Các use case nghiệp vụ bên dưới còn cần triển khai; chưa đổi runtime.

## 1. Giữ nguyên Clean Architecture

```
API -> Application, Infrastructure (composition root)
Application -> Domain
Infrastructure -> Application, Domain
Domain -> không phụ thuộc project khác
UnitTest -> Application, Domain; tham chiếu API/Infrastructure chỉ để integration test có category riêng
```

- Application định nghĩa use case, repository/service interfaces và IUnitOfWork.
- Infrastructure triển khai persistence và Unit of Work bằng cùng scoped ForFitChickenDbContext.
- API xử lý HTTP, xác thực/phân quyền và middleware rate limiting.
- Domain không biết DbContext, HTTP status, rate limiter hoặc Telegram.

## 2. Unit of Work — yêu cầu bắt buộc

### 2.1. Trách nhiệm

IUnitOfWork thuộc Application/Abstractions/Persistence; triển khai EfUnitOfWork ở Infrastructure/Persistence.
Contract tối thiểu gồm SaveChangesAsync và abstraction điều phối transaction khi use case cần nhiều lần ghi.
Contract đã chọn: ExecuteInTransactionAsync<T>(Func<CancellationToken,Task<T>>, CancellationToken),
tự lưu các thay đổi còn lại rồi commit, cho phép SaveChanges trung gian. Không trả
IDbContextTransaction hoặc kiểu EF sang Application. Nếu cần đọc Id identity để tạo kết quả,
phải SaveChanges trước khi đọc scalar; auto-save cuối callback không sửa scalar đã trả trước đó.
Scope lỗi bị vô hiệu hóa, không tự replay delegate; nested/ambient và retrying execution strategy bị từ chối.

Mọi repository tham gia cùng use case phải dùng cùng DbContext scoped; không tự new context theo từng repository.
Repository thay đổi/truy vấn aggregate nhưng không tự SaveChanges hoặc commit.
Use case dùng Unit of Work quyết định lưu/hoàn tất nghiệp vụ. Controller không giữ transaction hoặc dùng DbContext trực tiếp.

EF Core DbContext đã có change tracking và SaveChanges như một unit of work;
abstraction bổ sung giúp Application kiểm soát ranh giới use case mà không tham chiếu EF.
Không tạo lớp transaction/logging/cache lặp lại những gì DbContext đã cung cấp.
Không bắt buộc GenericRepository cho mọi bảng hoặc expose IQueryable/DbSet qua Application;
ưu tiên repository theo aggregate và truy vấn chuyên biệt thực sự cần.

### 2.2. Ranh giới atomic quan trọng

| Use case | Những thao tác cùng commit hoặc rollback |
| --- | --- |
| Gửi yêu cầu COD | Order, Items, Event, idempotency result và NotificationJob Telegram |
| Xác nhận đơn | Kiểm tra trạng thái, allocations, tăng Reserved, Confirmed và Event |
| Bàn giao | Kiểm tra hạn/khóa, giảm OnHand/Reserved, Issued, phiếu OrderIssue và trạng thái đơn |
| Hủy trước xuất | Released, giảm Reserved, Cancelled và Event |
| Giao/thu hồi/hủy hàng | Posted document và mọi balance liên quan |
| Chốt kiểm đếm | Kiểm tra mốc, snapshot đủ dòng và trạng thái phiếu |
| Phân loại chênh lệch | Kiểm tra tổng đang phân loại, insert resolution và audit |

SaveChanges đơn lẻ atomic không có nghĩa mọi chuỗi query/cập nhật nhiều bước đã an toàn.
Use case nhiều thao tác SQL hoặc SaveChanges phải có transaction chung và protocol khóa/concurrency theo thiết kế dữ liệu.
UoW không thay rowversion, unique/check constraints, conditional updates hoặc idempotency.

Không gọi Telegram/HTTP, chờ điện thoại hoặc người dùng trong transaction SQL.
Job được lưu cùng đơn, worker gửi sau commit và tạo scope riêng cho mỗi lần xử lý; không inject scoped UoW vào singleton worker.
Không dùng chung một context cho các thao tác song song.
Không mở transaction cho mọi GET hoặc giữ transaction suốt mọi request theo middleware mặc định.

### 2.3. Lỗi và retry

- Exception/cancellation trước commit -> rollback/dispose; không commit phần còn lại.
- Không lồng transaction độc lập hoặc để inner commit ngoài kiểm soát use case.
- Nếu retry transaction, retry toàn nghiệp vụ bằng context/state phù hợp, re-read dữ liệu và giữ idempotency;
  không replay delegate tùy ý có external side effect hoặc chỉ retry SaveChanges trên state cũ.
- Kết quả commit không rõ sau mất kết nối cần xác minh theo key/chứng từ; không mặc định rollback đã chắc chắn xảy ra.
- Chuỗi cùng database dùng local transaction; chưa thêm distributed transaction, RabbitMQ hoặc Redis lock.

## 3. Rate limiting — yêu cầu bắt buộc

### 3.1. Phủ API mặc định

Dùng ASP.NET Core rate limiting middleware. Mọi endpoint nghiệp vụ API có global limiter mặc định;
endpoint mới không được bỏ sót chỉ vì chưa gắn attribute. Thêm named policy theo nhóm khi cần,
không tạo một cấu hình copy-paste cho từng method.

| Nhóm | Cách giới hạn đề xuất |
| --- | --- |
| Public catalog/content/điểm bán | Ngưỡng đọc thoáng theo client/IP, kết hợp cache khi có |
| Login | Ngưỡng chặt theo IP; bổ sung account protection/lockout theo Identity khi làm auth |
| Submit COD | Policy riêng chống spam theo client/IP; idempotency vẫn bắt buộc |
| Admin nghiệp vụ | Theo user id đã xác thực; có baseline bảo vệ tài nguyên |
| Report/export | Giới hạn tần suất hoặc số tác vụ đang chạy, tùy chi phí thực tế |

Các con số request/window là cấu hình và phải đo khi tích hợp, không coi ngưỡng ví dụ là SLA production.
Named policy phải partition theo client/user thay vì vô tình chia một quota duy nhất cho mọi khách.
Một IP có thể nhiều khách dùng chung Wi-Fi gym; không chọn hạn mức thấp đến mức cả gym bị khóa vì một khách.
Không chỉ dựa cookie client tùy tạo để chống spam vì có thể reset; chọn phối hợp IP/identity phù hợp.

### 3.2. Pipeline và phản hồi

- Đặt routing trước middleware dùng endpoint policy; khi partition theo user phải chạy authentication trước limiter đó.
- Trả HTTP 429 có ProblemDetails thống nhất và Retry-After khi thuật toán cung cấp, không redirect hoặc trả 200 giả.
- Queue mặc định không chờ lâu cho login/submit, tránh giữ request và khiến người dùng bấm lặp.
- Reverse proxy chỉ dùng forwarded headers từ proxy/network tin cậy được cấu hình; không tin X-Forwarded-For tùy ý của client.
- Health/Swagger là endpoint vận hành, không phải API nghiệp vụ; nếu cần ngoại lệ phải khai báo rõ và review,
  không thêm DisableRateLimiting tùy tiện để làm test qua.
- Client không được retry mọi POST 429 tự động như request mới; submit COD tiếp tục giữ key theo hợp đồng idempotency.
- Không đưa tên tài khoản/địa chỉ/secret vào partition key trong log; metric phục vụ chỉnh ngưỡng.

### 3.3. Giới hạn cần biết

Rate limiter trong process có counter riêng mỗi API instance. MVP một instance có thể dùng cấu hình này;
khi scale nhiều instance cần gateway/counter dùng chung, không tự giải quyết bằng Redis lock.
Concurrency limiter là hạn chế số request chạy cùng lúc, không phải chống ghi đè hoặc bán vượt tồn.
Rate limiting không thay authorization, validation, idempotency, transaction hoặc bảo vệ ở tầng hosting.

## 4. Kiểm thử khi triển khai

- Unit test Application use case commit/lỗi/cancellation bằng abstraction, không tạo test luôn đúng.
- SQL Server integration test dùng transaction thật chứng minh lỗi giữa chừng không để lại nửa đơn/giữ hàng;
  không dùng EF InMemory để chứng minh rollback/locking của SQL Server.
- Kiểm tra repository/UoW cùng scoped DbContext và không dispose context khi repository khác còn dùng trong scope.
- HTTP test thật: vượt quota ->429, client độc lập không dùng chung named-policy bucket, cửa sổ reset,
  retry headers/lỗi đúng contract và API mới vẫn có baseline.
- Thử pipeline có auth/proxy đúng cấu hình, ngưỡng submit không tạo thêm đơn hoặc spam Telegram.

Giữ năm project theo yêu cầu, UnitTest chứa thư mục Unit và Integration với category riêng.
Test-only references tới API/Infrastructure không thay đổi dependency production;
SQL tests opt-in tạo database riêng, không dùng/xóa database ứng dụng. Không gọi unit test mock
là bằng chứng end-to-end hoặc test concurrency nghiệp vụ chưa tồn tại.

Đã kiểm chứng: 6 test SQL transaction, 6 HTTP integration, 5 validation cấu hình.
Health có ngoại lệ rõ để liveness không tiêu quota; Swagger Development được xử lý trước limiter.
Ngưỡng khởi điểm: global/catalog/admin 120/phút, login 5/phút, checkout 10/phút,
reports 2 đồng thời theo user/client; cần restart sau sửa settings, chưa phải ngưỡng production đã đo.

## 5. Thứ tự triển khai cập nhật

1. Entity/EF configurations và Identity schema: đã hoàn thành.
2. Unit of Work và abstraction transaction scoped: đã hoàn thành; repository bổ sung theo use case.
3. Migration trên SQL Server development: đã hoàn thành, có kiểm thử database tạm riêng.
4. Global/named rate limiting và phản hồi 429: đã hoàn thành.
5. Làm auth, catalog, kho, COD, Telegram, gym; bổ sung policy theo endpoint và kiểm thử cùng từng module.

Không cần thêm bảng database chỉ để lưu Unit of Work hoặc rate limiting trong process.

## 6. Nguồn kỹ thuật

- [EF Core DbContext lifetime và Unit of Work](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/).
- [ASP.NET Core rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-8.0).
