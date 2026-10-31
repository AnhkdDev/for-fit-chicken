# Trưng bày trang chủ — 29/10/2026

## Rà soát nguồn nội dung và hướng dẫn Owner — 30/10/2026

Đây là kiểm tra và đồng bộ cấu hình, chưa triển khai phương án rút gọn typography vừa phân tích.
Không thay nội dung Owner đã nhập bằng copy cố định, không sửa dữ liệu SQL hoặc schema.

| Nội dung | Nguồn hiện hành / nơi Owner chỉnh | Quy tắc |
| --- | --- | --- |
| Banner: title/body/nút chính/ảnh | Nội dung website → Banner trang chủ (HomeHero) | Đọc bản công bố; cùng chữ cho mọi slide. Nút phụ và dòng tiếng Anh cố định. |
| Tiêu đề/giới thiệu/nút khu sản phẩm | Trưng bày trang chủ (HomeProducts) | Giữ title/body/linkLabel/linkUrl của Owner. Đã sửa nút đọc linkUrl thay vì luôn /san-pham; kiểm tra safeLink, fallback /san-pham. |
| Danh sách/thứ tự/ảnh chính | Trưng bày trang chủ | Khi có bản công bố, ưu tiên productIds/productMainImages; enabled=false hoặc list rỗng thì ẩn. Chỉ khi chưa có cấu hình mới dùng isFeatured. |
| Tên/mô tả/ảnh/giá/quy cách/khối lượng | Sản phẩm → từng sản phẩm, bước quy cách/ảnh | Dùng dữ liệu hiện hành, không bản sao CMS. Sửa sản phẩm đã công bố có thể lên web sau khi lưu, không đợi công bố HomeProducts. |
| Câu chuyện title/body/ảnh | Nội dung website → Câu chuyện thương hiệu | Dùng chung trang chủ và /cau-chuyen; trang chủ tóm tắt 240 ký tự, trang riêng đầy đủ. Slogan, tiêu đề phụ, thẻ ảnh phụ và nút Xem thêm cố định. |
| FAQ bổ sung | Nội dung website → Câu hỏi thường gặp | Title/body hiển thị cuối trang câu chuyện; không thay danh sách câu hỏi cố định. |
| Liên hệ chân trang | Nội dung website → Thông tin liên hệ | Title/body/linkLabel/linkUrl; không dùng ảnh. |
| Usage / SiteSettings | Mục lưu trữ trong Nội dung website | Chưa được giao diện đọc. Không hứa công bố sẽ đổi trang chủ/SEO/logo. Hướng dẫn sử dụng thật nằm trên từng sản phẩm. |
| Logo/menu/màu/nhãn khu sản phẩm/5 điểm nổi bật/nội dung trang trí | Mã nguồn | Chưa có form cấu hình. Ghi rõ giới hạn trong Owner; không giả lập ô nhập có tác dụng. |

`content-guidance.ts` giữ hướng dẫn theo consumer thực tế. Form CMS ẩn ảnh/nút không được sử dụng,
nhưng giữ nguyên giá trị cũ trong payload; không xóa dữ liệu. Mục chưa hiển thị có nhãn riêng.
Form sản phẩm phân biệt slug lưu trữ với URL public dạng ID. Preview CMS chỉ xem văn bản/ảnh,
không phải render website; preview Trưng bày dùng chung Home, khóa điều hướng và không tạo đơn.

Kiểm chứng: build TypeScript/client/SSR đạt; 18 tests frontend đạt; SSR integration dùng mock public API
kiểm tra title/body/linkUrl Owner trong HTML. Không truy cập SQL/R2/Telegram, không thay VPS.
Chưa kiểm tra thao tác Owner qua browser thật ở lượt này. Chưa commit/push.

## Đã triển khai
Admin `/quan-tri/trung-bay`: chọn sản phẩm bằng tên/ảnh, thêm/bỏ, lên/xuống, số lượng chính là số đã chọn;
bật/tắt, tiêu đề/lời giới thiệu/chữ nút. Owner/Operator lưu nháp và xem trước; Owner công bố bản đã lưu.
Không công bố khi form chưa lưu. Tối đa50 ID dương, không trùng; backend kiểm tra active/published khi lưu và công bố.
Website lọc lại sản phẩm bị ẩn/ngừng hoạt động/xóa; hết hàng vẫn trưng bày nhưng khóa mua.

HomeProducts nằm trước StoryShowcase. Mỗi sản phẩm có ảnh lớn contain, thumbnails đổi ảnh, phóng to,
mô tả ngắn, giá/quy cách và QuickBuy cũ. Không lặp ảnh thiếu. Dùng ảnh sản phẩm hiện hành theo sortOrder API;
ảnh đầu làm chính nếu chưa chọn riêng. Admin Trưng bày chọn được main asset theo từng product; không đổi thứ tự ảnh toàn cục.

Preview dùng cùng Home và HomeProducts, đọc nội dung công bố hiện tại + cấu hình trưng bày đang nhập;
khóa links và thay thao tác mua bằng thông tin quy cách, không tạo giỏ/đơn. Preview không snapshot giá/sản phẩm.
Chế độ điện thoại thu khung390 và xếp khối sản phẩm một cột; không phải trình giả lập thiết bị hoàn chỉnh.
Chưa có header/footer StoreLayout trong preview; phần thân Home được dùng chung.

## Contract và database
SiteSectionInput thêm optional `productIds: long[]?`, `enabled: bool=true`, key mới `HomeProducts`.
Tận dụng SiteContent DraftJson/PublishedJson/version/UoW business lock; không thêm bảng.
Migration mới `20261029080000_AddHomeProductsSection` mở CK_SiteContent_Section, không sửa migration cũ.
Không có cấu hình HomeProducts công bố thì dùng sản phẩm isFeatured cũ để chuyển tiếp. Có cấu hình nhưng list rỗng
hoặc enabled=false thì ẩn hẳn, không tự điền sản phẩm khác. Không dùng isFeatured để quyết định khi đã cấu hình.
Ảnh không được sao chép sang CMS: tham chiếu thông qua sản phẩm, giữ cơ chế bảo vệ media hiện có.

## Nâng cấp máy đang chạy (CHƯA áp dụng DB kinh doanh lượt này)
API đang chạy/Visual Studio khóa DLL; không dừng phiên của người dùng. Sau khi người dùng Stop Debug:

```powershell
cd D:\TaiLieu\UcGaNauCham\backend\ForFitChicken
dotnet build
dotnet ef database update --project ForFitChicken.Infrastructure --startup-project ForFitChicken.API
# Chạy lại API bằng Visual Studio hoặc launch profile https, chỉ một instance.
```

Dùng cấu hình SQL Server KDADEPTRAI\KDADEPTRAI hiện có, không LocalDB. Không reset DB/counters/credentials.
Sau đó đăng nhập frontend5173 → Trưng bày trang chủ → chọn → lưu nháp → preview → Owner áp dụng.
Không nhập R2/Telegram keys vào frontend. OpenAPI snapshot cũ cần xuất lại sau khởi động API mới.

## Kiểm chứng
Build frontend đạt;9/9 Node tests (cart/slug/thứ tự và loại sản phẩm ẩn/list rỗng).
16/16 CommerceTests SQL-enabled đạt với artifacts-path riêng, database GUID disposable; gồm HomeProducts draft
không public, publish, duplicate reject, unpublished reject. Không sửa DB kinh doanh/R2/Telegram thật.
UI fixture đã chọn2, đổi thứ tự2→1, lưu, preview desktop/mobile không mua thật, công bố, xem website2 khối đúng thứ tự,
mở QuickBuy mobile; trang390 không tràn ngang. Fixture tách published khỏi draft, không forward backend.
Ảnh minh họa fixture là reference tạm; không phải bộ ảnh sản phẩm thật. Ảnh home-display-* trong frontend-previews.
Chưa nghiệm thu auth/Operator/upload thật hay migration trên database đang dùng. Chưa commit/push.

## Bổ sung 29/10: ảnh chính, carousel và xóa
- HomeProducts.productMainImages là dictionary productId→assetId. Backend bắt buộc ảnh thuộc product đã chọn; public fallback ảnh đầu nếu ảnh được gỡ sau đó. Bỏ product khỏi cấu hình cũng bỏ mapping.
- HomeHero.assetIds là danh sách slide: upload/chọn và lên/xuống trong Nội dung website → Banner trang chủ → Lưu nháp → Owner công bố. Tiêu đề/body/nút dùng chung các slide; không autoplay. Khách dùng nút trái/phải/số hoặc phím mũi tên. Tối đa20 ảnh theo giới hạn CMS cũ.
- Thân Home chỉ hero + HomeProducts + StoryShowcase. Bỏ value strip, usage, gym teaser, FAQ khỏi Home; header/footer/giỏ/trang sản phẩm/điểm bán vẫn giữ. Nội dung CMS cũ không bị xóa khỏi DB.
- Owner có DELETE /admin/products/{id}, /admin/inventory/batches/{id}, JSON {version}; CSRF/rate policy Admin và UoW/SQL business lock như cũ, có audit. UI xác nhận không thể hoàn tác.
- Product có batch/OrderItem hoặc còn trong draft/published ProductIds bị chặn; dùng ngừng hoạt động/ẩn và bỏ khỏi trưng bày khi phù hợp. Xóa product chưa dùng sẽ gỡ SKU/ảnh liên kết trong cùng transaction, không xóa tệp R2.
- Batch có WarehouseBalance (kể cả0), StockDocumentLine hoặc GymStockCountLine bị chặn. Không xóa lịch sử để sửa sai tồn; dùng chứng từ/khóa lô hiện có.
- Không thêm migration lượt bổ sung này: ảnh chính nằm trong JSON. Migration HomeProducts từ lượt trước vẫn cần nếu máy chưa áp dụng; build/restart API để nhận DTO/endpoints mới. Không dừng Visual Studio của người dùng.
- Build frontend đạt,10/10 Node tests;18/18 SQL CommerceTests disposable đạt gồm xóa chưa dùng/stale version/chặn lô và sản phẩm đã dùng/chọn ảnh đúng product. Không xóa bất kỳ dữ liệu kinh doanh thật.
- Browser fixture: chọn ảnh2→lưu/công bố→main src /api/media/101; carousel2→1 trên mobile390 không overflow; 3 section Home đúng; xóa fake product giảm6→5; hộp xóa lô khóa nút tới khi tick. Không gọi R2/Telegram hoặc SQL kinh doanh thật.
- Chưa test Operator UI/live API auth và chưa chụp ảnh sản phẩm thật; fixture chỉ chứng minh UI, không thay SQL test. Chưa commit/push.

## Cập nhật trình bày30/10 — thay phân cấp tiêu đề trước
H2 nay là Sản phẩm nổi bật cố định; HomeProducts.title vẫn giữ nguyên và hiển thị thành dòng phụ, không ghi đè JSON/DB. Owner sửa tại Trưng bày, trường Dòng phụ dưới Sản phẩm nổi bật; body tùy chọn. Nhãn cũ Sản phẩm nổi bật của 4fitchicken không còn. Mỗi sản phẩm bỏ eyebrow và label hương vị lặp, một SKU hiển thị netWeightGrams cạnh giá, nhiều SKU Từ + số quy cách; tên SKU vẫn đầy đủ trong popup. Mô tả trang chủ chỉ xem3 dòng, bản đầy đủ tại chi tiết. Gallery giới hạn chiều cao contain + thumbnails + phóng to; preview Owner dùng chung component. Chưa visual UAT do Windows computer-use không xác định được URL và đã dừng.

## Công cụ căn ảnh Owner —30/10/2026
1. Vào Trưng bày trang chủ, chọn sản phẩm và ảnh chính.
2. Chọn Căn ảnh trang chủ; Vừa khung giữ đủ ảnh, Phủ kín cắt phần ngoài khung. Kéo ảnh hoặc dùng slider, phóng1–3×.
3. Chuyển Máy tính/Điện thoại. Mặc định mobile kế thừa desktop; bật Căn riêng cho điện thoại để chỉnh độc lập. Đặt lại chỉ reset khung đang chỉnh; Hủy không áp dụng.
4. Áp dụng vào bản nháp, Xem trước trang chủ, Lưu bản nháp, Áp dụng lên website. Áp dụng trong hộp chỉnh chưa tự lưu/công bố.

Ảnh gốc ở đây nghĩa là tệp WebP đã lưu trong hệ thống, không phải ảnh đầu vào trước pipeline tối ưu. Không upload tệp mới khi căn, không tăng stored quota, không đổi trang chi tiết/ảnh phóng to. Không giảm dung lượng tải ảnh bằng thao tác này.
Khung editor/public/preview4:3. JSON lưu productId→{assetId,desktop,mobile?}; chỉ ảnh chính khớp asset mới được áp dụng. Đổi main reset config; bỏ product xóa config khỏi nháp. Nếu ảnh bị gỡ sau công bố thì renderer bỏ config không khớp và dùng ảnh fallback contain. Không tự xóa liên kết/tệp R2 hoặc sửa lịch sử.
Save/publish backend kiểm tra quan hệ và giới hạn; field optional trong JSON SiteContent nên không migration. Rebuild/restart API và frontend mới đồng thời trước khi nghiệm thu, không deploy frontend crop với API cũ. Frontend giữ draft và báo lỗi nếu response bỏ config do API cũ.
Giá/nút nay theo vùng tên2 dòng/mô tả3 dòng/tình trạng1 dòng, ngay dưới mô tả; không còn spacer dựa trên chiều cao ảnh. Chi tiết giữ nội dung đầy đủ.
Build và22 unit/2SSR/20SQL Commerce tests đạt, chưa browser drag/touch UAT. Không thay dữ liệu thật hoặc triển khai VPS.
