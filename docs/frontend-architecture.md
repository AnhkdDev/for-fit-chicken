# Kiến trúc giao diện ForFit

29/10/2026. Frontend local React/TS/Vite, không hosted Sites. Theo backend Clean Architecture hiện hành.

## Hướng thiết kế
Người dùng đã chốt **concept C** 29/10/2026, thay burgundy của bản đầu: navy #15294B,
cam đất #C86436, nền kem #FFF2DD, vàng nâu #C59F60 và sand #F3E2C7.
Navy cho brand/chữ/nút/footer storefront, cam đất cho em/điểm nhấn. Giữ Playfair Display cho kể chuyện,
Be Vietnam Pro cho nội dung/nghiệp vụ; không áp dụng gợi ý sans toàn trang của concept A.
Màu ngữ nghĩa thành công/lỗi giữ riêng. Concept C được chọn, không còn ở trạng thái chờ duyệt.
Trang chủ giữ trình tự hero/values/story/products/usage/gym/FAQ/footer của reference, code mới cho ForFit.
Không đưa gift box 3D, chứng nhận, đánh giá, dinh dưỡng hoặc thống kê chưa được xác thực vào giao diện.
Ảnh hero A Sín được copy tạm theo yêu cầu, có nhãn reference; ảnh thật thay qua HomeHero.

## Banner và CMS
- Bố cục, breakpoint, overlay, animation nằm trong code. Ảnh, title/body/link dùng HomeHero đã publish.
- Owner/Operator upload qua /api/media; ảnh tối ưu đi qua quota 9 GB và governed storage backend.
- Cả hai sửa draft; chỉ Owner công bố. Không đưa R2 key/presigned URL vào trình duyệt.
- Asset đầu là ảnh desktop/mobile chung. Chuẩn bị ảnh ngang 1920×900/1920×1080, sản phẩm bên phải,
  chừa trái cho text HTML. Không ghép sẵn chữ vào ảnh; cần xem trước cả desktop lẫn mobile.
- Backend v1 chỉ có assetIds, chưa có trường desktop/mobile riêng/focal point. Nếu muốn hai ảnh responsive,
  bổ sung schema có version sau; không tự ngầm gán asset thứ hai theo một quy ước không được tài liệu hóa.
- Quy tắc banner hiện hành: width/max-width100%/min-width0; desktop height
  clamp(430px,min(39vw,62svh),620px), không auto aspect-ratio tăng cao theo màn hình rộng.
  Ảnh cover/right75% nên không letterbox; crop phần nền trên/dưới theo kích thước để giữ vùng sản phẩm.
  Mục tiêu nhìn thấy dải thông tin và phần đầu câu chuyện ở màn hình desktop thông dụng. Ảnh thương hiệu
  cần vùng an toàn, không hứa mọi ảnh tùy ý luôn vừa mà không crop.
- <=1100px copy/ảnh tách hai vùng; ảnh theo chiều ngang và đúng tỉ lệ, không chữ đè lên ảnh. Desktop copy vẫn
  tối đa36%/430px, title52px, gradient nhẹ trái. Không còn crop64% mobile của bản đầu.
- Hướng dẫn dùng/contact/FAQ nhận plain text từ CMS. Không render raw HTML.

## Responsive và accessibility

Logo chính thức: `public/brand/4fit-original.png`, giữ bitmap nguyên bản. Component BrandLogo hiển thị
hai viewBox SVG vào biểu tượng và chữ từ cùng source, không tạo lại nét logo bằng AI.
BrandArtwork áp dụng SVG filter sRGB cho pixel cam sang #C86436 theo concept C, giữ navy và master PNG nguyên bản.
Header dùng bản ngang gọn; trên nền navy (footer/login/sidebar) có nền kem riêng để không chìm logo.
Nếu có SVG/vector gốc của nhóm thì thay raster 2.8 MB bằng asset vector được duyệt để giảm tải;
hiện không tự truy vẽ hoặc tái tạo làm thay đổi logo.
Thiết kế đồng thời desktop/tablet/mobile. Navigation public gấp menu, admin sidebar off-canvas.
Giỏ/checkout/detail xếp một cột; cards sản phẩm hai cột ở mobile, một ở <=360px; dashboard hai cột.
Bảng nghiệp vụ có vùng cuộn ngang riêng (giữ nghĩa so sánh dữ liệu, không làm toàn trang tràn ngang).
Dialog dùng native showModal focus trap, Escape/restore focus; skip link, visible focus, aria-label nút icon.
Reduced motion tắt chuyển động; form font input mobile cần 16px để tránh iOS zoom.

## Session và API
Cookie HttpOnly do browser giữ; CSRF chỉ memory. Sau login refresh token, logout clear session.
401 khi gọi nghiệp vụ chuyển về login; 403 hiển thị thiếu quyền. 409 không tự ghi đè version.
Không log body/PII/secrets. Không gọi cloud/Telegram từ frontend. Header CSRF mọi thao tác ghi.
Frontend chỉ ẩn nút Owner để hỗ trợ UX, server vẫn kiểm tra quyền.

## Phạm vi / duy trì
Khu câu chuyện Home dùng `components/StoryShowcase.tsx`, nối tiếp bằng dải sản phẩm ngang;
`components/QuickBuy.tsx` giữ dialog mua nhanh có GET public detail mới mỗi lần mở.
`lib/cart.ts` giữ quy tắc thêm quantity và kiểm tra giới hạn; `npm test` kiểm chứng helper này độc lập backend.
Xem `frontend-story-quickbuy.md` để tiếp tục CMS/rail/dialog, không sao chép logic popup vào mỗi card.

Không tạo đơn/khởi tạo Owner hoặc seed dữ liệu thật khi mở giao diện. Empty/error/loading rõ ràng.
Mọi thay đổi nghiệp vụ qua API, giữ version và idempotency cho các luồng có hỗ trợ.
Build tách biệt backend; production cần SPA fallback và /api proxy, theo runbook VPS.

## Admin ưu tiên nhập liệu (29/10, thay phong cách admin ban đầu)
- `src/admin.css` sau styles.css: nền trắng/xám, sans, accent xanh cho hành động, responsive riêng.
- `pages/admin.tsx`: shell/session/dashboard/list/order dialog/media/reports; không còn ProductEditor/Content cũ.
- `pages/admin-product.tsx`: `/quan-tri/san-pham/tao-moi` và `/:id`, 4 bước. Tạo nháp xong replace URL sang ID
  để refresh không trở lại form tạo mới. Thông tin chưa lưu giữ trong memory khi đổi bước; công bố cần đã lưu.
- `pages/admin-content.tsx`: chọn section, draft, hình ảnh, preview, công bố Owner có xác nhận.
- `pages/admin-order-forms.tsx`: đơn thủ công chọn SKU, sửa recipient pending. Không lưu recipient lên localStorage.
- `components/AdminAssetPicker.tsx`: upload Staff, thư viện chọn ảnh Owner (API assets Owner-only), gỡ liên kết
  không xóa tệp. Upload thành công/gắn thất bại giữ asset để thử gắn lại, không ép upload lại.
- `lib/useResource.ts`, `lib/admin-session.ts`, `components/admin-ui.tsx` dùng chung với operations, tránh import vòng.
- Dirty warning refresh/đóng tab/link thường áp dụng thông tin sản phẩm và CMS; không tự lưu nháp lên disk.
  Không hứa khôi phục qua trình duyệt Back/crash. SKU và form đơn cần bấm lưu/tạo theo thao tác riêng.
- Mobile danh sách đơn/sản phẩm chuyển thành từng thẻ; bảng kho/gym vẫn cuộn ngang vùng bảng riêng.

## Nền sản phẩm concept A —29/10
Áp dụng họa tiết tiêu/thảo mộc SVG native public/decor/forfit-botanical.svg, không dùng ảnh AI chứa text/product làm background. Pseudo-elements2 mép, pointer-events:none, không animation, opacity giảm trên mobile, không đè nội dung. CSS components/home-products.css; container sản phẩm tối đa1280px, ảnh max620, heading46, mô tả17px, giá29; mobile30/16/27 và khung ảnh contain. Preview dùng chung CSS.
Build đạt, browser UI fixtures đo320/390/768 không page overflow, desktop1920 được xem trực quan; ảnh botanical-products-desktop/mobile trong frontend-previews. Không đổi API/DB/R2/auth, không phát sinh test SQL hoặc Telegram thật; không thêm tests cho styling. Chưa commit/push. Fixture phải dừng sau kiểm tra, giữ nguyên server người dùng.

## SSR public implementation checkpoint3 —30/10/2026
Implemented local sales-page SSR preservingReact/Vite/components/ID routes, no API/schema changes. Read docs/seo-ssr.md as current SEO/runtime reference (supersedes CSR-only architecture notes). Initial HTML includes title/description/canonical/robots/OG/JSON-LD and real public content, header statuses404/503, dynamic XML sitemap/textrobots. Public DTO whitelist, safe bootstrap/script escaping, no auth/cookie forwarding to data loader. Anonymous in-flight GET coalescing only (no retained stale snapshots), bounded4render/data10s. Admin lazyJS/CSS; cart restored after hydration, private routesCSR/noindex, ProductImage remains visible withoutJS using noscript override. Canonical origin fixed to owned domain; schema prices/SKUs/availability actualAPI. Font preconnect/stylesheet replaces CSS@import; main product photo fetchPriorityhigh. No invented business/nutrition/certifications/reviews.
Evidence: strict build/clientSSR passed;14existingfrontendtests+2SSR integration tests passed with mock public API on disposable local listeners; SSR tests also script-injection escaping/privatefield exclusion/hidden404/freshsitemap/upstream503/private route isolation. Linux images forfit-ssr/web:seo-ssr-v1 build passed; non-root Linux render HTML/JSONLD smoke, nginx-t and compose overlay configquiet passed. No productionAPI/SQL/R2/Telegram access, all --rm containers/mock listeners closed. No backend tests rerun because backend untouched.
NOT deployed or offline-packaged, not committed/pushed. Current VPS still CSR. Three-service update requires new updater/rollback preserving namespace ordering; old operator-v2 script insufficient. Browser hydration/cart/nav/visual mobile QA, Lighthouse/fieldCWV/RichResults/SearchConsole and liveUAT remain unverified; don't claim metrics/SEO rank. Draft SSR service256MiB/cpu.5 needs loadtest on2CPU sharedVPS; API sharedIP rate limits unchanged, catalogcap2000 fails503. Full public dataset preloaded for route compatibility; large-catalog optimization deferred. Knowledge/blog intentionally not created. Next: staging/browser/load validation then release-specific3service packaging when requested, no DBrestore/migrations or secret changes.

## Home image framing —30/10/2026
FramedProductImage dùng ProductImage loading/error, image-framing.ts sanitize fit/x/y/zoom và CSS custom properties cho desktop/mobile; ImageFrameEditor dùng cùng renderer, pointer capture + slider bàn phím. Khung editor/public/Owner preview4:3. Config productImageFrames optional trong HomeProducts JSON, không canvas/export/tệp mới. Public apply chỉ khi assetId khớp main hiện tại; thiếu config dùng contain, thiếu mobile kế thừa desktop. Zoom modal/page detail giữ ảnh gốc. Tên2 dòng/mô tả3 dòng/tình trạng1 dòng tạo vị trí giá/nút thống nhất, bỏ bottom-push theo chiều cao gallery. API kiểm tra quan hệ/range trong Application; không Infrastructure/DB schema/media quota thay đổi. Chưa visual drag/touch UAT. Chi tiết test và release trong HANDOFF mới nhất.

## Logo chuẩn thay bộ lọc cũ —30/10/2026
Nguồn hiện hành public/brand/4fit-approved.jpg là JPEG chuẩn mới người dùng cung cấp, giữ byte-for-byte. BrandArtwork clipPath từng vùng thay việc dựa vào viewBox đơn thuần (letterbox từng làm lọt đầu nĩa thành4chấm). Bỏ alpha feColorMatrix vì fringe trắng ở navy footer, composite multiply trên nền sáng; dark footer có plate sáng giữ màu navy/cam master. Favicon SVG dùng chính symbol nguồn nhúng data URI, version link tránh cache. Các mô tả trước về alpha filter/orange-original/masterpng chỉ còn lịch sử. Logo là code/static asset, chưa có CMS chỉnh. Build đạt, chưa visual browser UAT.

## Dải thông tin compact —30/10/2026
HomeTransition1hàng flex,5mục mỗi mục2 dòng; màn hẹp scroll ngang native/focuskeyboard, không trở về grid nhiều hàng. Mobile hero chỉ đổi spacing/type/CTA layout và nhãn link câu chuyện ngắn; copy CMS giữ nguyên. Không JS carousel/animation mới. Preview mobile rail giữ cardwidth210px. Kiểm chứng build+mockSSR, chưa browser/device visual UAT.

## Mobile highlights3–2 —30/10/2026
Theo quyết định mới, HomeTransition thêm shortTitle cố định, @container highlights<=700px chuyển rail thành grid6track: item1–3 span2, item4ởcột2span2/item5ởcột4span2. Mỗi mục icon+label, không body; desktop không đổi. Chiều rộng container thay viewport giúp Owner preview mobile khớp. Không JS viewport branching nên SSR/hydration dùng cùngDOM. Build đạt, chưa visual device QA. Quy tắc này thay mobile scroll-rail trước.

## Home fluid gutters —30/10/2026
Hero > container.hero-content override width/max-width bằng clamp16/4%/80 gutter; HomeTransition section cùng gutter, inner grid5columns width100%. Không thay global container. Rail cũ bị bỏ, container<=1050 dùng shortlabels1hàng; desktopbodywrap tự nhiên. Thanh được inset và paddingdọc8px. Build đạt, visualQA chưa đo. Phương án này thay cap1280/fixedcardwidth trước đó.
