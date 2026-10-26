# Đánh giá frontend tham khảo tải về

Ngày 26/10/2026. Phạm vi: kiểm tra tĩnh thư mục `CodeReverseASinTayBac/REA-Web`, đọc code/component/styles và xem ảnh hero. Không sửa source tải về, không cài dependencies, không chạy Express/SQLite, chưa build hoặc kiểm thử browser của bản local.

## 1. Kết luận

Có giá trị làm nguồn tham khảo và trích xuất frontend. Đây là dự án React/Vite có JSX component, không chỉ một bộ bundle không đọc được. README tự mô tả là bản reverse/reimplementation; nội dung README không chứng minh đây là source gốc hoặc khớp hoàn toàn website đang chạy. Không dùng các khẳng định về backend gốc trong README làm căn cứ kiến trúc dự án của nhóm.

Đề xuất giữ làm reference, trích xuất phần giao diện sang frontend của nhóm theo phạm vi đã chốt; không đổi backend ASP.NET Core/SQL Server thành Express/SQLite và không thay ERD theo mô hình demo.

## 2. Những gì kiểm tra được

- Frontend package khai báo React 18, Vite 6, React Router 6, lucide-react, qrcode.react; đây là dependency của bản tải về, chưa phải đề xuất version cho dự án mới.
- Có App/router, Header/Footer, ProductCard, CartDrawer, CheckoutModal, CartContext và các page riêng.
- Main import index.css trước custom.css: có tầng CSS override.
- index.css khoảng 288KB, toàn bộ trên một dòng; custom.css khoảng 9KB/465 dòng. Không nên sửa stylesheet minified toàn cục theo kiểu thử và ghi đè mãi.
- HomePage.jsx khoảng 740 dòng, trộn nội dung hardcode, section trình bày, modal, dữ liệu API và logic rail. Cần chia section khi dùng lại.
- public/images có 41 ảnh khoảng 10,53MB tổng file; có SVG mask/mép giấy và fonts ở public/assets.
- Kiểm tra tĩnh trên 26 file JS/JSX/CSS: chưa thấy import tương đối thiếu; 74 đường dẫn ảnh/font trực tiếp tìm được đều tồn tại. Không bao gồm URL động, response API, tính hợp lệ media hay khả năng compile.
- Chưa có node_modules frontend/backend tại thời điểm kiểm tra. Không thấy file test/spec/license riêng qua lần tìm tên file; package root tự khai báo MIT, chưa xác minh quyền của từng tài nguyên bên ngoài.

## 3. Cách banner thực sự được dựng trong bản này

`frontend/src/pages/HomePage.jsx` phần hero dùng `/images/asin/hero-watercolor.webp`, ảnh có kích thước khai báo 1942x809. Đã mở ảnh để kiểm tra: nền núi, bệ gỗ, túi sản phẩm, gia vị và thịt đều nằm trong cùng ảnh raster không chữ.

JSX bên trên đặt eyebrow, H1, mô tả, CTA và chữ trang trí; CSS tạo vị trí/khoảng trống/hiệu ứng. Vì vậy:

- Có thể giữ cấu trúc hero và cách đặt typography/CTA.
- Thay câu chữ bằng nội dung nhóm tương đối dễ.
- Không thể chỉ đổi một URL ảnh túi rồi tự thay toàn bộ thịt trên banner: chúng đã ghép cùng ảnh nền.
- Cần làm hero ức gà riêng từ bao bì và ảnh món thật; chuẩn bị bố cục mobile riêng.
- Ảnh/font/mask đang mang nhận diện A Sỉn là tài nguyên reference; hình công bố cần phù hợp thương hiệu và quyền sử dụng của nhóm.

## 4. Ma trận tận dụng

| File/nhóm | Tận dụng | Cần thay/điều chỉnh |
| --- | --- | --- |
| HomePage.jsx | Thứ tự section, ảnh/chữ, CTA, giá trị, câu chuyện | Tách Hero/Story/Featured/Usage/Gym/FAQ; bỏ dữ liệu sản phẩm/review hardcode |
| Header.jsx/Footer.jsx | Header responsive, search dialog, menu/footer | Logo/menu/link và liên hệ nhóm; không dùng số Zalo cũ |
| styles/index.css | Token, font, selector layout, texture/mask/spacing để nghiên cứu | Format/tách phần cần, tránh giữ toàn bộ class không dùng và override xung đột |
| styles/custom.css | Các style bổ sung, form/modal/toast | Hợp nhất thành token/component có thứ tự rõ |
| ProductCard.jsx | Bố cục ảnh/tên/quy cách/giá/CTA | DTO SKU mới, ảnh thật, availability đúng, không mặc định xuất xứ Tây Bắc |
| ProductsPage/ProductDetailPage | Khung tìm/lọc/sort, chi tiết và số lượng | Tích hợp product+variants API, error states, bỏ ưu đãi/ship/đổi trả không chốt |
| CartDrawer/CheckoutModal | Visual tham khảo của giỏ/form | Sửa điều khiển mở, thiết kế các bước review, copy COD, không phí giao |
| CartContext | Ý tưởng local persistence và UI state | Dùng VariantId, revalidate giá, idempotency submit, không fallback catalog demo |
| services/api.js | Điểm tập trung gọi API hiện có | Thay client/OpenAPI ASP.NET, auth, validation/conflict/rate-limit và retry semantics |
| AdminDashboard.jsx | Chỉ tham khảo bố cục | Thiết kế lại theo quyền thật, kho theo lô, gym/kiểm đếm và báo cáo MVP |
| GiftBox3D, gift customizer, deals, journal, newsletter, chatbot | Chưa cần | Không kéo vào core chỉ vì source có |
| backend Express/SQLite + JSON seed | Chỉ đọc để hiểu contract cũ khi cần | Không dùng làm backend bán thật hoặc nguồn dữ liệu nhóm |

## 5. Những vấn đề cụ thể phát hiện qua đọc code

### 5.1. Chuyển từ giỏ sang checkout đang sai logic render

CartDrawer.jsx: dòng 21 `if (!isCartOpen) return null`. handleOpenCheckout tại khoảng dòng 30 đóng isCartOpen rồi mở checkoutModalOpen, nhưng CheckoutModal lại nằm phía dưới cùng component đó. Render tiếp theo trả null trước khi tới modal, nên theo logic code hiện tại checkout không xuất hiện.

Hướng sửa khi triển khai: đặt checkout ở component/layout độc lập với điều kiện mở giỏ, hoặc dùng một controller state machine Cart -> Recipient -> Review -> Received. Chưa áp dụng sửa trong lần review này và chưa kiểm thử runtime.

### 5.2. Logic giá/phí không khớp MVP

CartContext.jsx khoảng dòng 154 hardcode ship 30.000đ, miễn phí từ 500.000đ. CartDrawer có progress miễn phí, CheckoutModal hiển thị tổng có ship. Cần bỏ khỏi luồng mới; ghi tiền hàng chưa gồm phí giao và nhóm sẽ trao đổi khi gọi.

### 5.3. Cart theo product, chưa có SKU thật

CartContext dùng productId/product.id cùng weight dạng text, lưu name/price/image trong localStorage. Chưa bảo đảm giỏ phân biệt các quy cách theo VariantId của ERD mới. api.createOrder gửi items: cart nguyên trạng; chưa có idempotency header trong đoạn client hiện tại. Cần client mới, server tính giá và snapshot, xử lý giá đổi/timeout/replay đúng contract.

### 5.4. Fallback dữ liệu và hardcode thương hiệu

CartContext khởi tạo DEFAULT_PRODUCTS và nuốt lỗi tải API; nếu response rỗng/lỗi có thể vẫn hiện sản phẩm demo. HomePage có FEATURED_PRODUCTS, REVIEWS_DATA hardcode và nội dung địa phương/claim khác. Bản production của nhóm phải hiện trạng thái lỗi/rỗng hoặc nội dung đã duyệt, không thay bằng catalog của mẫu.

### 5.5. Admin chưa có nền xác thực phù hợp

AdminDashboard dùng cờ localStorage `asin_admin_auth`. API client không gửi credential/token phù hợp ở các lời gọi admin đã xem. Express login trả token hằng; server.js mount các router trực tiếp, admin stats route đã đọc không có middleware kiểm tra xác thực. Đây không phải nền auth/role để dùng cho dữ liệu bán hàng thật. Không sao chép cơ chế này; dùng Identity và policy ASP.NET đã thiết kế.

### 5.6. Mức độ giống website và SEO chưa được xác nhận

Source chứa cả nhóm class asin/moc và nhóm style/component dựng thêm; ProductsPage hiện có intro xanh và nhiều inline style, không có cơ sở nói mọi trang giống bản live. index.html là SPA root, metadata thương hiệu cũ; chưa có bằng chứng SSR/prerender. Cần chạy/so ảnh nhiều viewport và quyết định render/SEO trước khi chốt frontend mới.

## 6. Cách dùng thực tế trong dự án

1. Giữ nguyên thư mục reference, không refactor trực tiếp rồi mất bản đối chiếu.
2. Sau backend/contract, tạo frontend React TypeScript của nhóm theo kế hoạch đã chốt.
3. Trích xuất Header/Footer/Hero/Story/ProductCard và style liên quan; thay branding, chia component và thống nhất tokens.
4. Làm ảnh hero ức gà và ảnh SKU thật; xem desktop/mobile trước khi gọi visual hoàn thiện.
5. Làm giỏ/checkout/controller mới theo SKU và COD pending, sửa lỗi render ngay ở cấu trúc.
6. Nối API ASP.NET, auth admin, kho/gym/báo cáo mới; không lấy database demo làm chuẩn.
7. Build và browser QA; kiểm tra submit đơn có mặt ở admin, Telegram job, giá đổi, lỗi mạng, quyền và responsive.

Tải về này giảm công đoán bố cục, tìm selector và dựng component trình bày. Chưa có cơ sở định lượng phần trăm hoặc thời gian tiết kiệm. Bản review này không chứng minh dự án đã build/chạy hoặc sẵn sàng mở bán.
