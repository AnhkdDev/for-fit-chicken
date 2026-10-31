# Hợp đồng nối frontend với backend

29/10/2026. DTO nguồn: Application/Commerce/Contracts.cs và docs/openapi/mvp.json.
Client `frontend/src/lib/api.ts` chỉ gọi `/api` cùng origin, credentials same-origin; không có API key trong bundle.

| Giao diện | API | Quy ước quan trọng |
|---|---|---|
| Website | products/content/gyms | Public only; không seed fallback sản phẩm/giá |
| Trang chi tiết | products/{id}, media/{id} | ID route, SKU active, lượng available MAIN |
| Giỏ | localStorage IDs/quantities | Không PII/auth; giá hiển thị tham khảo theo GET hiện tại |
| Popup mua nhanh Home | GET products/{id} | Refresh SKU/giá/available; thêm quantity có xét giỏ hiện tại; không POST đơn |
| Checkout | csrf rồi POST orders | Hai bước, COD, Idempotency-Key UUID; server tính tiền |
| Login | csrf → auth/login → csrf | CSRF đổi theo danh tính; cookie HttpOnly Secure |
| Session | auth/me, auth/logout | Không localStorage auth; logout thu hồi mọi phiên |
| Đơn | admin/orders/detail/events/actions/receipts | version hiện tại; xác nhận mới giữ, handover mới xuất |
| Sản phẩm | admin/products/variants/images | draft → SKU → upload/gắn ảnh → publish |
| Nội dung | admin/content/{key}, publish | Staff draft, Owner publish; assetIds, plain text |
| Kho | locations/batches/balances/documents | UnitOfWork phía backend; operationKey giữ khi retry cùng payload |
| Gym | gyms/stock/counts | Snapshot không realtime, version stock, nhập đủ mọi lô |
| Media | media upload, admin/media/status/assets | Upload Staff, status/delete Owner; không raw R2 |
| Báo cáo | reports/sales | Khoảng ngày VN đổi UTC [from,to); không phải lợi nhuận |
| Telegram | notifications/retry | Không token frontend; retry Failed có cảnh báo trùng |

Admin 29/10: `POST /admin/orders` nhận `{source,order}` và Idempotency-Key UUID, giữ key khi retry cùng payload.
Nếu request mất kết nối/5xx chưa rõ kết quả thì chặn đổi payload để gửi như đơn mới, yêu cầu kiểm tra danh sách trước.
`PUT /admin/orders/{id}` chỉ pending, gửi version và recipient/internalNote; không thay giá/items từ form này.
Product SKU PUT dùng version của SKU; ảnh attach/detach dùng version sản phẩm và GET lại parent sau ghi.
Publish sản phẩm PUT parent; CMS publish POST bản đã lưu với version, không publish nội dung unsaved.
Chọn ảnh cũ GET `/admin/media/assets` chỉ Owner; Operator vẫn upload `/media`, không mở rộng quyền server.

## Retry và concurrency
Không tự replay mutation. 409 yêu cầu người dùng đọc lỗi và tải lại bản mới; không sửa rowversion từ client.
Checkout giữ key theo payload trong memory của trang. Khi reload/mất tab, key chưa được lưu lâu dài để tránh lưu PII;
trường hợp đã gửi nhưng mất response và reload cần nhóm kiểm tra trùng trước khi đặt lại. Đây là giới hạn cần nâng cấp
bằng pending submission hash/key (không PII), không tự thêm một cơ chế tra cứu đơn công khai.
Phiếu kho giữ UUID+payload trong state cho retry cùng thao tác. Một thao tác mới cần key mới.

## Giới hạn kiểm chứng
UI fixture cổng 5174 chặn tất cả /api; không chứng minh cookie login/SQL/Telegram/R2 thật.
Backend có bằng chứng 83 tests/14 smoke từ checkpoint trước; frontend vẫn cần người dùng login Owner thật để
nghiệm thu thao tác tích hợp qua trình duyệt. Không yêu cầu người dùng gửi password/keys qua chat.
Không tự gọi mutation thật chỉ để chụp màn hình. Không xóa/cancel dữ liệu TEST TELEGRAM cũ khi làm frontend.

## HomeProducts checkpoint 29/10/2026
Admin trưng bày trang chủ + khối sản phẩm lớn/gallery/QuickBuy trước câu chuyện; lưu nháp/preview/công bố Owner. Đọc docs/home-display.md (từ docs: home-display.md). Frontend build +9 tests và16 SQL CommerceTests đạt trên DB disposable. Migration AddHomeProductsSection mới CHƯA áp dụng DB kinh doanh; cần Stop Debug, build, dotnet ef database update rồi chạy lại API. Không commit/push, không đổi quota/secrets.

### Home rút gọn + carousel + ảnh chính + xóa 29/10
Thân Home còn banner/sản phẩm/câu chuyện. HomeHero nhiều slide; Trưng bày chọn ảnh chính từng sản phẩm qua productMainImages. Owner xóa product/batch chưa dùng, chặn lịch sử/config references; không xóa tệp R2. Đọc home-display.md để biết contract và hướng dẫn. Build +10 frontend tests +18 SQL CommerceTests disposable đạt, browser fixtures đạt; DB kinh doanh không bị sửa/xóa. Cần rebuild/restart API đang chạy, migrationHomeProducts trước đó nếu chưa áp dụng; chưa commit/push.

## UI nhân viên 29/10
Owner có /quan-tri/nhan-vien, menu Nhân viên; Operator bị ẩn menu và chặn route. Danh sách mặc định active, search username/displayName, paging20; tạo Operator, sửa tên, khóa/mở khóa, reset password với xác nhận. Mật khẩu nhập lại, hiện/ẩn, không lưu localStorage hoặc tự điền mặc định; unmount đóng modal xóa khỏi memory. Conflict409 khóa submit, cho GET lại version và bỏ form cũ, không replay mutation. API employee mới phải rebuild/restart backend. Không thêm migration/UI quyền Owner khác không đổi. Build frontend kiểm chứng trong lượt này; chưa live UAT/tài khoản thật, chưa UI fixture cho employees. Chưa commit/push. Đọc employees-api.md cho contract.

## Operator scope — backend29/10
Chốt Operator được nhập/xuất và giao/kiểm đếm Regular gym. Backend orders/content chuyển Owner; gym resolutions Owner; Operator không đổi publish/featured/ẩn product đã publish. FallbackOwner; kiểm tra quyền Adjustment/correction trước replay. Đọc operator-permissions.md là ma trận hiện hành, thay quyền Staff cũ trong tài liệu lịch sử. FRONTEND CHƯA THU GỌN: cần menu/redirect/hide actions riêng ở lượt sau. Không migration/DB thật, chưa commit/push.

## Operator frontend đồng bộ29/10
Menu Operator chỉ san-pham/kho/phong-gym; index redirect sang sản phẩm. Routes đơn/nội dung/trưng bày/thư viện/nhân viên/báo cáo/thông báo bị chặn mà không mount component, tránh gọi API cấm. Product Operator ẩn featured/publish step, khóa active khi product đang publish; vẫn form ảnh upload phục vụ sản phẩm. Owner giữ nguyên chức năng.
Build và14/14 Node tests đạt (gồm2 permission tests cho menu/subroutes). Không thử account Operator thật hoặc browser fixture lượt này; backend34 tests trước đó là bằng chứng phân quyền server, không phải UI UAT. Không chạy listener/test DB, không commit/push. Cần backend mới và reload frontend để nghiệm thu role thật.

## Operator expanded operational access —30/10/2026
User requested Operator xem/sửa Tổng quan, Đơn hàng, Báo cáo, Thông báo. API orders/reports/notifications now AdminStaff. Orders includes existing manual create/edit/status/events/receipts/create receipt/void receipt; no business validation, rowversion, FEFO, UoW locks, CSRF or rate limits removed. Report includes existing aggregated cost fields; no report-edit endpoint exists. Notification only existing list/retry failed jobs, no bot-token/config management.
Frontend operatorAreas includes root/don-hang/bao-cao/thong-bao; dashboard no redirect, routes mount allowed modules. Receipt view/actions available to both staff roles. Content/HomeDisplay/Employees/media governance/product deletion/stock-cost/adjustment/baseline/resolutions stay Owner as before. Read operator-permissions.md updated matrix; older notes saying orders/reports/notifications Owner-only are superseded.
Validation:15/15 AdminAuthTests SQL-enabled pass on GUID disposable database (expanded list GET access, manual order request reaches validation, notification retry reaches business404, receipts GET allowed, existing Owner restrictions still403). Frontend14/14 Node tests pass including operator route allow/deny; frontend build pass. No live Operator/browser UAT, no real orders/Telegram/R2/SQL production mutations, no test listener started. No migration. Not committed or pushed; VPS still old images. Rebuild/redeploy BOTH images with new version tags to apply, do not restore/reset DB again.

## SSR public implementation checkpoint3 —30/10/2026
Implemented local sales-page SSR preservingReact/Vite/components/ID routes, no API/schema changes. Read docs/seo-ssr.md as current SEO/runtime reference (supersedes CSR-only architecture notes). Initial HTML includes title/description/canonical/robots/OG/JSON-LD and real public content, header statuses404/503, dynamic XML sitemap/textrobots. Public DTO whitelist, safe bootstrap/script escaping, no auth/cookie forwarding to data loader. Anonymous in-flight GET coalescing only (no retained stale snapshots), bounded4render/data10s. Admin lazyJS/CSS; cart restored after hydration, private routesCSR/noindex, ProductImage remains visible withoutJS using noscript override. Canonical origin fixed to owned domain; schema prices/SKUs/availability actualAPI. Font preconnect/stylesheet replaces CSS@import; main product photo fetchPriorityhigh. No invented business/nutrition/certifications/reviews.
Evidence: strict build/clientSSR passed;14existingfrontendtests+2SSR integration tests passed with mock public API on disposable local listeners; SSR tests also script-injection escaping/privatefield exclusion/hidden404/freshsitemap/upstream503/private route isolation. Linux images forfit-ssr/web:seo-ssr-v1 build passed; non-root Linux render HTML/JSONLD smoke, nginx-t and compose overlay configquiet passed. No productionAPI/SQL/R2/Telegram access, all --rm containers/mock listeners closed. No backend tests rerun because backend untouched.
NOT deployed or offline-packaged, not committed/pushed. Current VPS still CSR. Three-service update requires new updater/rollback preserving namespace ordering; old operator-v2 script insufficient. Browser hydration/cart/nav/visual mobile QA, Lighthouse/fieldCWV/RichResults/SearchConsole and liveUAT remain unverified; don't claim metrics/SEO rank. Draft SSR service256MiB/cpu.5 needs loadtest on2CPU sharedVPS; API sharedIP rate limits unchanged, catalogcap2000 fails503. Full public dataset preloaded for route compatibility; large-catalog optimization deferred. Knowledge/blog intentionally not created. Next: staging/browser/load validation then release-specific3service packaging when requested, no DBrestore/migrations or secret changes.

## Căn ảnh trang chủ —30/10/2026
Trưng bày gửi productImageFrames cùng payload HomeProducts/version, không endpoint upload/crop mới. Optional assetId/desktop/mobile config theo contract trong mvp-backend-api.md; save draft/publish Owner policy như cũ. SSR public-loader allowlist đã thêm field; editor/public dùng cùng CSS/FramedProductImage. Config cũ thiếu field vẫn contain. Rebuild cả backend/web/SSR trước release; không có migration.22 unit/2SSR/20SQL Commerce tests đạt, browser drag/touch UAT chưa kiểm chứng.

## Manual orders update30/10/2026
POST /admin/orders payload/key giữ nguyên, đơn mới trả statusConfirmed (không PendingConfirmation), đồng thời giữ FEFO và không enqueue Telegram. WebsitePOST/orders vẫn Pending/chưareserve/Telegram như cũ. Transactionfail rollback cảorder/items/reserved/idempotency. UI success dựa response status, sauđónglọc theo mã đơn mới và không giữ filter Chờ gọi. Retrykey cũ giữ snapshot cũ để không duplicate hoặc tự thay trạng thái. Không migration/quyền/COD/handover thay đổi; cần cả API vàfrontend mới.23SQLCommerce+22frontendtests/build đạt, không liveUAT.

## Thao tác gộp hoàn thành/thu tiền —30/10/2026
OrderDialog→complete-paid gọi1POST actions/complete-paid {version,reason}, không gọi tuần tựhandover/complete/receipt từbrowser. Hiện nút theo status/receiptsready/quyền, checkbox thực nhận bắtbuộc. Application refactor TransitionCore/ReceiveCore để reuse logic trong1transaction; wrappers riêngvẫngiữ. Stock/history/COD rollback đồng bộ, khóabusinessSQL vàversion bảo vệ concurrent; completed+paid retry không duplicate. Khôngmigration, restartAPI+frontend trước UAT.
