# Deploy, backup và chuyển VPS

Cập nhật 29/10/2026. Đây là hướng dẫn deploy; **chưa có VPS production nào được cấu hình trong phiên này**.
Windows development vẫn dùng KDADEPTRAI\KDADEPTRAI, Windows Authentication; không đổi sang LocalDB.

## Các dữ liệu phải giữ cùng nhau

1. Source đúng commit **và mọi thay đổi chưa commit**. Clone Git không lấy được working tree chưa commit.
2. SQL backup: Identity, nghiệp vụ, MediaAssets, **cả ba bảng MediaStorageStates/MediaObjectReservations/MediaUsageCounters**,
   migrations history. Không export mỗi bảng Products/Orders rồi reset quota.
3. R2: giữ nguyên account/endpoint/bucket và toàn object keys. Không cần tải ảnh về/upload lại khi chỉ đổi VPS.
4. Ảnh local cũ: copy Media:RootPath/App_Data/media nếu DB vẫn có key local; R2 không chứa các file này.
5. Secret cấu hình ngoài repo: SQL, R2, Telegram; không nằm trong commit hay bản publish.
6. Data Protection key ring và ApplicationName=ForFitChicken nếu muốn giữ phiên đăng nhập/CSRF.
   Nếu không mang được key ring/khác cơ chế mã hóa OS, cho đăng nhập lại; không ảnh hưởng ảnh/đơn/quota.

Tài khoản Codex không phải tài khoản runtime. Đổi Codex trên cùng workspace thì đọc AGENTS.md/HANDOFF;
đổi máy phải sao lưu source chưa commit hoặc nhờ người dùng commit/push trước. Đổi Windows/Linux user/VPS
thì User Secrets không tự theo; **không chép token vào source** để khắc phục.

## Build và migration

Chạy build/test tại máy build, kiểm tra dependencies mới nhất qua `dotnet list package --vulnerable --include-transitive`.
Code hiện net8.0; lên kế hoạch cập nhật .NET/EF/runtime trước production và không bỏ qua audit bảo mật.

```powershell
cd D:\TaiLieu\UcGaNauCham\backend\ForFitChicken
dotnet restore
$env:FORFITCHICKEN_RUN_SQL_TESTS = '1'
dotnet test ForFitChicken.UnitTest
dotnet publish ForFitChicken.API -c Release -r linux-x64 --self-contained false -o artifacts/publish-linux-x64
```

Chọn đúng RID (linux-x64/linux-arm64/win-x64); SkiaSharp cần native assets đúng kiến trúc. Đã thêm Linux.NoDependencies
nhưng chưa chạy trên VPS Linux thật; dùng distro glibc (Ubuntu/Debian) và test decode/WebP trên đích, không mặc định Alpine/musl.

Migrate trước khi khởi động code mới. Máy có SDK có thể dùng EF CLI với connection qua secret/environment.
Runtime VPS không cần SDK nếu CI tạo **EF migration bundle** đúng RID và dùng môi trường bí mật của service.
Không truyền password SQL qua `--connection` trong command line/log. Chạy bundle từ thư mục publish
để đọc appsettings; không chạy nhầm Development trên production. Backup trước; không sửa/xóa migration đã áp dụng.
Migration mới `20261029012656_AddMediaGovernance` chỉ thêm 3 bảng, tổng 32 bảng (không kể EF history).

## Cấu hình VPS

Xem các template `backend/ForFitChicken/deploy/`. Thay placeholder **trên VPS**, không commit file chứa khóa.
User Secrets chỉ dùng development. Production dùng secret manager hoặc EnvironmentFile có quyền đọc hạn chế.
Không chạy backend bằng root; service account chỉ được đọc config/ghi key ring và local media cần thiết.

Các khóa bắt buộc:
- `ASPNETCORE_ENVIRONMENT=Production`, bind HTTP loopback sau reverse proxy HTTPS.
- `ConnectionStrings__DefaultConnection`: database đã restore/migrate, encrypted connection.
  VPS Linux không dùng Windows Integrated Security giống máy development; cần cấu hình authentication SQL phù hợp
  với hạ tầng (ví dụ tài khoản SQL riêng có quyền tối thiểu). Không thay connection development trong repo.
- `Media__Provider=R2`, endpoint/bucket và AccessKeyId/SecretAccessKey đúng scope cũ.
- `Telegram__Enabled=true`, BotToken/TargetChatId khi dùng production group. Staging nên tắt hoặc dùng bot/nhóm riêng.
- `DataProtection__KeysPath` đường dẫn bền riêng ngoài release; backup và bảo vệ key ring như bí mật.
- `Media__RootPath` đường dẫn bền cho ảnh local cũ.
- `ReverseProxy__KnownProxies__0=127.0.0.1` khi Nginx cùng VPS. Chỉ trust IP proxy thật, không trust mọi địa chỉ.

Tạo service user và các thư mục trước khi start (thực hiện trên VPS Linux, chưa chạy ở máy Windows này):

```bash
sudo useradd --system --home /var/lib/forfit --shell /usr/sbin/nologin forfit
sudo install -d -o forfit -g forfit -m 700 /var/lib/forfit /var/lib/forfit/keys /var/lib/forfit/media-local
sudo install -d -o root -g forfit -m 750 /etc/forfit
```

Nếu user/thư mục đã có thì kiểm tra trước, không xóa/tạo lại để ghi đè dữ liệu. Secret file backend.env
đặt root:forfit, mode 640 hoặc chặt hơn. Migration dùng **migration.env riêng** với SQL principal đủ quyền schema,
không cấp db_owner cho runtime chỉ để migrate. Không `source` EnvironmentFile như shell script vì chuỗi SQL chứa
dấu chấm phẩy và secret có thể bị shell diễn giải; để systemd nạp file. Kiểm tra đường dẫn dotnet/user/quyền,
certificate và placeholders của templates, chạy `nginx -t` và test service trên môi trường staging trước.

Forwarded headers chỉ được xử lý khi có danh sách proxy rõ ràng, ForwardLimit=1, trước HTTPS redirection/rate limiter.
Nginx phải **ghi đè** X-Forwarded-For với IP đã xác minh; không chuyển tiếp header do client tự đặt.
Nếu có Cloudflare phía trước, cấu hình Nginx real_ip cho **đúng CIDR Cloudflare hiện hành**, chỉ tin CF-Connecting-IP
từ IP Cloudflare, và firewall origin chỉ cho Cloudflare/reverse proxy vào. Chưa áp các rule này trên tài khoản/VPS thật.
Không tắt Secure cookie để chữa redirect loop; sửa forwarded proto/trust proxy.

Thiết lập HTTPS/certificate, firewall, thời gian NTP, log rotation và giới hạn CPU/RAM của service/container.
Ví dụ systemd MemoryMax=1G, CPUQuota=200%; đo thực tế, OOM restart vẫn để reservation charged để đối soát.
Reverse proxy request body limit khoảng 6 MiB; ASP.NET kiểm tra file thực 5 MiB.
Swagger chỉ có trong Development; không bật Development trên production chỉ để có Swagger.

## Trình tự chuyển VPS an toàn

1. Kiểm tra version/source/config; chụp trạng thái Owner media storage và migrations.
2. Pause upload trên VPS cũ bằng Owner API. Lập maintenance window, dừng nhận ghi đơn/kho khi lấy backup cuối.
3. Dừng mọi API/worker cũ, đặc biệt code chưa hỗ trợ quota. Không để hai database độc lập cùng ghi một bucket.
4. Backup SQL đúng quy trình (không copy file MDF đang chạy). Kiểm tra checksum/restore thử vào DB riêng;
   copy local media/key ring nếu có, không copy log nhạy cảm sang repo.
5. Restore lên SQL đích hoặc giữ SQL hiện tại; migrate; cấp secret/config đúng bucket, cùng application name.
6. Khởi động node mới. Mỗi node R2 tự đối soát lúc startup; upload bị chặn đến khi thành công.
   Xem `NodeReady=true`, LastReconciled mới, ChargedBytes hợp lý, Unknown/warnings đã được xem xét.
7. Kiểm tra health, đăng nhập, ảnh cũ public/private, thử ảnh mới; kiểm tra quyền Operator/CSRF/quota.
   Test Telegram có thông báo rõ ràng; không tạo đơn hoặc gửi nhóm thật ngoài kế hoạch nghiệm thu.
8. Đổi DNS/routing sau khi đạt, mở upload bằng Owner. Nếu dùng cùng SQL và rolling deployment, vẫn phải
   bảo đảm tất cả node ghi ảnh đều chạy code quota mới; chờ node mới reconcile trước khi nhận upload.
9. Tắt dịch vụ/quyền secret trên VPS cũ. Có thể cấp khóa R2 mới cùng bucket rồi thu hồi khóa cũ sau xác minh.
   Nếu token có IP restriction, cập nhật IP mới. Không xóa bucket hay object khi hủy VPS.

Khóa R2 đổi không ảnh hưởng key ảnh; đổi account/bucket thì khác: cần copy giữ nguyên object keys,
đối chiếu manifest/bytes, kế hoạch cập nhật scope có kiểm soát và backup. Không tự sửa ScopeHash thành rỗng.
Chưa có công cụ migration giữa bucket; không giả định đổi endpoint là ảnh tự đi theo.

## Backup và xử lý sự cố

- SQL backup tự động theo RPO của nhóm, lưu ngoài VPS chính, mã hóa và có restore drill.
- R2 độc lập VPS nhưng không phải backup chống xóa nhầm/khóa lộ. Sao lưu object/manifest sang nơi độc lập
  theo lịch phù hợp; tính cả chi phí lưu trữ/request của bản sao. Không bật lifecycle xóa mọi ảnh sau N ngày.
- Key ring/local media cần backup riêng. Không reset MediaUsageCounters/MediaObjectReservations để "chữa" quota.
- Quota đầy: pause, đối soát, xem orphan; không xóa ảnh đang có MediaAsset. Unknown giữ byte cho đến xác minh/xóa.
- DB/R2/network lỗi: giữ dữ liệu và counter; sửa kết nối, chạy reconcile. Không dùng DB trống hoặc đổi bucket để né lỗi.
- Rollback app: giữ schema additive và bảng quota; không rollback sang code upload cũ không có quota khi vẫn mở upload.
- Log không chứa token/secret/recipient; cảnh báo status/worker phải được chuyển vào hệ thống giám sát khi deploy.
  Budget Alert trên Cloudflare là email cảnh báo, không tự cắt phí. App không bảo đảm invoice bằng 0.

## Bàn giao cho phiên Codex mới

Đọc theo thứ tự: AGENTS.md -> HANDOFF -> mvp-backend-progress -> media-security-progress -> media-security
-> API contract -> runbook này. Xem git status trước sửa; giữ mọi thay đổi chưa commit. Không cần và không in secret.
Ghi rõ test fake hay cloud thật, database nào đã migrate, process nào đang chạy và node nào đã reconcile.
Không tự tạo Owner mới/reset password/drop development DB. Khi cần credential, cho người dùng nhập tại máy.

## Gói cập nhật3container —31/10/2026
Gói full-v3 offline đã chuẩn bị: đọc release-full-v3.md để biết checksum/lệnhupdate/rollback. Cập nhật chỉforfit-phase1, giữ /etc/forfit JSON, novastay-net vàkeys/localmedia volumes. API→SSR→web dùngsharednetworknamespace; phảigỡweb/SSR trướcAPIrecreate. ScriptbackupimageIDs,healthHTML+sitemap,rollback2hoặc3dịchvụ; noDBrestore/migration/prune/hostNginxreload. Preflight1.5GiBdisk/650MiBRAM. UserchưachạyupdateVPS, khôngcoi simulation làproductionrollbackđãkiểmchứng.
