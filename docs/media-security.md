# Quy tắc bảo vệ ảnh và chi phí R2

Cập nhật 29/10/2026. Source và migration mới đã triển khai; kết quả chạy xem `media-security-progress.md`.
Chỉ `IMediaStorage` qua `GovernedMediaStorage` được dùng cho nghiệp vụ. Không gọi raw R2 adapter trực tiếp
để upload trong controller/use case mới vì sẽ bỏ qua quota. Application giữ nghiệp vụ trong MediaPolicyService;
Infrastructure xử lý SQL/HTTP/Skia/cache. Các ảnh local cũ tiếp tục đọc theo key, không tự chuyển file.

## Quyền và giới hạn

- Owner **và Operator** được upload, cần cookie + CSRF. Khách không được upload.
- Input PNG/JPEG/WebP tối đa **5 MiB = 5.242.880 byte**. HTTP multipart tối đa 6 MiB (bao gồm overhead).
- Signature hợp lệ chưa đủ: Skia decode thật, chặn ảnh động, tối đa 24 triệu pixel và 10000 pixel/chiều.
- Auto-orient EXIF, không upscale; vừa khung 1920×1920; WebP quality 80. Chỉ lưu bản tối ưu, không lưu gốc.
  Re-encode bỏ metadata EXIF/GPS; alpha được giữ. Không có biến thể thumbnail/mobile riêng ở mốc này.
- 2 tác vụ upload/resize mỗi process, 4 cloud-read/cache-fetch active mỗi process; không xếp hàng upload vô hạn.
  Với ảnh đầu vào lớn nên dùng VPS tối thiểu 1 GB RAM, giới hạn memory/CPU ở process/container và test theo thực tế.

| Cấu hình | Mặc định | Ý nghĩa |
| --- | --- | --- |
| `Media:Security:MaxStoredBytes` | 9000000000 | 9 GB thập phân, **không reset**; chỉ được cấu hình thấp hơn |
| `DailyUploads` | 100 | Admission/ngày toàn DB, kể cả decode/upload sau đó lỗi |
| `DailyInputBytes` | 200000000 | Input byte/ngày toàn DB |
| `PerUserDailyUploads` | 50 | Admission/ngày/tài khoản |
| `ActiveUploads` | 2 | Cloud reservation active toàn DB; tối đa cấu hình 4 |
| `MonthlyClassA` | 100000 | PUT/LIST logical operation/tháng UTC; tối đa cấu hình 500000 |
| `MonthlyClassB` | 1000000 | GET logical operation/tháng UTC; tối đa cấu hình 5000000 |
| `ReconcileMaxAgeHours` | 24 | Không upload khi snapshot cũ quá ngưỡng; cấu hình tối đa 24 |
| `OrphanGraceHours` | 24 | Thời gian chờ tối thiểu trước Owner dọn orphan |
| `RateLimiting:Upload:PermitLimit` | 10 | Request/phút/tài khoản (in-memory mỗi process) |

Các counter ngày/tháng dùng **UTC** và lưu SQL. Rate limiter theo phút/gate/cache là local theo process;
quota bytes, daily counters, cloud operation budgets và active cloud reservations dùng chung SQL cho nhiều VPS.
Ngân sách operations không phải bản sao hóa đơn: thao tác ngoài app/khóa bị lộ không đi qua bộ đếm;
chu kỳ Cloudflare có thể khác tháng UTC. Dùng bucket riêng và để khoảng trống ngân sách cho việc khác.

## Quota không reset và lỗi không rõ kết quả

Ba bảng vận hành mới:

| Bảng | Vai trò |
| --- | --- |
| `MediaStorageStates` | Singleton Id=1: scope hash endpoint+bucket, tổng byte charged, pause, snapshot, lease đối soát |
| `MediaObjectReservations` | Key, byte, actor, state và thời điểm; lịch sử không tự xóa |
| `MediaUsageCounters` | Counter upload ngày/user và A/B tháng |

Luồng upload R2:
1. Admission SQL theo ngày -> kiểm tra/encode ảnh (không HTTP).
2. Transaction + business SQL lock: kiểm tra pause/baseline/dung lượng/gate, giữ **byte output chính xác**,
   ghi Reserved + ngân sách PUT. Commit transaction.
3. PUT lên R2 ngoài transaction, không SDK auto-retry. Thành công -> ghi Stored bằng UoW mới.
4. Timeout/cancel/commit chưa rõ -> Unknown hoặc Reserved vẫn charged. **Không tự giải phóng khi hết lease.**
5. Catalog lưu MediaAsset. Nếu transaction metadata lỗi: UoW khác xác minh không có metadata,
   DELETE cloud rồi mới trả quota. Delete hoặc SQL completion lỗi thì quota vẫn giữ, cần đối soát.

Tháng mới/reset process/đổi tài khoản VPS không reset dung lượng. Chỉ đối soát xác minh object đã mất
hoặc DELETE cloud thành công mới có thể giảm charged. Không tự xóa ảnh đang dùng để giải phóng quota.
Giới hạn 9 GB tính toàn bucket (kể cả object ngoài ứng dụng và orphan), cộng phần giữ chỗ chưa rõ kết quả.
Không bảo đảm hạn mức với người thao tác trực tiếp R2; ngăn lộ khóa và không chia bucket với ứng dụng khác.

## Đối soát, nhiều VPS và restore

- Mỗi node mới **chặn upload** cho đến khi chính node đó đối soát thành công; tránh dùng bộ đếm cũ sau restore.
- Worker chạy lúc startup và mỗi 6 giờ, kiểm tra lại mỗi 5 phút; thất bại chỉ log mã an toàn rồi thử lại.
  Hết hạn snapshot 24 giờ sẽ fail closed upload, vẫn cho xem ảnh/đặt hàng nếu dependencies còn hoạt động.
- Lease SQL 10 phút chặn upload/delete mới khi scan; không giữ transaction trong lúc LIST. Không scan khi
  có reservation active. Snapshot commit chỉ nhận đúng lease còn hạn, chặn worker cũ ghi đè worker mới.
- LIST toàn bộ bucket, có budget từng page, giới hạn 100000 objects; snapshot không đầy đủ/lỗi không cập nhật tổng.
- Snapshot ghi nhận object ngoài app. Reserved/Unknown/Deleting vắng mặt vẫn giữ quota; không đoán là upload chưa xảy ra.
- Scan thất bại sau khi lấy lease đánh dấu baseline chưa sẵn sàng trên SQL và node; chặn upload toàn DB
  đến khi có snapshot thành công, không tiếp tục dựa vào tổng cũ trong lúc lỗi.
- Scope hash cố định theo endpoint+bucket. Đổi access keys cùng bucket được; đổi bucket/account cần quy trình
  di chuyển riêng, không sửa hash/reset counter để bỏ qua. Dùng cùng DB khi các VPS phục vụ cùng bucket.
- Với nhiều node, worker node mới có thể đợi lượt scan/đợi reservation cũ hết active lease. Owner có thể
  gọi reconcile trên node đó; không mở upload bằng cách sửa DB. Tất cả node phải dùng code mới khi rollout.

## Cache và ngân sách lượt đọc

Catalog kiểm tra published/quyền **trước mỗi lần gọi cache**. Chỉ luồng public dùng byte cache RAM 64 MiB,
TTL 5 phút, một cache-miss fetch tại một thời điểm/process, không cache lỗi. Private/admin bỏ qua byte cache.
SDK GET chỉ được thực hiện sau khi counter Class B commit; hết budget trả 503, hit cache vẫn phục vụ được.
Key cache có endpoint+bucket+object key. API public `Cache-Control: public,max-age=60`, `Vary: Cookie`;
phiên đăng nhập `no-store`. Browser đã tải có thể giữ ảnh tối đa 60 giây sau unpublish; dữ liệu ảnh khách
đã tải không thể thu hồi. Muốn thu hồi tức thì phải chọn no-store cho public và chấp nhận ít lợi ích cache.

Chưa tạo Cloudflare CDN Cache Rules trong tài khoản. Nếu cấu hình CDN sau này, bypass mọi request có Cookie
và mọi API admin/auth/order; không bật Cache Everything cho toàn API. Test ảnh private/public/unpublish trước.
Cache và các budget giảm rủi ro, không bảo đảm tổng phí R2/server/CDN/Workers bằng 0.

## API vận hành Owner (đều dùng auth/CSRF khi ghi)

| API | Chức năng |
| --- | --- |
| GET `/api/admin/media/storage` | Charged/limit/uncertain bytes, last snapshot, counter tháng, warnings, NodeReady |
| PUT `/api/admin/media/uploads` body `{ "paused": true }` | Ngắt upload mới trên toàn DB; false để mở lại |
| POST `/api/admin/media/reconcile` | Đối soát bucket thật trên node nhận request |
| GET `/api/admin/media/orphans` | 100 object ledger đầu tiên chưa có MediaAsset, kể cả chưa đủ grace period |
| GET `/api/admin/media/assets?unusedOnly=true` | Danh sách metadata chưa được product/gym/content draft+published dùng, có phân trang |
| DELETE `/api/admin/media/assets/{id}` | Owner dọn ảnh không dùng sau grace period; kiểm tra lại tham chiếu dưới SQL lock |
| POST `/api/admin/media/orphans/{id}/delete` | Chỉ key R2 do app quản lý, không còn MediaAsset, quá 24h, không active |

Gỡ ảnh khỏi sản phẩm không đồng nghĩa xóa file ngay. Owner cần DELETE asset không dùng; metadata được xóa/audit
trong transaction trước HTTP. Response `{metadataRemoved,objectDeleted,cleanupRequired}` cho biết kết quả vật lý;
nếu cleanupRequired=true thì quota vẫn giữ và Owner xem orphan queue. Retry DELETE asset đã xóa metadata có thể trả 404;
không lấy 404 đó làm bằng chứng R2 đã xóa. Ngăn xóa metadata còn tham chiếu kể cả nội dung nháp.
Các object ngoài naming convention vẫn được tính dung lượng, nhưng không tự xóa bằng endpoint orphan.
Hiện dọn orphan là **Owner chủ động**, không có job tự xóa tất cả ảnh cũ. Cảnh báo xuất hiện trong status
và log tối đa mỗi chu kỳ worker; không tự gửi Telegram/emails trong phiên phát triển.

Mã thường gặp: `MEDIA_STORAGE_QUOTA` (409), `MEDIA_DAILY_LIMIT`/`MEDIA_BUSY`/`MEDIA_UPLOAD_BUSY` (429),
`MEDIA_OPERATION_BUDGET`/`MEDIA_RECONCILE_REQUIRED`/`MEDIA_STORAGE_UNAVAILABLE`/`MEDIA_UPLOADS_PAUSED` (503).
Không đưa credentials/SDK exception payload vào response/log. API product/order không bị pause bởi quota ảnh.

## Giám sát chi phí ngoài ứng dụng

Tạo Budget Alert trong Cloudflare Billing với ngưỡng nhỏ chấp nhận được; cảnh báo **không tự cap hóa đơn**.
Giữ bucket private, token chỉ Object Read & Write đúng bucket, tách dev/prod, bảo vệ tài khoản Cloudflare/Owner.
Nếu lộ khóa: pause app upload, thu hồi khóa ngay trên Cloudflare, tạo khóa mới và kiểm tra toàn bucket/usage.
Chỉ pause app không ngăn được người đã có khóa. Không coi payment card limit là cơ chế bảo đảm dịch vụ/chi phí.

Nguồn: [R2 pricing](https://developers.cloudflare.com/r2/pricing/),
[Budget alerts](https://developers.cloudflare.com/billing/manage/budget-alerts/),
[SkiaSharp MIT](https://github.com/mono/SkiaSharp).
