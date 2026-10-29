# Lưu ảnh bằng Cloudflare R2

**Mốc hiện hành 29/10/2026:** đã có quota tổng 9 GB, upload 5 MiB và WebP, cùng baseline bắt buộc mỗi node.
Xem [media-security](media-security.md) và [VPS runbook](vps-maintenance-runbook.md) trước deploy/chuyển VPS.
Các ghi chú kiểm tra 28/10 bên dưới là lịch sử; giữ thao tác tạo bucket/keys nhưng dùng quy tắc mới này.

Cập nhật 29/10/2026. Backend đã cấu hình và kiểm tra R2 thật; hướng dẫn setup dùng cho máy/user mới.

## Kiến trúc

- `IMediaStorage` nằm Application; adapter và AWS SDK S3 nằm Infrastructure.
- `Media:Provider=Local` mặc định giúp development/test không phụ thuộc tài khoản cloud.
- Chọn `R2` để upload mới vào private bucket. Không fallback sang ổ đĩa khi cloud lỗi.
- `ConfiguredMediaStorage` đọc/xóa theo StorageKey: `r2/<guid>.<ext>` dùng R2, key phẳng cũ dùng Local.
  Object key trên R2 cũng bao gồm `r2/`; đây là prefix, không phải bucket con.
- Database lưu metadata/key và 3 bảng vận hành quota mới (migration AddMediaGovernance); API ảnh vẫn `/api/media/{id}`.
- Upload qua backend, Owner/Operator + CSRF, PNG/JPEG/WebP đầu vào tối đa 5 MiB;
  decode thật, giới hạn pixel/frame và resize/WebP trước khi lưu. Legacy read vẫn nhận ảnh cũ tối đa 10 MiB.
- Public GET chỉ trả ảnh tham chiếu bởi sản phẩm/gym/content đã xuất bản. Nhân viên xem được ảnh nháp.
  Backend đọc R2, buffer legacy tối đa 10 MiB; public có byte cache và browser max-age60, authenticated no-store.
  Chưa cấu hình Cloudflare CDN Cache Rules hoặc presigned URL; xem media-security.md về quyền và thu hồi cache.
- HTTP R2 ngoài transaction SQL. Ghi metadata lỗi sẽ gọi Delete để bù; crash/network mơ hồ có thể để lại
  object mồ côi; có worker đối soát và Owner cleanup, không tự xóa mọi ảnh cũ. Key GUID tránh ghi đè ảnh cũ.
  Không xóa ảnh local khi chuyển provider.

## 1. Tạo bucket

1. Đăng nhập Cloudflare Dashboard, chọn tài khoản -> **R2 object storage**.
2. Kích hoạt R2 nếu tài khoản yêu cầu thiết lập billing; xem điều khoản/chi phí trên Cloudflare trước khi xác nhận.
3. Create bucket, ví dụ `forfit-media-dev`. Dùng bucket khác cho production sau này.
4. Giữ bucket **private**: không bật Public Development URL/r2.dev và không gắn public custom domain.
5. Lấy **S3 API endpoint** từ Dashboard, dạng `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`.
   Copy endpoint đúng jurisdiction nếu dùng EU/FedRAMP. Không dùng URL dashboard, public URL hay thêm tên bucket vào endpoint.

## 2. Tạo khóa truy cập

1. R2 Overview -> Account Details -> API Tokens -> Manage.
2. Create Account API token (nếu có quyền) hoặc Create User API token.
3. Chọn **Object Read & Write**, giới hạn **chỉ bucket vừa tạo**. Không cần Admin Read & Write/list toàn tài khoản.
4. Tạo token và lưu **Access Key ID**, **Secret Access Key** tại nơi riêng tư. Secret chỉ hiện lúc tạo.
5. Script cần hai khóa S3 này, không phải chuỗi Cloudflare API Token dùng cho REST management API.
   Không gửi khóa lên chat/Git/ảnh chụp. Thu hồi và cấp lại nếu đã lộ.

## 3. Lưu cấu hình một lần

Dừng API đang chạy (Ctrl+C), mở PowerShell:

```powershell
cd D:\TaiLieu\UcGaNauCham\backend\ForFitChicken
.\scripts\setup-r2.ps1
```

Script lần lượt hỏi endpoint, bucket, Access Key ID và Secret Access Key. Chạy lệnh trước rồi mới dán
giá trị tại lời nhắc; hai khóa được nhập ẩn. Script ghi qua stdin vào .NET User Secrets, không ghi
arguments/file tạm/repo; chỉ cập nhật các khóa Media, giữ nguyên Telegram và secrets khác.

| Khóa | Giá trị |
| --- | --- |
| `Media:Provider` | `R2` |
| `Media:R2:Endpoint` | S3 endpoint |
| `Media:R2:BucketName` | Bucket riêng cho ứng dụng |
| `Media:R2:AccessKeyId` | S3 Access Key ID |
| `Media:R2:SecretAccessKey` | S3 Secret Access Key |

Script bỏ override Media tại process PowerShell hiện tại. Nếu đã đặt biến ở Windows User/Machine,
chúng vẫn ưu tiên cao hơn User Secrets; cần tự bỏ/cập nhật chúng. Không dùng `dotnet user-secrets list`
trong đầu ra chia sẻ vì in cả khóa và Telegram token. User Secrets ngoài repo nhưng không mã hóa, chỉ dành cho development.

Khởi động lại:

```powershell
dotnet run --project ForFitChicken.API --launch-profile https
```

API validate provider/endpoint/bucket/khóa lúc startup, nhưng startup thành công chưa chứng minh kết nối/quyền cloud.
Production dùng secret manager hoặc environment: `Media__Provider`, `Media__R2__Endpoint`,
`Media__R2__BucketName`, `Media__R2__AccessKeyId`, `Media__R2__SecretAccessKey`; không deploy User Secrets.
Frontend không nhận khóa R2. Không cần R2 CORS vì browser chỉ gọi API cùng origin, backend gọi R2.

## 4. Kiểm tra thật sau khi cấu hình

1. Mở Swagger HTTPS, lấy CSRF, đăng nhập Owner; lấy CSRF mới sau login (xem admin-auth-api.md).
2. POST `/api/media` với header `X-CSRF-TOKEN` và file PNG/JPEG/WebP thật nhỏ.
3. Nhận Asset ID/URL. Trong R2 Dashboard phải có object `r2/<guid>.<ext>`.
4. GET URL ảnh bằng phiên admin phải trả đúng ảnh; khi chưa gắn vào nội dung public, khách không đăng nhập phải nhận 404.
5. Gắn Asset ID vào sản phẩm rồi xuất bản theo nghiệp vụ để kiểm tra khách xem được ảnh.
6. Lỗi `MEDIA_STORAGE_UNAVAILABLE` (503): kiểm tra endpoint/khóa/bucket/quyền/network tại máy, không chia sẻ khóa.
   `MEDIA_MISSING` (404): object/metadata thiếu; không tự tạo lại key hoặc đổi bucket để che lỗi.

Không xóa object kiểm thử nếu metadata đang tham chiếu nó. Script setup chỉ lưu cấu hình, không tạo object/không gửi Telegram.
Các automated tests dùng S3 giả, không chứng minh tài khoản Cloudflare đã hoạt động.

## 5. Ảnh cũ và đổi môi trường

Ảnh local cũ tiếp tục đọc ở `Media:RootPath`/`App_Data/media`; phải giữ thư mục đó khi deploy hoặc di chuyển riêng.
Đổi sang R2 không tự upload ảnh cũ. Chưa có migration file hàng loạt; không sửa StorageKey trong SQL bằng tay.
Chuyển tạm upload về Local:

```powershell
dotnet user-secrets set "Media:Provider" "Local" --project ForFitChicken.API
```

Giữ khóa R2 để còn đọc được ảnh key `r2/`. Đổi endpoint/bucket sang nơi khác cần di chuyển đầy đủ objects trước;
metadata hiện chưa lưu bucket trên từng ảnh. Backup DB và bucket/local files phải được thiết kế cùng nhau.

Nguồn chính thức:
- [Cloudflare R2 .NET SDK](https://developers.cloudflare.com/r2/examples/aws/aws-sdk-net/)
- [Tạo khóa S3 cho R2](https://developers.cloudflare.com/r2/api/tokens/)
- [Bucket public/private](https://developers.cloudflare.com/r2/buckets/public-buckets/)

## Ket qua kiem tra that 28/10/2026
Nguoi dung da chay setup; Codex kiem tra production R2MediaStorage voi bucket that thanh cong:
upload PNG 1 pixel -> download dung bytes -> delete -> GET nhan MEDIA_MISSING.
Object thu da xoa; khong ghi MediaAsset SQL hay gui Telegram. Day la kiem tra adapter cloud that,
chua phai nghiem thu upload qua Swagger/auth/CSRF tren anh san pham. Cac ghi chu chua cau hinh phia tren la lich su.
