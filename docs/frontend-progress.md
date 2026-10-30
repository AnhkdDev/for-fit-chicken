# Tiến độ frontend

## Áp dụng concept C + banner gọn — 29/10/2026 (ĐÃ CHỌN)
- Người dùng chốt C dù trước đó cân nhắc A: navy #15294B, cam đất #C86436, kem #FFF2DD, vàng nâu #C59F60.
- Brand/heading/button/footer/sidebar/theme-color đồng bộ navy, em/accent cam đất; giữ serif kể chuyện,
  Be Vietnam Pro cho nội dung/operations. Không chuyển sang sans toàn trang theo gợi ý A trước đó.
- BrandArtwork dùng SVG filter chỉ vùng cam thành cam đất khi hiển thị, giữ navy/nét/logo gốc không ghi đè;
  BrandLogo và logo trong StoryShowcase dùng cùng artwork. PNG gốc vẫn nguyên bản để rollback/đổi asset vector sau.
- Hero bỏ auto aspect-ratio tăng vô hạn. Desktop height clamp(430px,min(39vw,62svh),620px), ảnh cover/right75%
  để chừa phần dưới và không letterbox. <=1100px vẫn tách text/image, tự chiều cao; giữ width100% tránh overflow.
- Build strict TypeScript/Vite đạt. Browser fixture kiểm tra320/390/768/1101/1366/1920/2560 và desktop cao600:
  không overflow, copyFits=true, không chồng hero-foot. Màn1920×980 hero≈608px, storyTop≈816 (<980), đã thấy nội dung dưới.
- Xem desktop/mobile/admin, console không error. Ảnh concept-c-live-desktop/mobile/admin.jpg trong frontend-previews.
  Không chạy lại cart/backend tests vì không đổi nghiệp vụ. Chỉ fixture, không ghi DB/R2/Telegram/secrets.
  Đã dừng fixture sau kiểm tra; không dừng server người dùng. Chưa commit/push.

## Lịch sử tạo concept — 29/10/2026 (trước khi chốt C)
- Người dùng yêu cầu phân tích logo và màu web + vài ảnh mẫu. Đã tạo 3 concept bằng imagegen built-in:
  A navy/cam gốc, B burgundy/vàng nâu (đề xuất đổi logo), C navy/cam đất/nền kem.
- Lưu PNG + palette/giải thích/prompts trong docs/brand-concepts/. Đây là ảnh AI minh họa desktop/mobile,
  món ăn/bao bì/copy/logo trong ảnh không phải tài sản sản phẩm thật đã duyệt.
- Chưa thay CSS hay logo gốc. C là gợi ý, KHÔNG phải quyết định đã chốt; cần người dùng chọn mới triển khai.
- Không sửa runtime/backend/credentials; không chạy build/tests vì chỉ tạo ảnh/docs. Chưa commit/push.

## Banner phủ khung rộng + hướng dẫn admin — 29/10/2026
- Ảnh người dùng cho thấy dải trống trái ở màn hình rộng: object-fit contain trong hero cao tối đa 660px
  khiến ảnh không đủ ngang. Sửa desktop hero theo aspect-ratio 1942/809 (không giới hạn cao 660),
  object-fit cover/right bottom để không letterbox; min-height520 chỉ bảo đảm chỗ cho copy ở desktop nhỏ.
- <=1100px tách copy/ảnh thành hai vùng, giữ ảnh tự co theo chiều ngang. Mobile tiếp tục không đè chữ lên ảnh.
- README bổ sung đăng nhập thật https://localhost:5173/quan-tri, API HTTPS7010, Owner người dùng đã tạo,
  phân biệt fixture5174, HTTPS/certificate và lỗi lockout. Không tạo/reset tài khoản hoặc in credentials.
- Build TypeScript/Vite đạt. Browser đo9 viewport320/390/768/1024/1100/sát1101/1366/1920/2560:
  gapX=gapY=0, không tràn ngang, copyFits=true. Phát hiện min-height+aspect-ratio làm khung rộng1248px
  ở sát breakpoint1101, đã khóa width/max-width100%/min-width0 rồi kiểm tra lại đạt. Console không có error.
- Lưu banner-fluid-1920.jpg và banner-fluid-mobile.jpg. Chỉ UI fixtures, không thay đổi auth/backend/DB/secrets.
  Không login hoặc reset Owner thật. Đã dừng fixture sau kiểm tra, giữ nguyên server người dùng nếu đang chạy. Chưa commit/push.

## Câu chuyện + trưng bày sản phẩm + popup — 29/10/2026
- Người dùng muốn khu câu chuyện có sản phẩm trưng bày và bấm sản phẩm mở popup mua nhanh.
- Home dùng StoryShowcase: nội dung Story CMS, ảnh chính/2 ảnh phụ (assetIds), logo/thẻ minh họa khi chưa có ảnh;
  CTA khám phá câu chuyện. Dải sản phẩm ngang ngay bên dưới, nút trước/sau, vuốt mobile, popup từ mỗi sản phẩm.
- QuickBuy GET lại public detail, chọn SKU/số lượng/ảnh, giá theo SKU, hết hàng không cho thêm; thêm xong có xác nhận
  inline và đường đi giỏ. Đóng Escape/backdrop/khôi phục focus qua native Modal dùng chung.
- Cart thêm quantity nguyên tử qua cartRef; xét lượng đã có, max100/SKU và 20 SKU, không toast thành công khi bị từ chối.
  Detail GET đồng bộ catalog để giỏ nhận giá/SKU mới. Không giữ hàng, không POST đơn từ popup.
- Build TypeScript/Vite đạt; 5/5 node tests cart đạt (merge/stock/cap/20 SKU/input invalid). Browser desktop/mobile đã đạt: chọn SKU đổi giá, hết hàng disabled, lượng giỏ cũ được trừ khỏi số có thể thêm; Escape đóng và trả focus card.
- Fixtures riêng có 6 product UI, SKU thứ hai và SKU hết hàng để kiểm tra chọn/giới hạn. Không dữ liệu bán thật.
- Browser fixture: 3 gói SKU 48000 + 2 gói SKU 37000 → giỏ 5 gói/218000 VND. Không POST order.
- Rail scrollLeft từ 0→186 sau nút tiếp; viewport320/390/768 không tràn ngang, popup390 vừa màn hình.
  Console QA không có error. Ảnh quick-buy-desktop/mobile và story-products-desktop ở frontend-previews.
- Chi tiết: frontend-story-quickbuy.md. Không sửa backend/DB/R2/Telegram/secrets, chưa commit/push.

## Banner và logo thật — 29/10/2026
- Người dùng báo chữ/lớp phủ che nửa banner và cung cấp logo chính thức 4fit.
- Thu copy desktop về 36%/430px, title tối đa 56px; giảm gradient chỉ về phía trái, image contain
  không cắt ảnh. Mobile copy phía trên, ảnh nguyên khung phía dưới, không gradient/text đè lên ảnh.
- Logo copy nguyên bản vào public/brand/4fit-original.png (không sửa bitmap, không tạo lại bằng AI).
  BrandLogo dùng hai SVG viewBox hiển thị symbol và wordmark từ cùng ảnh để bố trí ngang dễ đọc ở header.
  Footer/login/sidebar dùng logo trên miếng nền kem để giữ nguyên màu navy/cam; không đổi palette website.
- Kiểm chứng: build TypeScript/Vite đạt; browser desktop và viewport 320/390/768/1440 không tràn ngang.
  Mobile đo imageTop >= copyBottom (không chồng ảnh/chữ); image object-fit contain. Logo xem đúng ở header/footer/sidebar/login.
  Đã bổ sung sidebar cuộn được trên màn hình thấp; logout giả lập qua bàn phím đạt; console không có error.
- Hash xác nhận logo giữ nguyên byte. Ảnh bàn giao home-logo-banner-desktop.jpg và home-logo-banner-mobile.jpg
  trong frontend-previews (watermark UI fixture). Không ghi DB/R2/Telegram, không thay cấu hình/secrets. Chưa commit/push.

## Điều chỉnh palette 29/10/2026 — theo A Sín
- Người dùng yêu cầu thay tone xanh rừng/sage bằng tone trang A Sín. Đọc CSS reference local:
  primary-red #681B22, hover #501318, warm-gold #C59F60.
- Đổi palette storefront/admin/login: nền cream/be ấm, burgundy cho brand/CTA/heading/footer/sidebar;
  nền dashboard và ảnh placeholder đổi từ xanh nhạt sang beige. Giữ màu ngữ nghĩa success/error.
- Theme-color và favicon đồng bộ. Không sửa API/dữ liệu/cấu hình media, không đụng secrets.
- Build strict TypeScript/Vite đạt; xem lại home desktop/mobile 390px và admin desktop bằng fixture tách biệt. Màu CTA/heading đo thực tế rgb(104,27,34) = #681B22; mobile scrollWidth bằng clientWidth; console QA không có error. Không chạy mutation thật hoặc backend/cloud.
- Ảnh mới: home-burgundy-desktop.jpg, home-burgundy-mobile.jpg, admin-burgundy-desktop.jpg trong frontend-previews; ảnh có watermark fixture. Chưa commit/push.

## 29/10/2026 — Bắt đầu
- Người dùng yêu cầu frontend thực trong repository, không Antigravity/Sites hosting; React TypeScript/Vite.
- Đã đọc HANDOFF, progress/backend API, auth, media security, VPS runbook và HomePage reference.
- Trang chủ tham khảo thứ tự section của CodeReverseASinTayBac, viết code mới; chỉ dùng tạm hero-watercolor.webp.
- Banner: dùng HomeHero CMS hiện có (title/body/assetIds/link), không hardcode ảnh thương hiệu lâu dài.
- Responsive điện thoại triển khai cùng lúc; kiểm tra 390px/desktop; reduced motion, keyboard, feedback lỗi/loading.
- Đang xây storefront + admin. Chưa nghiệm thu UI/API, chưa thay đổi dữ liệu/ảnh R2, không commit/push.

## Checkpoint giao diện và build
- Đã có public routes: home/catalog/detail/story/gyms/cart/COD hai bước, success receipt công khai an toàn.
- Admin có login/session/logout, dashboard/orders/detail/action/receipt, product draft+SKU+upload,
  kho/lô/phiếu một dòng, gym/snapshot/count, content draft/publish, media/upload/quota/assets/unused cleanup,
  reports/notifications/retry. Owner-only UI đi theo server roles; Operator tiếp tục upload.
- Strict TypeScript + production build đã đạt. npm install audit ban đầu: 0 vulnerabilities.
- API thật đã chạy với Telegram disabled riêng process; storefront GET thành công, hiện chưa có sản phẩm publish.
  Không seed sản phẩm hay tạo đơn thật để kiểm tra UI. Native dev certificate export vào .certs Git ignored.
- Đã xem desktop và mobile 390×844 bằng browser; home không tràn ngang, menu mở/điều hướng đúng, login mobile hiển thị.
- Đang kiểm tra UI sâu bằng harness `npm run dev:fixtures` cổng 5174, có watermark rõ ràng.
  Middleware intercept toàn bộ /api, KHÔNG forward SQL/R2/Telegram. Fixture không phải nghiệm thu tích hợp thật.
- Lần chạy sandbox đầu không đọc được Windows certificate/keyring, đã chạy lại process thử có quyền phù hợp;
  không đổi cấu hình bảo mật/backend. Dev frontend cần phục vụ ngoài giới hạn socket để browser truy cập localhost.

## Chưa bao phủ trong giao diện đợt đầu
- Sửa SKU/price, gỡ/gắn lại ảnh có sẵn; tạo đơn thủ công và sửa recipient/internal note; void COD.
- Kho: nhiều dòng/chứng từ sửa sai/Adjustment/Reclassify, khóa lô và giá vốn Owner, bộ chọn >100 lô/SKU.
- Gym: xác minh/void chênh lệch, xem toàn bộ lịch sử count có phân trang (hiện 20 gần nhất).
- Media: hàng orphan cleanup riêng. Backend đã có các API này, không dùng nút giả để báo thành công.
- SiteSettings chưa có cấu hình thương hiệu có schema riêng; Usage đã nối title/body CMS; các bước dùng chung giữ trong code.
- Cần tài liệu/assets thương hiệu thật, rà copy/ảnh, test Owner thật do người dùng nhập credentials riêng; chưa deploy VPS.

## Checkpoint kiểm chứng cuối 29/10/2026
- `npm run build`: strict TypeScript + Vite đạt, output JS 357.75 kB / gzip 108.98 kB, CSS 41.76 kB / gzip 9.76 kB.
- `npm audit` cả dependencies/devDependencies: 0 vulnerabilities theo registry tại thời điểm kiểm tra.
- Browser thật/API thật: trang chủ, catalog empty, menu mobile, login; GET products/csrf 200, auth/me 401 khi chưa login.
- Browser UI fixtures: thêm giỏ, tăng số lượng 1→2, review COD, submit trả UI-ONLY-CHECKOUT 72000 VND, giỏ về 0;
  không tạo đơn SQL/Telegram. Dashboard, orders/detail modal, content draft save, gym snapshot modal,
  media quota/library và reports render đúng. Console error cuối trên tab QA mới: không có.
- Home đo scrollWidth==clientWidth ở viewport 320/390/768/1024; order modal 390 không làm page tràn ngang.
  Chỉ emulation viewport trên Chromium/IAB, chưa test máy iPhone/Android thực hoặc Safari.
- Sửa UX review COD/success cuộn về đầu; mobile input 16px tránh iOS auto zoom; closed mobile sidebar không focus được.
- Sửa lỗi dev HMR createRoot trùng khi format đồng loạt bằng tách App.tsx component boundary khỏi main.tsx entry.
  Một lần Vite restart làm tab cũ rơi vào connection refused; đã mở tab QA mới sau server ổn định, không bỏ qua browser warning.
- API thật chưa login Owner (không biết và không hỏi password qua chat), chưa chạy mutation thật/upload mới.
  Backend không đổi code/schema. Không chạy lại 83 backend tests vì không có thay đổi backend; bằng chứng cũ giữ ở HANDOFF.
- `.certs/localhost.key` và `.pem` Git ignored; Vite chặn cả direct path và /@fs, kiểm tra đều 403.
  Scan source frontend/docs không có private key/Telegram token; production bundle không có fixture labels/data.
- Lưu ảnh home-desktop.jpg, home-mobile.jpg và admin-desktop.jpg (admin là fixture có watermark) trong docs/frontend-previews/.
- Code đã format bằng Prettier, lockfile có trong frontend; muốn máy khác tái lập dùng npm ci.
- Chưa commit/push. Đọc README frontend để chạy lại sau khi các server kiểm tra được dừng.


- Teardown: đã xác minh API đúng executable của repo rồi Ctrl+C process thử; frontend dev/fixture cũng đã dừng. Không giữ server ở 7010/5195/5173/5174 sau kiểm chứng. Đóng browser không thay cho dừng server.


- Đã dừng dev:fixtures sau kiểm chứng; không giữ API/test server, không commit/push thay đổi.





## Admin nhập liệu — checkpoint 29/10/2026 (đang kiểm chứng)
- Tách helper/session/hooks, sản phẩm chuyển trang riêng 4 bước; nội dung chọn ảnh trực quan và preview.
- Đơn có các tab công việc, form tạo thủ công chọn SKU theo tên, sửa người nhận khi pending.
- CSS admin riêng; storefront giữ nguyên. Đang chạy build và UI fixture, chưa khẳng định kiểm thử backend thật.
- Chi tiết quyết định: admin-workflows.md. Chưa commit/push.


## Admin vận hành — hoàn tất lượt UI 29/10/2026
- Build TypeScript/Vite cuối đạt; npm test 7/7 (5 cart + 2 Vietnamese slug). Không chạy lại backend tests vì không đổi backend.
- Browser fixture đã thao tác: tạo nháp → SKU → chọn ảnh thư viện → công bố; draft CMS → publish; đơn thủ công COD2×37000=74000; sửa địa chỉ pending tổng tiền giữ nguyên. Tạo nháp chuyển URL sang ID để refresh không tạo lại.
- Product/CMS đo viewport320/390/768/1280: không page overflow. Orders390 chuyển thẻ, nút chi tiết nằm trong chiều rộng màn hình. Console cuối không có error/warn.
- Ảnh admin-operations-product/orders/content/mobile-product/mobile-orders trong frontend-previews. Các ảnh concept-c-live-admin cũ chỉ là lịch sử.
- Không gọi backend thật/SQL/R2/Telegram; auth thật và role Operator qua browser chưa nghiệm thu lại. Đọc admin-workflows.md trước khi làm tiếp.
- Đã xác minh fixture process thuộc frontend/Vite5174 và dừng sau kiểm tra; không khởi chạy API, không dừng instance của người dùng. Chưa commit/push.


## HomeProducts checkpoint 29/10/2026
Admin trưng bày trang chủ + khối sản phẩm lớn/gallery/QuickBuy trước câu chuyện; lưu nháp/preview/công bố Owner. Đọc docs/home-display.md (từ docs: home-display.md). Frontend build +9 tests và16 SQL CommerceTests đạt trên DB disposable. Migration AddHomeProductsSection mới CHƯA áp dụng DB kinh doanh; cần Stop Debug, build, dotnet ef database update rồi chạy lại API. Không commit/push, không đổi quota/secrets.

### Home rút gọn + carousel + ảnh chính + xóa 29/10
Thân Home còn banner/sản phẩm/câu chuyện. HomeHero nhiều slide; Trưng bày chọn ảnh chính từng sản phẩm qua productMainImages. Owner xóa product/batch chưa dùng, chặn lịch sử/config references; không xóa tệp R2. Đọc home-display.md để biết contract và hướng dẫn. Build +10 frontend tests +18 SQL CommerceTests disposable đạt, browser fixtures đạt; DB kinh doanh không bị sửa/xóa. Cần rebuild/restart API đang chạy, migrationHomeProducts trước đó nếu chưa áp dụng; chưa commit/push.

## Login + chẩn đoán 409 — 29/10/2026
- Sửa lỗi client bỏ qua ProblemDetails.title (middleware business gửi title, không detail); helper lib/problem.ts hỗ trợ code/detail/validation/title và fallback, 12 frontend tests đạt, build đạt. Không đổi backend status/rules.
- Media delete error trước đây nằm sau native modal, nay hiển thị trong modal; ảnh dưới24h khóa nút, hiển thị mốc chờ tối thiểu và nút tải lại. Backend vẫn kiểm tra thời gian grace cấu hình, references và quota.
- DeleteRecord có link tới sản phẩm/trưng bày và tải lại khi VERSION_CONFLICT; không auto replay DELETE. 409 vẫn là phản hồi đúng khi có liên kết.
- SQL chỉ đọc số đếm trên DB đang dùng: product1 có1 batch+1 order; batch1 có1 balance+1 documentline; media1 khoảng18 giờ, chưa đủ24h. Không xóa/sửa dữ liệu thật và không gọi raw R2. Không lưu PII/secrets.
- Login form logo/căn giữa/sans/input16px/hiện mật khẩu/fieldset busy; giữ Secure cookie, CSRF và auth endpoint cũ. Không tạo/reset account.
- Browser fixture xác minh product409 hiện title+link; media409 hiện ngay trong modal; logout vào login, desktop và390px không overflow. Không đăng nhập Owner thật, không xóa sản phẩm/lô/ảnh thật. Không chạy lại backend tests vì chỉ đổi frontend.
- Preview admin-login-revised.jpg. Chưa commit/push; dừng fixture sau test, giữ server người dùng. Dòng React DevTools là thông báo phát triển, không là lỗi.

## Tìm kiếm/lọc admin 29/10
Catalog UI mặc định status=active, lựa chọn inactive hoặc tất cả. Search tên/slug/SKU. Kho mặc định tab lô với status=active nghĩa chưa khóa bán và chưa hết hạn, không hứa còn số lượng tồn. Có blocked/expired/tất cả; search mã lô/SKU/tên sản phẩm. Submit search và đổi status reset page1.
GET admin/products và admin/inventory/batches thêm search/status, lọc SQL trước count/pagination. Không truyền status giữ tương thích các picker/admin khác cần toàn bộ dữ liệu; public API không đổi. Không thêm migration hoặc sửa/xóa DB thật.
Build frontend đạt;18 CommerceTests cũ và1 SQL test mới search/filter đạt trên DB GUID disposable. Chưa nghiệm thu browser/live auth lượt này. Rebuild/restart API để nhận query mới; chưa commit/push.

## UI nhân viên 29/10
Owner có /quan-tri/nhan-vien, menu Nhân viên; Operator bị ẩn menu và chặn route. Danh sách mặc định active, search username/displayName, paging20; tạo Operator, sửa tên, khóa/mở khóa, reset password với xác nhận. Mật khẩu nhập lại, hiện/ẩn, không lưu localStorage hoặc tự điền mặc định; unmount đóng modal xóa khỏi memory. Conflict409 khóa submit, cho GET lại version và bỏ form cũ, không replay mutation. API employee mới phải rebuild/restart backend. Không thêm migration/UI quyền Owner khác không đổi. Build frontend kiểm chứng trong lượt này; chưa live UAT/tài khoản thật, chưa UI fixture cho employees. Chưa commit/push. Đọc employees-api.md cho contract.

## Operator scope — backend29/10
Chốt Operator được nhập/xuất và giao/kiểm đếm Regular gym. Backend orders/content chuyển Owner; gym resolutions Owner; Operator không đổi publish/featured/ẩn product đã publish. FallbackOwner; kiểm tra quyền Adjustment/correction trước replay. Đọc operator-permissions.md là ma trận hiện hành, thay quyền Staff cũ trong tài liệu lịch sử. FRONTEND CHƯA THU GỌN: cần menu/redirect/hide actions riêng ở lượt sau. Không migration/DB thật, chưa commit/push.

## Operator frontend đồng bộ29/10
Menu Operator chỉ san-pham/kho/phong-gym; index redirect sang sản phẩm. Routes đơn/nội dung/trưng bày/thư viện/nhân viên/báo cáo/thông báo bị chặn mà không mount component, tránh gọi API cấm. Product Operator ẩn featured/publish step, khóa active khi product đang publish; vẫn form ảnh upload phục vụ sản phẩm. Owner giữ nguyên chức năng.
Build và14/14 Node tests đạt (gồm2 permission tests cho menu/subroutes). Không thử account Operator thật hoặc browser fixture lượt này; backend34 tests trước đó là bằng chứng phân quyền server, không phải UI UAT. Không chạy listener/test DB, không commit/push. Cần backend mới và reload frontend để nghiệm thu role thật.

## Nền sản phẩm concept A —29/10
Áp dụng họa tiết tiêu/thảo mộc SVG native public/decor/forfit-botanical.svg, không dùng ảnh AI chứa text/product làm background. Pseudo-elements2 mép, pointer-events:none, không animation, opacity giảm trên mobile, không đè nội dung. CSS components/home-products.css; container sản phẩm tối đa1280px, ảnh max620, heading46, mô tả17px, giá29; mobile30/16/27 và khung ảnh contain. Preview dùng chung CSS.
Build đạt, browser UI fixtures đo320/390/768 không page overflow, desktop1920 được xem trực quan; ảnh botanical-products-desktop/mobile trong frontend-previews. Không đổi API/DB/R2/auth, không phát sinh test SQL hoặc Telegram thật; không thêm tests cho styling. Chưa commit/push. Fixture phải dừng sau kiểm tra, giữ nguyên server người dùng.

## Tối ưu tải ảnh và bỏ banner mẫu — 29/10
- Home chỉ render banner từ HomeHero đã cấu hình; chờ API dùng nền trung tính, không flash reference-asin-hero.webp. Asset tham khảo còn được dùng trong fixtures riêng, không là fallback live.
- ProductImage cho ảnh chính sản phẩm: trạng thái tải/lỗi và nút thử lại thủ công; không retry tự động hoặc thêm query phá cache. Nút phóng to riêng giữ gallery hoạt động.
- Media cache dùng chung cho lượt đọc public/admin SAU kiểm tra quyền của Catalog. HTTP ảnh private vẫn no-store. Cache RAM64MiB/5phút, coalesce cùng key, các key khác tải song song; tối đa4 lượt fetch thực, chờ slot tối đa5giây, tối đa24 requests cache-miss đang chờ. Cache hit không giữ slot cloud. Quota9GB và operation budget không đổi, không bypass reservation.
- Kiểm chứng: build frontend đạt;9/9 GovernedMediaTests + MediaCacheConcurrencyTests SQL-enabled đạt trên DB GUID disposable, fake cloud adapter. Live public page5173 quan sát2 ảnh chính loaded=true, console không warn/error ở lần xem; screenshot frontend-previews/product-image-loading-fix.jpg. Chưa đo benchmark R2 hoặc xác định HTTP status của lỗi ảnh ban đầu. Backend đang chạy của người dùng chưa restart nên chưa chứng minh cache mới trên live API.
- Cần rebuild/restart backend để nhận cache mới; không migration. Không tạo đơn/upload/xóa dữ liệu thật, không đổi secrets; không khởi động test listener, không dừng server người dùng. Chưa commit/push.

## Khung nối banner/sản phẩm mẫu C viền cam — 29/10
Đã triển khai HomeTransition giữa hero và HomeProducts: đường phân cách cong nhẹ, khung cam đất bo góc, câu dẫn Hôm nay bạn chọn vị gì, CTA khám phá sản phẩm/tìm điểm bán và3 điểm nổi bật không thêm chứng nhận hoặc cam kết chưa xác minh. Icon lucide/SVG và CSS, không dùng ảnh mockup AI làm UI. CTA sản phẩm cuộn và focus section nổi bật, tôn trọng reduced-motion; nếu không có section thì mở /san-pham. Điểm bán mở /diem-ban. Breakpoint1000 chuyển1 cột,540 xếp nút/benefits dọc; không animation mới.
Files: frontend/src/components/HomeTransition.tsx, home-transition.css, HomeProducts.tsx (focus target), pages/storefront.tsx. Nội dung dải này hiện là text trong component, không thêm CMS/API mới. Build frontend đạt. Chưa nghiệm thu browser/mobile trực quan lượt này (tab local trong browser báo site không truy cập được); responsive mới kiểm tra theo CSS/build, không tuyên bố đã đo overflow. Không đổi backend/DB/R2/secrets; không chạy hoặc dừng server, chưa commit/push.

## Thu gọn khung C và nền khách hàng — 29/10
HomeTransition giảm padding/font/icon, mở rộng container theo viewport tối đa1680px; desktop cao khoảng nửa bản đầu tùy nội dung, không fixed height tránh cắt chữ. Desktop rộng gom benefits bên cạnh nút,1200 xuống dưới,760 xếp1 cột,540 giữ2 nút,350 xếp nút dọc; vùng bấm >=44px. CTA đổi Sản phẩm nổi bật; eyebrow HomeProducts đổi Sản phẩm nổi bật của 4fitchicken, font responsive14–19px. StoreLayout main được scope storefront-main, nền SVG botanical A dùng chung mọi route khách hàng, opacity.43 desktop/.26 mobile, thêm nhánh lá; bỏ lớp nền home trùng để không cộng opacity. Admin không đổi; nền nội dung/ảnh/thẻ vẫn ưu tiên dễ đọc. Không cam kết mockup AI pixel-identical; SVG native tái hiện phong cách.
Kiểm chứng build frontend đạt. Chưa nghiệm thu trực quan browser/mobile lượt này; không test API/SQL/R2, không commit/push. Chỉ UI và tài liệu.

## Hướng A5 điểm nổi bật + trang Câu chuyện — 30/10/2026
Thay hoàn toàn khung C bằng dải sand không khung/CTA,5 icon lucide: Sous Vide/đóng gói/đặt hàng/sẵn sàng thưởng thức/lắng nghe. Copy tiện lợi dùng Dùng ngay theo hướng dẫn trên nhãn; chưa xác minh ăn nguội trực tiếp nên không ghi Lấy từ tủ lạnh ăn ngay. Desktop5 cột,1100px3 cột,640px2 cột với mục cuối toàn hàng; không carousel/motion. HomeTransition.tsx/home-transition.css là nguồn sửa copy, chưa thêm CMS/API.
Trang Story giữ title/body/assets từ CMS và FAQ. Tiêu đề căn giữa max850, nội dung max1180, quote và bài viết cột.85/1.15, breakpoint760 về1 cột. Body CMS tách đoạn theo newline, trim khoảng trắng đầu dòng, text thuần không HTML; line-height1.95/độ rộng65ch. Ảnh giữ tỉ lệ gốc, gallery không chừa khoảng trống khi rỗng. Không sửa dữ liệu CMS thật.
Kiểm chứng build frontend đạt; mở live /cau-chuyen và xem screenshot desktop xác nhận title/body thật, chia đoạn và căn cột. Chưa đo overflow/mobile hoặc UAT toàn site; responsive mới dựa CSS. Không API mutations/SQL/R2 uploads/Telegram, không server test mới, giữ server người dùng. Chưa commit/push.

## Câu chuyện bố cục B — 30/10/2026
Khắc phục khoảng trống dài dưới slogan: story-page-grid chỉ chứa quote và2 đoạn CMS đầu. Các đoạn còn lại + đoạn hỗ trợ/CTA nằm trong story-page-continuation một cột căn giữa max780px, đường phân cách nhẹ, khoảng cách responsive. Tách paragraph một lần theo newline, không mất/nhân đôi/sửa nội dung CMS; dưới760px mở đầu cũng xếp1 cột. Gallery/FAQ giữ nguyên. Nội dung CMS nên chia đoạn bằng newline; một đoạn rất dài vẫn được giữ nguyên chứ không tự cắt câu.
Build frontend đạt; browser QA chưa thực hiện được lượt này do browser.user unavailable, không tuyên bố đã xem screenshot mới/mobile. Không đổi API/DB, không khởi động/dừng server, không commit/push.

## Sản phẩm đồng hàng, logo gốc và banner chữ lớn — 30/10/2026
ProductCard grid dùng flex-column + product-bottom margin-top:auto để giá/nút cùng baseline trong mỗi hàng khi mô tả dài ngắn khác nhau; không cắt nội dung. Chỉ có ý nghĩa so hàng ngang, mobile1 cột tự nhiên theo nội dung.
BrandArtwork chuyển sang asset mới người dùng gửi brand/4fit-orange-original.png (1620x1620), giữ byte gốc, chỉ SVG viewBox crop symbol/wordmark/full; bỏ filter đổi cam thành terracotta và blend. Logo mới không có FOR FIT CHICKEN, giữ4fit và Ức gà nấu chậm. Asset cũ giữ nguyên để bảo toàn lịch sử. Header/footer/admin cùng dùng artwork hiện hành.
Banner heading desktop44–72px, body15–18px, mở rộng copy44% max560; tablet/mobile tăng min-height theo nội dung và font responsive để không cắt chữ. Không đổi CMS/banner assets.
Build frontend đạt. Chưa kiểm chứng trực quan browser/mobile lượt này; không API/DB mutations, không server mới, chưa commit/push. Cần kiểm tra crop logo và banner với ảnh thực ở các viewport khi browser khả dụng.

## Logo bỏ nền và giảm chữ hero — 30/10/2026
BrandArtwork dùng SVG alpha filter chỉ loại vùng gần trắng, không đổi kênh RGB cam/navy; bitmap master vẫn nguyên. Đây là bỏ nền lúc render, không phải file PNG alpha xuất riêng. useId tránh trùng filter giữa các logo. Bỏ nền kem wrapper logo footer/login/sidebar. Hero desktop40–60px/body14–16, tablet35–48, mobile32–40/body14; giảm từ bản vừa tăng theo phản hồi người dùng. Build frontend đạt trước thay đổi CSS wrapper (chỉ background transparent); chưa kiểm tra browser/mobile trực quan. Không API/DB, chưa commit/push.

## Rút gọn câu chuyện trang chủ — 30/10/2026
StoryShowcase không còn render toàn bộ Story.body: gộp khoảng trắng, excerpt tối đa240 ký tự cắt theo từ và dấu…, CSS tối đa4 dòng/word-wrap trên mọi màn hình. Nút Xem thêm là Link /cau-chuyen, nằm ngoài vùng clamp nên luôn hiện. Trang Câu chuyện đầy đủ và dữ liệu CMS không đổi. Build frontend đạt; chưa browser QA lượt này. Không backend/DB, chưa commit/push.

## Phase1 review —30/10/2026
Read docs/phase1-production-readiness.md. Frontend14tests/build pass, npm audit0; backend90/90 SQL-enabled pass on disposable DB; NuGet no vulnerable packages at scan. Fixed stale schema test expected2 -> explicit3 migration IDs, no migrations changed. Draft Dockerfiles/Compose/nginx/.dockerignore added, not built: local Docker engine unavailable. NOT public-production approved yet: awaiting Bitvise read-only VPS inventory, SQL/TLS/backup/migration/media reconciliation/Linux/native tests and live smoke. No SSH/VPS modifications or secrets saved, no commit/push. User no domain, wants IP first; retain HTTPS/Secure/CSRF, private test certificate only until trusted TLS ready. Existing projects must remain isolated; no global Docker restart/prune or public SQL ports.
