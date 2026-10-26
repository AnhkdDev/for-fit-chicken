# Kế hoạch frontend MVP — Ức gà nấu chậm

Ngày: 26/10/2026. Phạm vi: kế hoạch thiết kế và triển khai, chưa sinh code hoặc xuất bản website.

Cập nhật: người dùng đã cung cấp bản React/Vite dựng lại ở `CodeReverseASinTayBac/REA-Web`. Xem [đánh giá source local](frontend-source-review.md) để trích xuất component/styles; chưa coi là source gốc hay bản đã build/test. Kế hoạch ASP.NET/SQL Server và nội dung/ảnh riêng của nhóm giữ nguyên.

Căn cứ: [Yêu cầu MVP](mvp-requirements.md), [Màn hình và luồng](mvp-screens-and-flows.md). Quyết định nghiệp vụ đã chốt có ưu tiên cao hơn phong cách của mẫu tham khảo. Các thông số màu, kích thước và cấu trúc dưới đây là đề xuất thiết kế, không phải thông số đo từ website mẫu.

## 1. Kết quả khảo sát website tham khảo

Nguồn: [A Sỉn — trang chủ](https://asintaybac.com/), [danh mục](https://asintaybac.com/san-pham), [chi tiết](https://asintaybac.com/san-pham/trau-gac-bep-tay-bac).

Ngày 26/10 đã mở lại trang chủ, danh mục, chi tiết và drawer giỏ rỗng bằng trình duyệt, xem cấu trúc công khai và ảnh hiển thị ở viewport khoảng 1280px. Trước đó trong chat đã kiểm tra giỏ có hàng và form checkout, không gửi đơn; người dùng cũng cung cấp ảnh desktop và ảnh bước kiểm tra COD. Chưa kiểm thử responsive nhiều viewport, chưa kiểm chứng backend, lưu đơn, bộ lọc/yêu thích, công thức phí hoặc cách họ triển khai lớp ảnh.

| Quan sát công khai | Đưa vào thiết kế của nhóm |
| --- | --- |
| Nền kem/ngà, đỏ nâu, tiêu đề serif lớn, icon nét mảnh | Giữ tinh thần ấm, có khoảng thở; dùng màu và font nhất quán, tên thương hiệu của nhóm |
| Hero chữ trái, cụm bao bì/thực phẩm lớn bên phải, nền hòa vào trang | Dùng ảnh bao bì ức gà thật + đĩa thịt, nền bàn ăn/bếp; vùng chữ sạch, chuyển tiếp mềm |
| Thanh giá trị ngay dưới hero | Đổi thành lợi ích/sự thuận tiện đúng sản phẩm, COD và điểm bán; không dùng claim chưa có căn cứ |
| Câu chuyện đan xen ảnh và chữ, card sản phẩm, phần quy trình | Áp dụng cho thương hiệu, cách dùng/bảo quản và sản phẩm ức gà |
| Danh mục có tìm, sắp xếp giá, lọc và nút thêm giỏ | Làm danh mục gọn; bộ lọc vị/quy cách chỉ hiện khi đủ dữ liệu |
| Chi tiết có ảnh, quy cách, giá, số lượng và thêm giỏ | Giữ, bổ sung thông tin phù hợp thực phẩm; không tự tuyên bố số tồn chính xác |
| Drawer nền kem, overlay tối, bước điền thông tin rồi kiểm tra COD | Áp dụng cho giỏ và gửi yêu cầu COD; mobile chiếm trọn màn hình |
| Hộp quà 3D, bài viết, newsletter, yêu thích, chat, đánh giá minh họa | Không đưa tự động vào MVP; Zalo là liên kết ngoài, không chatbot. Phản hồi chỉ đăng khi có thật |
| Nêu miễn phí từ 500.000đ và có phí ở bước đặt hàng | Không sao chép chính sách. Nhóm chưa tính phí giao trong app |

Mục tiêu giống về nhịp bố cục, chất lượng hình ảnh, typography và trải nghiệm mua gọn; không bê logo, nội dung hoặc tài nguyên của thương hiệu mẫu. Kế hoạch không yêu cầu đọc source riêng hoặc suy ra công nghệ của họ.

## 2. Mục tiêu và phạm vi

1. Khách hiểu sản phẩm và tìm được cách mua trong vài thao tác.
2. Banner là điểm nhấn hình ảnh, nhìn rõ bao bì và món ăn trên desktop/mobile.
3. Giỏ và yêu cầu COD dùng API thật, không buộc tài khoản, không giả lập thành công.
4. Admin thao tác được trên điện thoại và máy tính; ưu tiên rõ dữ liệu hơn trang trí.
5. Bám backend ASP.NET Core và SQL Server đã chọn; không tự thêm backend khác, thanh toán online hoặc tích hợp ngân hàng.

Không làm trong MVP: 3D, hội viên, wishlist, voucher/phí giao, blog/newsletter đầy đủ, chat nội bộ, QR/loa, công nợ/hoa hồng gym, quản lý shipper, raw-material production. Không có tài khoản khách hoặc cổng tra cứu đơn công khai.

## 3. Hướng mỹ thuật đề xuất

### 3.1. Cảm giác tổng thể

Thực phẩm chỉn chu, ấm, ngon miệng và thuận tiện. Giữ nền kem và nhịp tiêu đề lớn của mẫu; thay chất liệu núi rừng bằng mặt bàn/bếp, bao bì thật, đĩa ức gà và gia vị theo vị. Không dùng phong cách dashboard cho landing page; không đưa toàn bộ thông tin thành các ô card giống nhau.

### 3.2. Token thiết kế bản đầu

| Token | Đề xuất |
| --- | --- |
| Nền trang | `#FFF7EA` |
| Nền khối phụ | `#F4E7D1` |
| Màu chủ đạo / nút chính | `#6B2027` |
| Chữ nội dung | `#30271F` |
| Chữ phụ | `#655C50` |
| Viền trang trí | `#D9C5A8`; viền input/focus phải đủ dễ phân biệt, không chỉ dùng màu trang trí |
| Font | Một serif có đủ dấu Việt cho heading, một sans có đủ dấu Việt cho body/form; xác minh giấy phép và hiển thị trước khi chốt file font |
| Nội dung đọc | 16px trở lên cho body/input mobile, line-height khoảng 1.5–1.7 |
| Chiều rộng nội dung | Tối đa khoảng 1200–1280px, hero có thể full bleed |
| Khoảng cách | Thang 4/8/12/16/24/32/48/64/96px |
| Bo góc | Nút khoảng 4–8px, panel khoảng 12px; không lạm dụng pill |
| Chuyển động | Hover/fade nhẹ 150–250ms, tôn trọng reduced motion |

Kiểm tra tương phản bằng công cụ khi hiện thực; bảng màu chưa được tuyên bố đã đạt accessibility. Admin dùng font sans, nền sáng đơn giản, màu chủ đạo chỉ ở nút/điểm nhấn và trạng thái có nhãn chữ.

## 4. Kế hoạch hình ảnh và banner

### 4.1. Chọn cách dựng

Ghép sẵn cảnh gồm nền + mặt bàn + sản phẩm + bóng đổ, đặt tiêu đề/nút bằng HTML/CSS. Tránh ảnh gộp cả chữ, tránh bắt đầu với nhiều lớp parallax hoặc 3D. Nếu sau này cần chuyển động sản phẩm, tách cutout từ nguồn đã giữ sẵn.

Desktop: chữ trái khoảng 35–40%, cụm sản phẩm phải khoảng 60–65%; hero cao theo nội dung khoảng 600–760px trên màn hình rộng, không khóa cứng khiến chữ tràn. Mobile: tiêu đề, mô tả, CTA rồi ảnh cảnh dọc; có thể tách nền/ảnh khỏi vùng chữ để giữ cả sản phẩm. Không crop ảnh desktop tùy tiện rồi gọi là mobile.

### 4.2. Checklist tài nguyên

| Mã | Tài nguyên | Đầu vào / đầu ra | Điều kiện đạt |
| --- | --- | --- | --- |
| IMG01 | Logo | Bản chính SVG, bản màu đơn và favicon | Đọc được ở header mobile; tên thương hiệu chưa chốt dùng nhãn tạm trong bản nháp |
| IMG02 | Bao bì thật | Ảnh chính diện + góc 3/4 độ phân giải cao | Nhãn rõ, sạch, không phản sáng che thông tin |
| IMG03 | Món ăn | Ức gà cắt lát trên đĩa, ánh sáng cùng hướng | Thấy kết cấu, trình bày đúng sản phẩm thực tế |
| IMG04 | Cutout master | PNG/WebP alpha của bao bì/món ăn; giữ file gốc | Không viền trắng, không mất mép hoặc nhãn |
| IMG05 | Hero desktop | File nguồn có layer, bản xuất web; canvas tham khảo 1920x900 | Khoảng chữ sạch, bóng hợp lý, chủ thể không bị cắt |
| IMG06 | Hero mobile | Bố cục dọc riêng, canvas tham khảo 900x1200 | Bao bì và đĩa đủ lớn trên 360–430px |
| IMG07 | Card sản phẩm | Mỗi SKU một ảnh chính, khung vuông hoặc 4:5 thống nhất | Không card nào bao bì bé bất thường hoặc bị crop |
| IMG08 | Câu chuyện/cách dùng | 2–4 ảnh thật về nhóm, sản phẩm hoặc thao tác | Không giả chứng nhận, cơ sở hoặc đánh giá |
| IMG09 | Điểm bán | Ảnh gym nếu có quyền sử dụng | Đúng điểm bán thực, thiếu ảnh dùng bố cục không ảnh |

Quy trình: chốt tên/bao bì -> chụp -> tách nền -> ghép concept -> xem desktop/mobile -> sửa -> xuất nhiều kích thước. File gốc giữ ngoài bundle deploy; bản tối ưu đặt trong assets hoặc storage theo hợp đồng backend. Có manifest tên file, mục đích, kích thước, alt và điểm lấy nét. Không tải ảnh của mẫu về để thay sản phẩm của mình.

AI có thể hỗ trợ concept hoặc nền; nếu dùng mockup bao bì để duyệt phải ghi rõ mockup và thay/đối chiếu ảnh sản phẩm thật trước khi mở bán. Việc sinh ảnh chưa nằm trong lần lập kế hoạch này.

### 4.3. Handoff hero

- Một bản desktop 1440px và một bản mobile 390px thể hiện cả chữ/nút bằng layout mẫu.
- Một bộ cảnh ảnh riêng không chữ; logo và cutout riêng.
- Ghi rõ vùng an toàn cho chữ, vùng giữ chủ thể, cách xuống dòng tiêu đề và crop cho từng breakpoint.
- Bản nháp headline: “Ức gà mềm ngon, gọn bữa mỗi ngày.”; nội dung cuối cần phù hợp sản phẩm thật.
- CTA: “Khám phá sản phẩm” và “Tìm điểm bán”. Không tự dùng slogan hoặc số liệu dinh dưỡng của mẫu.

## 5. Trang chủ — thứ tự và đặc tả

| Thứ tự | Section / component | Desktop | Mobile | Dữ liệu / hành động |
| --- | --- | --- | --- | --- |
| 1 | `SiteHeader` | Logo, menu, tìm kiếm, giỏ; sticky nền rõ khi cuộn | Logo, giỏ, menu; tìm kiếm trong panel | Menu: Trang chủ, Sản phẩm, Câu chuyện, Điểm bán, Liên hệ |
| 2 | `HeroSection` | Chữ trái, ảnh phải/full bleed có lớp nền mềm | Chữ trên, ảnh dọc dưới | Hai CTA; một cảnh tĩnh tốt trước, không carousel tự chạy |
| 3 | `ValueStrip` | 3–4 lợi ích với icon nét mảnh, đường chia mỏng | Grid 2 cột hoặc danh sách | Chỉ đăng nội dung có căn cứ; không bịa số gram đạm/calo |
| 4 | `BrandStorySection` | Bố cục ảnh/chữ bất đối xứng, tiêu đề serif | Một cột, ảnh vừa phải | Câu chuyện nhóm tự sản xuất; liên kết giới thiệu |
| 5 | `FeaturedProducts` | 3 card nếu có ít SKU, tối đa 4 cột khi đủ rộng | Một hoặc hai cột tùy độ đọc | 3–4 sản phẩm thực, nút xem thêm và thêm giỏ |
| 6 | `UsageAndCareSection` | Ảnh + 3 bước sử dụng, bảo quản | Bước xếp dọc | Nội dung cụ thể do nhóm cung cấp; không tự tạo nhiệt độ/hạn dùng |
| 7 | `GymLocationsPreview` | 2–3 điểm bán thật và CTA | Card một cột | Tên, địa chỉ, liên hệ; không badge còn hàng |
| 8 | `FaqSection` | Accordion ngắn | Tương tự, tap target lớn | COD, xác nhận qua điện thoại, khu vực HN, phí trao đổi khi gọi, cách dùng |
| 9 | `ContactSection` + footer | Liên hệ và hợp tác, menu phụ | Xếp dọc | Liên kết thật; chỉ bật Zalo khi có số của nhóm |

Đánh giá chỉ thêm khi có phản hồi thật và sự đồng ý sử dụng. Chưa cần section tin tức hoặc newsletter. Section trống được ẩn có chủ đích, không lấy dữ liệu giả cho đủ trang.

## 6. Danh mục, chi tiết và điểm bán

### 6.1. Danh mục `/san-pham`

- Header gọn + giới thiệu ngắn, không đặt hero khổng lồ/3D đẩy sản phẩm xuống quá xa.
- Tìm tên; sắp xếp mặc định/giá tăng/giá giảm. Chưa cần sidebar nhiều danh mục khi chỉ có vài SKU.
- Nếu đủ vị/quy cách thì dùng filter chips. Bộ lọc/sắp xếp thể hiện trên query string để back/share hoạt động.
- Card: ảnh thống nhất, tên, vị/quy cách, giá, CTA. SKU nhiều lựa chọn cần chọn đúng trước khi thêm; không đoán quy cách.
- Trạng thái: tải, lỗi + thử lại, không kết quả + xóa lọc, sản phẩm ngừng bán.
- Xem nhanh là ưu tiên sau khi luồng chính hoàn chỉnh, tái dùng nội dung chi tiết; không tạo logic giá/giỏ thứ hai.

### 6.2. Chi tiết `/san-pham/:slug`

- Hai cột: ảnh lớn bên trái, tên/giá/quy cách/số lượng/CTA bên phải; mobile xếp dọc.
- Ảnh bao bì dùng contain, không cover cắt nhãn. Nếu nhiều ảnh có thumbnails điều khiển bằng bàn phím.
- Các phần dưới: mô tả, thành phần/dinh dưỡng có dữ liệu, sử dụng, bảo quản; thiếu số liệu thì không hiển thị số tự tạo.
- Dòng “Đơn sẽ được gọi xác nhận trước khi giao”; phí giao chưa gồm trong tiền hàng.
- Thông tin khả dụng dựa vào API công khai; không suy từ tồn gym hoặc cam kết “sẵn sàng giao ngay”.
- Sản phẩm liên quan lấy dữ liệu thật, không bắt buộc thêm module gợi ý thông minh.

### 6.3. `/diem-ban`, `/gioi-thieu`, `/lien-he`

- Điểm bán là danh sách gym đang công bố; không cần bản đồ tích hợp, dùng liên kết mở bản đồ từ địa chỉ đã có khi phù hợp.
- Chỉ hiển thị thông tin liên hệ đã được phép công bố; không lộ người phụ trách/ghi chú nội bộ.
- Giới thiệu và liên hệ dùng chung block ảnh/chữ, heading, FAQ, footer; chưa làm hệ thống bài viết.

## 7. Drawer giỏ và checkout COD

### 7.1. Hình thức

Desktop panel phải rộng khoảng 440–520px, mobile full width; overlay đủ phân biệt nền. Header tên bước + đóng, nội dung tự cuộn, vùng CTA luôn tiếp cận được. Trên mobile xử lý bàn phím, safe area và chiều cao viewport động, không che nút hoặc input.

Drawer được render một lần trong layout chung, không nhân bản theo từng trang. Quản lý focus khi mở/đóng, Escape, trap focus, chặn scroll nền và trả focus về nút mở. Menu mobile và drawer không mở chồng nhau. Tránh đóng ngoài ý muốn khi nhấn nền ở bước nhập form; đóng không xóa dữ liệu đang nhập trong phiên.

### 7.2. Các bước

| Bước | Hiển thị | CTA / điều kiện |
| --- | --- | --- |
| Cart | Ảnh nhỏ, SKU/quy cách, số lượng, tiền dòng và tạm tính | “Tiếp tục đặt hàng”; giỏ rỗng không đi tiếp |
| Recipient | Tên, điện thoại, địa chỉ chi tiết tại Hà Nội, ghi chú tùy chọn | “Kiểm tra đơn”; có nhãn trường và lỗi cụ thể |
| Review | Tóm tắt thông tin có nút sửa, dòng hàng, tiền hàng, COD | “Gửi yêu cầu đặt hàng COD”; chỉ submit ở đây |
| Received | Mã tham chiếu và thông báo đã nhận yêu cầu | “Tiếp tục xem sản phẩm”; không nói đã thanh toán hoặc đã xác nhận |

Copy bắt buộc trong Review: “Tiền hàng chưa bao gồm phí giao hàng. Chúng tôi sẽ gọi xác nhận đơn và trao đổi phí giao trước khi giao.” Không dùng dòng “Giao hàng 0đ” hoặc số 30.000đ từ mẫu.

### 7.3. Dữ liệu và lỗi

- Giỏ chỉ lưu SKU + số lượng và version schema trong local storage; ảnh/tên/giá lưu tạm nếu cần nhưng giá hiện hành lấy lại từ API.
- Không lưu tên, điện thoại, địa chỉ vào local storage mặc định; form giữ trong memory của phiên. Không cache response admin/đơn vào service worker.
- Giỏ và đơn chờ không giữ tồn; chỉ hiển thị tham khảo và xử lý kết quả từ backend. Người dùng đã chốt giữ hàng khi admin xác nhận ngày 26/10/2026.
- Có trạng thái submitting, disable nút và dùng idempotency key cho một yêu cầu logic. Timeout/mất mạng: giữ key và nội dung để retry, không tự gửi request mới với key mới.
- Nếu thay nội dung sau lỗi chưa rõ kết quả, giải quyết kết quả cũ theo hợp đồng trước khi tạo ý định mới. Không tự coi mọi HTTP lỗi là đơn chưa được tạo.
- `400/422`: lỗi trường theo contract; `409`: hiển thị xung đột giá/hàng, cập nhật và cho xem lại; `429`: thông báo thử lại theo hướng dẫn server; `5xx/network`: không xóa giỏ.
- Mã lỗi cụ thể phải theo OpenAPI cuối, không hardcode giả định cả hai status validation luôn tồn tại.
- Sau thành công chỉ xóa các lượng hàng thuộc đơn vừa gửi; nếu giỏ thay đổi ở tab khác không xóa nhầm phần mới. Lưu tối thiểu kết quả trong phiên nếu cần chống thao tác lại; không mở API public lấy PII chỉ bằng mã đơn.
- React hiển thị và kiểm tra form; server quyết định đơn giá, tổng và quyền tiếp nhận. Không có thanh toán thẻ/chuyển khoản cho checkout MVP.

## 8. Admin responsive

Layout riêng `AdminLayout`: sidebar desktop, menu gọn mobile; không tải hero/font trang trí trên admin. Menu: Tổng quan, Đơn hàng, Sản phẩm, Kho & lô, Gym, Báo cáo, Nội dung; Tài khoản/Nhật ký theo quyền.

| Nhóm | Thiết kế tương tác ưu tiên |
| --- | --- |
| Tổng quan | Card số liệu đúng định nghĩa, đơn cần gọi và cảnh báo; không biểu đồ lợi nhuận bịa từ dữ liệu thiếu |
| Đơn | Desktop table, mobile card; filter trạng thái/nguồn/ngày; chi tiết có timeline, nút gọi `tel:`, ghi chú và hành động hợp lệ |
| Xác nhận đơn | Hiển thị đủ hàng/không đủ theo phản hồi backend; lỗi concurrency yêu cầu tải lại, không optimistic xác nhận |
| Ghi thu COD | Nút riêng với bàn giao/hoàn tất; theo quyền và quy tắc nghiệp vụ cuối |
| Sản phẩm/lô | Form có validation, ảnh preview, công bố/ngừng bán; field giá vốn chỉ người có quyền nhận từ API |
| Kho | Cột lượng vật lý, giữ, có thể bán, không đủ điều kiện; không gộp thành số tồn mơ hồ |
| Gym | Thẻ “Đã kiểm đếm … lúc …”, lịch sử giao/thu hồi sau mốc; nút “Tạo kiểm đếm” chủ động |
| Kiểm đếm | Form từng lô dễ nhập điện thoại; phân biệt nhập 0 với chưa nhập; nháp trên server khi contract có, cảnh báo rời khi chưa lưu |
| Chênh lệch | Hiển thị lượng cần giải thích, bán xác nhận, thiếu/thừa chưa rõ; không tự điền toàn bộ thành bán |
| Báo cáo | Phân biệt giá trị đơn, giao hoàn tất, COD thực nhận; số bán gym theo khoảng kiểm đếm, không tổng tiền gym |
| Nội dung | Trường cố định ảnh/tiêu đề/mô tả/CTA/FAQ, không page builder; chỉ dữ liệu đã xuất bản ra public |

Không làm wizard giao vận, màn hình nhân viên gym, hoa hồng hoặc tài chính gym. Điều chỉnh/sửa sai có lý do và xác nhận trước thao tác; không tự cập nhật chứng từ lịch sử bằng sửa số trên bảng.

## 9. Component và tổ chức source đề xuất

Một codebase React + TypeScript, public/admin chia layout và lazy-load route admin. Chưa chốt package/version khi chưa có repo frontend; không tự thay React bằng framework khác trong bước lập kế hoạch.

```text
frontend/src/
  app/                 router, providers, config
  layouts/             PublicLayout, AdminLayout
  components/ui/       Button, Input, Select, Dialog, Drawer, Badge, Table
  components/brand/    Header, Footer, SectionHeading, Picture, ValueItem
  features/content/    homepage, story, contact, FAQ
  features/catalog/    list, detail, ProductCard, QuantityInput
  features/cart/       cartStore, CartDrawer, local persistence
  features/checkout/   form, Review, Received, submit coordinator
  features/locations/  public gym cards
  features/admin/      auth, orders, inventory, gyms, counts, reports
  api/                 generated types/client + shared error adapter
  styles/              tokens, typography, global styles
  assets/              optimized brand images and fonts
```

Một lớp API chung, không fetch rải rác trong component trang trí. Dữ liệu server và UI state tách nhau: catalog/admin từ server; drawer/tab/filter từ UI; cart lưu local; auth theo hợp đồng backend. Không thêm Redux hoặc cache server riêng nếu chưa có nhu cầu; chọn thư viện query/form phù hợp khi setup, tránh nhiều giải pháp chồng chức năng.

Component tối thiểu: ProductCard, ProductPicture, QuantityInput, Money, ErrorState, EmptyState, LoadingSkeleton, StatusBadge, ConfirmAction, FormField, DataTable/MobileList, AuditTimeline. Không dùng raw input cho mọi field mà thiếu label và lỗi; không nhúng các rule kho vào QuantityInput.

## 10. Hợp đồng frontend/backend cần bàn giao

Đây là nhóm dữ liệu cần có, chưa phải tên endpoint hoặc DTO cuối. Không viết frontend gọi các route tự nghĩ rồi ép backend đổi theo.

| Nhóm | Đầu vào/đầu ra cần thống nhất |
| --- | --- |
| Public content | Nội dung đã publish, asset URL, alt, CTA, FAQ; không trả nội dung admin |
| Catalog | SKU ID ổn định, slug, tên, quy cách, giá VND, ảnh, trạng thái nhận yêu cầu; lọc/sort/pagination nếu cần |
| Public gyms | Tên, địa chỉ, liên hệ công khai, trạng thái hoạt động; không tồn/ghi chú nội bộ |
| Submit order | SKU/số lượng, người nhận, COD, idempotency; kết quả pending và mã tham chiếu, lỗi có cấu trúc |
| Auth admin | Cơ chế session/cookie hoặc token được backend chốt, quyền, hết phiên; không tự lưu token lâu dài vào local storage |
| Admin orders | Trạng thái và allowed actions, revision/concurrency token, số tiền và cập nhật từng nghiệp vụ |
| Inventory / gym | Lô, balance, snapshot kiểm đếm có thời điểm, biến động, nháp, phiên bản và xác minh |
| Reports | Định nghĩa chỉ số và phạm vi dữ liệu, quyền trường, xuất file |

- Tiền: quy ước đơn vị VND và serialization nhất quán; formatter dùng locale Việt, không dùng phép tính float tùy tiện cho tiền.
- Thời điểm: ISO có timezone/UTC theo contract, hiển thị Asia/Ho_Chi_Minh; ngày sản xuất/hạn dùng là dữ liệu ngày theo quy tắc kho, không đổi ngày do timezone.
- Chốt pagination, nullability, enums, mã lỗi, auth/CORS và CSRF khi dùng cookie trước tích hợp. Frontend không phải lớp bảo vệ quyền cuối.
- OpenAPI sinh TypeScript types/client theo công cụ được chốt; tài liệu bổ sung các rule không thể hiện hết trong schema.
- Fixture chỉ dùng phát triển/kiểm thử theo contract và gắn nhãn; production không fallback sang “đặt thành công” hoặc số liệu mẫu khi API lỗi.

## 11. Responsive, accessibility, SEO và hiệu năng

### 11.1. Responsive

Mốc kiểm tra: 360, 390, 430, 768, 1024, 1440, 1920px; đây là mục tiêu nghiệm thu của website mới, không phải kết quả đã kiểm tra trên mẫu.

- Mobile padding khoảng 16–20px; desktop 32–48px tới max container.
- Menu chuyển drawer khi không đủ chỗ, không chỉ dựa tên thiết bị. Grid card 1–2/2–3/3–4 cột theo độ đọc.
- Tiêu đề dùng clamp và chiều rộng giới hạn, không ép cỡ chữ desktop xuống mobile. Dấu Việt không bị cắt bởi line-height.
- Không scroll ngang toàn trang; bảng kho có vùng scroll riêng, cột quan trọng hoặc card thay thế trên mobile.
- Drawer có bàn phím mở vẫn thấy input và CTA; nút Zalo không đè CTA, bị ẩn khi checkout mở.

### 11.2. Accessibility

- Một H1 mỗi trang; HTML có landmark, thứ tự heading hợp lý; link/nút dùng đúng chức năng.
- Form có label, autocomplete đúng, input điện thoại hỗ trợ keyboard phù hợp; lỗi liên kết trường, focus lỗi đầu khi submit.
- Tương tác dùng được bàn phím; focus visible, không phụ thuộc hover; thao tác chạm mục tiêu khoảng 44px.
- Ảnh nội dung có alt thật, ảnh trang trí alt rỗng; thông báo thêm giỏ/lỗi có live region phù hợp.
- Kiểm tra tương phản, zoom 200%, reduced motion, drawer focus restore; không dùng màu là tín hiệu trạng thái duy nhất.

### 11.3. SEO và tải trang

- Chọn chiến lược render trước setup: public landing/giới thiệu cần HTML có nội dung, metadata và link có thể crawl khi tải trực tiếp. Đề xuất prerender/static generation cho trang marketing; catalog/detail cần phương án cập nhật/rebuild hoặc SSR phù hợp hosting và độ thay đổi. Admin là client-rendered.
- Không tự chuyển sang Next.js hoặc đổi backend. Nếu chọn Vite SPA thuần phải ghi rõ giới hạn SEO/preview link và kế hoạch prerender tương thích, không coi chỉ đổi document.title là hoàn tất SEO.
- Có title/description/OG/canonical/sitemap cho public, noindex cho admin; dữ liệu sản phẩm có cấu trúc chỉ dùng giá/trạng thái thật, không rating giả.
- URL refresh trực tiếp hoạt động; route không tồn tại có trạng thái/response phù hợp cấu hình hosting, không trả trang sản phẩm giả.
- Hero dùng picture/srcset, dimension/aspect-ratio, không lazy-load ảnh LCP; ảnh dưới fold lazy-load. Admin và module nặng không nằm trong public initial bundle.
- Mục tiêu tham khảo khi kiểm thử: LCP <=2.5s, CLS <=0.1; INP <=200ms cần đo tương tác/thực tế, không thể chứng minh chỉ bằng build. Ghi rõ thiết bị/mạng khi đo, không hứa điểm Lighthouse 100.
- Ngân sách khởi đầu đề xuất: ảnh hero mobile khoảng <=250KB, desktop <=450KB nếu giữ được chất lượng; font dùng ít weight và bộ dấu Việt đầy đủ. Nếu vượt cần đo và tối ưu, không nén tới mức mờ nhãn.

## 12. Các giai đoạn và điều kiện hoàn thành

Giữ chiến lược backend MVP trước frontend. Trong lúc làm backend có thể chuẩn bị ảnh, wireframe và contract; không triển khai React đầy đủ chỉ vì có kế hoạch này.

| Giai đoạn | Công việc | Bàn giao / điều kiện qua bước |
| --- | --- | --- |
| F0 — Chốt thiết kế | Chốt tên/màu/font, sitemap, wireframe desktop/mobile, manifest ảnh và các quyết định còn mở | Bản hero + một card + drawer được duyệt về bố cục; danh sách ảnh thật còn thiếu |
| F1 — Chuẩn bị assets | Chụp/tách nền, ghép hero hai bố cục, ảnh SKU/câu chuyện; tối ưu | File nguồn + bản web + alt/crop specs, không dùng ảnh tạm trong bản công bố |
| F2 — Nền frontend | Sau backend/contract đủ ổn định: React TS, router, token, public/admin layout, API client, auth, lỗi chung | Chạy/build được; route trực tiếp, quyền và cấu hình môi trường có hướng dẫn |
| F3 — Public marketing | Header, hero, sections, footer, điểm bán, giới thiệu/liên hệ, chiến lược SEO | Desktop/mobile khớp thiết kế; API nội dung hoặc nội dung đã thống nhất; không claim/dữ liệu giả |
| F4 — Catalog và COD | Danh mục, chi tiết, giỏ, form, review, gửi request thật và nhận pending | Một đơn xuất hiện ở admin; lỗi giá/hàng/network/retry không trùng và không mất giỏ |
| F5 — Admin cốt lõi | Đơn, sản phẩm, lô, kho, gym, giao/thu hồi, kiểm đếm/xác minh | Hoàn tất hành trình nghiệp vụ trên API thật và SQL Server; quyền được backend thực thi |
| F6 — Admin bổ trợ | Nội dung, tài khoản/nhật ký theo quyền, báo cáo cơ bản | Chỉ số đúng định nghĩa; không hiển thị tiền/hoa hồng gym hoặc lãi toàn hệ thống |
| F7 — QA và bàn giao | Browser/mobile, accessibility, mạng chậm, SEO/build, luồng lỗi, tài liệu | Checklist có kết quả và giới hạn; cấu hình deploy/hướng dẫn vận hành, chưa tự publish trong tác vụ lập kế hoạch |

Không gán ngày hoàn thành khi chưa có ảnh thật, API, môi trường và năng lực kiểm thử. Có thể chia mỗi giai đoạn thành commit nhỏ, review theo kết quả đầu ra thay vì khối lượng file.

## 13. Kịch bản kiểm thử frontend cần có

1. Hero ở mọi mốc: không đè chữ lên nhãn, không cắt sản phẩm, CTA nhìn thấy và bấm được.
2. Menu, tìm kiếm, deep link, back/forward, danh mục không kết quả và sản phẩm ngừng bán.
3. Giỏ refresh còn SKU/số lượng; giá cũ phải được cập nhật trước gửi; nhiều tab không xóa nhầm lượng mới.
4. Không tài khoản: gửi COD -> nhận pending -> admin gọi và xác nhận -> xuất/giao -> hoàn tất. Thu COD độc lập.
5. Double click, timeout sau server đã tạo đơn, retry cùng key, validation, 409, 429 và 5xx. Không hiển thị thành công khi chưa có kết quả server.
6. Đóng/mở drawer, quay lại sửa thông tin, bàn phím mobile, Escape/focus và zoom; không lộ PII qua URL hoặc browser storage lâu dài.
7. Admin hai tab cập nhật cùng đơn/phiếu: xử lý stale revision, không lặng lẽ ghi đè.
8. Gym: nút tạo kiểm đếm thủ công, nhập 0 vs chưa nhập, mốc cũ và giao sau mốc, xác minh chưa xong không bị tính bán tự động.
9. Operator không được xem/sửa giá vốn/tài khoản; file export không lộ trường; hết session không làm mất nội dung phiếu âm thầm.
10. Refresh URL public/admin, lỗi ảnh, API unavailable, SEO HTML/OG, không có mock data hoặc debug credentials trong production.

Kiểm thử tự động tập trung luồng giỏ/COD và hành động admin có tác động, không viết test chỉ để kiểm tra mọi class CSS. Visual QA bằng ảnh chụp desktop/mobile của website mới; chưa có kết quả kiểm thử vì lần này chỉ lập kế hoạch.

## 14. Checklist bàn giao cho Antigravity

- Đọc ba tài liệu nghiệp vụ + kế hoạch này; không lấy tính năng của A Sỉn làm yêu cầu mặc định.
- Nhận file thiết kế desktop/mobile, assets và copy đã duyệt. Không có ảnh thật phải báo rõ placeholder; chưa đánh dấu hoàn thành visual.
- Nhận OpenAPI cuối, base URL local/staging, cách chạy backend, dữ liệu seed và tài khoản nội bộ dùng thử ngoài tài liệu public.
- Xác nhận render/hosting, thư viện UI/form/query và cơ chế auth; không lưu bí mật vào frontend environment public.
- Làm theo F2–F7 sau backend; báo rõ màn hình đã nối API và màn hình còn mock; kiểm chứng bằng browser.
- Bàn giao source React, assets tối ưu, hướng dẫn chạy/build, kết quả kiểm thử và các giới hạn còn lại.

## 15. Đầu vào còn thiếu và quyết định tiếp theo

| Đầu vào | Ảnh hưởng | Có thể làm trước khi có không? |
| --- | --- | --- |
| Tên thương hiệu, logo, bao bì | Header, màu, hero và card | Có thể wireframe, chưa chốt visual cuối |
| Ảnh bao bì + món ăn thật | Chất lượng giống mẫu | Có thể làm bố cục bằng khung ảnh, chưa gọi là banner hoàn thiện |
| SKU/giá/nội dung thật và thông tin bảo quản | Catalog, copy, FAQ | Có thể đặc tả schema, không công bố số liệu tự tạo |
| Điểm bán và thông tin liên hệ thật | Địa chỉ, Zalo, CTA | Có thể làm component; không dùng số/địa chỉ mẫu như thật |
| OpenAPI/auth/hosting | Client, quyền, SEO/render | Thiết kế được; triển khai tích hợp phải có contract |
| Thời điểm giữ hàng và thu COD tối thiểu | Admin/state/actions | Đã chốt giữ khi xác nhận, một kho chính, thu đủ tiền hàng ngày 26/10/2026; chi tiết thao tác theo thiết kế dữ liệu |

Hành động kế tiếp cho phần giao diện: dựng wireframe hero + trang chủ + một sản phẩm + drawer ở desktop/mobile, đồng thời chuẩn bị ảnh thật. Việc này không thay thế bước thiết kế ERD/backend đã lên kế hoạch và không yêu cầu sinh frontend trước backend.
