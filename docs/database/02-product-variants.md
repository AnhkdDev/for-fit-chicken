# 02 — ProductVariants: SKU, quy cách và giá bán

Ngày 26/10/2026. Tiếp theo [01 — Products](01-products.md). Đây là đề xuất thiết kế chi tiết; chưa chạy migration. Tham chiếu [ERD tổng](../database-design.md) và [DBML](../database-schema.dbml).

## 1. Một SKU đại diện cho gì?

Một SKU là một mặt hàng có thể đặt mua và theo dõi lô riêng. Với mô hình hiện tại, mỗi Product là một vị, còn Variant là một quy cách đóng gói của vị đó.

| Product | SKU | Nhãn quy cách | Khối lượng tịnh thành phẩm | Giá minh họa |
| --- | --- | --- | --- | --- |
| Ức gà tiêu đen | UG-TD-150 | Gói 150g | 150g | 35.000đ |
| Ức gà tiêu đen | UG-TD-200 | Gói 200g | 200g | 45.000đ |
| Ức gà thảo mộc | UG-TM-150 | Gói 150g | 150g | 35.000đ |

Toàn bộ tên, mã, quy cách và giá trên là ví dụ cấu trúc, không phải mức giá kinh doanh được đề xuất hoặc danh mục người dùng đã chốt. Một Product có một Variant vẫn dùng cùng mô hình; không thêm nhánh xử lý sản phẩm đơn giản/phức tạp.

Giỏ gửi VariantId và số gói; không chỉ ProductId. Số lượng 2 của SKU 150g là hai gói 150g, không phải một gói 300g. Lô thành phẩm, dòng đơn và phân bổ kho đều đi theo VariantId.

## 2. Các cột

| Cột | SQL Server | Bắt buộc | Ý nghĩa/rule đề xuất |
| --- | --- | --- | --- |
| Id | bigint IDENTITY PK | Có | Khóa nội bộ ổn định |
| ProductId | bigint FK Products | Có | Thuộc đúng một sản phẩm; không chuyển sang Product khác sau tạo |
| SkuCode | varchar(50) UNIQUE | Có | Mã gọn cho quản trị, phiếu và tìm hàng |
| Label | nvarchar(150) | Có | Quy cách khách nhìn thấy, ví dụ Gói 150g |
| NetWeightGrams | int | Có khi SKU hoạt động | Khối lượng tịnh thành phẩm của một gói; không phải khối lượng thịt sống trước nấu |
| SellingPrice | decimal(18,0) | Có | Giá bán hiện hành cho một gói, VND, >0 trong MVP |
| IsActive | bit DEFAULT 1 | Có | Có tiếp tục nhận mới/xác nhận đơn cho SKU này không |
| SortOrder | int DEFAULT 0 | Có | Thứ tự lựa chọn; sort phụ Id |
| CreatedAtUtc | datetime2(3) | Có | Tạo UTC |
| UpdatedAtUtc | datetime2(3) | Có | Sửa UTC |
| Version | rowversion | Có | Phát hiện sửa stale |

NetWeightGrams giữ nullable ở DB để sửa bản chưa hoạt động; đề xuất check giá trị dương khi có và CHECK IsActive=0 OR NetWeightGrams IS NOT NULL. Variant active yêu cầu đầy đủ số liệu. SellingPrice >0 ở DB, chưa có luồng quà tặng 0đ; nếu chưa biết giá thì chưa tạo SKU bán, Product vẫn có thể ở nháp.

Trong MVP đơn vị bán là một gói, không cần UnitOfMeasure hay quy đổi kg/lít tổng quát. Nhãn có thể mô tả bao bì nhưng không dùng Label để tính trọng lượng; tính bằng NetWeightGrams và Quantity. Nội dung dinh dưỡng ở Product phải nêu cơ sở tính, không tự hiểu tổng protein của mọi gói là bằng nhau.

## 3. Mã SKU và định danh

- Đề xuất format `[A-Z0-9-]`, từ 3–50 ký tự, trim và chuẩn hóa uppercase trước kiểm tra/lưu. Ví dụ UG-TD-150 chỉ là quy ước dễ đọc, không bắt buộc suy vị từ chuỗi.
- Unique toàn hệ thống, kể cả SKU ngừng hoạt động; không tái dùng mã cũ cho sản phẩm khác. Unique index thực thi ở DB, không chỉ check trước insert.
- SkuCode đề xuất cố định sau tạo. Id/FK vẫn là liên kết thật; không parse mã để điều khiển nghiệp vụ.
- ProductId cố định; sửa tên Product không đổi mã SKU hoặc quan hệ lô/đơn.
- Label đề xuất unique trong cùng Product theo collation case-insensitive đã chọn. Chuẩn hóa khoảng trắng để tránh hai lựa chọn “Gói 150g” nhìn giống nhau. Không unique NetWeightGrams: hai bao bì cùng trọng lượng vẫn có thể khác SKU khi cần, phải có Label rõ khác nhau.
- Barcode chưa cần trong MVP. SkuCode không mặc nhiên là barcode chuẩn và không phải mã lô.

## 4. Giá bán và đơn đã tồn tại

SellingPrice là giá catalog hiện hành theo SKU, không phải giá vốn hoặc bảng giá lịch sử. Ban đầu chưa có giá theo khách, chi nhánh hoặc thời gian, không thêm PriceLists/Promotions.

| Tình huống | Cách xử lý |
| --- | --- |
| Giá đổi khi khách chỉ đang xem/giữ giỏ | Nạp giá hiện hành; cho khách xem lại trước khi gửi |
| Khách gửi đơn với thông tin giá cũ | Backend đối chiếu giá kỳ vọng theo contract; nếu đổi thì trả xung đột, không âm thầm tạo đơn với tổng khác |
| Đơn pending đã tạo rồi giá catalog đổi | Giữ UnitPrice snapshot trên OrderItems, không tự cập nhật lúc gọi xác nhận |
| Nhóm cần đổi giá/số lượng đơn pending | Chỉnh có chủ đích sau trao đổi với khách, tính lại tiền hàng và audit; không lấy cớ catalog đổi để sửa ngầm |
| Đơn đã confirmed hoặc hoàn tất | Đổi catalog không tác động đơn; thay đổi đơn theo quy trình riêng trước xuất, không sửa tùy ý |

Server tính tiền từ dữ liệu được phép áp dụng; frontend không được tự quyết UnitPrice. Khi tạo đơn, đọc giá/revision và ghi snapshot cùng một transaction/consistent revision; concurrent update phải dẫn đến một kết quả giá rõ ràng, không pha trộn các lần đọc của cùng SKU. Không cần khóa giữ trong lúc khách xem trang hoặc đợi gọi.

Giá minh họa 35.000 lên 38.000: đơn cũ có snapshot 35.000 giữ nguyên; đơn mới chỉ dùng 38.000 sau khi khách đã nhìn thấy/xác nhận lại nếu cần. Audit lưu ai đổi giá và giá cũ/mới. Chưa cần bảng lịch sử giá chuyên biệt; audit không được coi là công cụ suy giá thực bán tại gym.

## 5. Hết hàng, ngừng SKU, ẩn Product

- IsActive=1 không có nghĩa đang còn hàng. Khả năng bán lấy từ Good warehouse balances của các lô đủ điều kiện, trừ reserved, không lấy từ gym.
- IsActive=0 chặn yêu cầu mới và xác nhận pending cho SKU; không xóa lô/tồn/đơn cũ và không tự giải phóng đơn đã confirmed.
- Các allocation đã giữ có thể tiếp tục xử lý theo đơn được xác nhận; rủi ro chất lượng phải dùng batch sale block và kiểm tra trước xuất, không chỉ cờ ngừng SKU catalog.
- Nếu ngừng SKU active cuối của Product đã public: đề xuất backend báo rõ và yêu cầu Owner đồng ý gỡ công bố cùng thao tác, trong một transaction. Không cho API ngừng cuối làm Product public vi phạm điều kiện có SKU active.
- Product chưa public thì không cho public API nhận SKU chỉ vì đoán được Id. Đơn nội bộ đã có phải theo dõi độc lập, không tự hủy khi gỡ nội dung marketing.
- Ngừng/re-activate không thay đổi danh tính SKU hoặc snapshot. Không bật lại để tái sử dụng mã cho một quy cách khác.

## 6. Những trường được sửa và không được đổi danh tính

| Trường | Quy tắc đề xuất |
| --- | --- |
| SellingPrice | Owner đổi, audit và Version; ảnh hưởng catalog mới, không sửa lịch sử |
| Label | Sửa lỗi trình bày được, không biến thành quy cách khác; đơn cũ giữ snapshot |
| SortOrder | Đổi được, không đổi nghiệp vụ |
| IsActive | Đổi có kiểm tra Product và đơn pending |
| SkuCode, ProductId | Không đổi sau tạo trong MVP |
| NetWeightGrams | Sửa trước khi có batch hoặc dòng đơn tham chiếu; khi đã dùng thì tạo SKU mới |

Đổi gói thật từ 150g sang 180g phải tạo SKU mới, không sửa NetWeightGrams của SKU đã có lô. Nếu công thức thay đổi đáng kể nhưng cùng tên/khối lượng, cần quyết định có tạo Product/SKU mới để không gán nội dung mới cho hàng cũ; không giải quyết bằng sửa Label. Versioning công thức chưa thuộc MVP.

Kiểm tra “chưa có batch/order” và thay NetWeight phải đồng bộ với transaction tạo batch/order cùng khóa/revision SKU; nếu chỉ SELECT exists rồi UPDATE không khóa thì hai thao tác vẫn có thể đua. Quan hệ và unique constraint không tự bảo vệ quy tắc đổi danh tính.

## 7. SKU, lô, ảnh và nhóm hàng

```
Products 1 -- N ProductVariants
ProductVariants 1 -- N ProductBatches
ProductVariants 1 -- N OrderItems
ProductVariants 1 -- N ProductImages (tùy chọn)
```

- Một SKU có nhiều lô, một lô chỉ thuộc một SKU. Khi nhập lô chọn SKU, không nhập lại vị/quy cách bằng text tự do.
- Giá vốn không đặt tại Variant vì khác lô; số tồn không đặt ở Variant vì khác địa điểm/tình trạng.
- Chọn ảnh riêng SKU trước, nếu không có thì dùng ảnh chung Product; tất cả theo SortOrder,Id. Quy tắc này là đề xuất xử lý ảnh, không thêm cột ImageUrl lặp trên Variant.
- Mua 5 gói: Quantity=5 của SKU, không tạo SKU “5 gói” chỉ vì khách mua nhiều.
- Combo nhiều vị hoặc bán một thùng bao gồm nhiều gói cần quan hệ thành phần/quy đổi kho riêng; chưa làm trong MVP. Không tạo bundle SKU độc lập rồi trừ một gói sai hoặc bỏ trừ các SKU thành phần.

## 8. Admin và ràng buộc

1. Owner mở Product -> thêm quy cách -> nhập code/label/khối lượng/giá/thứ tự -> lưu.
2. Operator xem SKU và giá cần để xử lý đơn, không đổi giá hoặc chuyển Product.
3. ProductVariants tạo FK ProductId, unique SkuCode, unique(ProductId,Label), index(ProductId,Id) unique để ProductImages composite FK hoạt động.
4. CHECK SellingPrice>0, weight>0 khi có và active phải có weight. SortOrder>=0 theo đề xuất, input không có phần thập phân cho grams.
5. Cập nhật Version stale -> conflict và tải lại; không tự merge hai thay đổi giá. Các thao tác ảnh hưởng Product publish dùng chung aggregate locking/revision với Products.
6. Lưu audit old/new trường thay đổi. Không có hard delete trong UI MVP; ngừng hoạt động thay xóa.

Unique Label cần collation rõ khi migration: so sánh tên tiếng Việt phù hợp và case-insensitive, không xóa dấu để làm unique. Giá tiền dùng decimal, không dùng float hoặc price dưới dạng chuỗi định dạng có dấu chấm.

## 9. Tiêu chí kiểm chứng khi code

- Hai SKU trùng code khác hoa/thường/space bị chặn sau chuẩn hóa, không chỉ cảnh báo frontend.
- Product khác được cùng Label; trong một Product không có hai Label giống nhau theo rule đã chọn.
- Không nhận giá âm/0, grams âm/0; active không thiếu grams; frontend không giả giá trống bằng 0.
- SKU đang active nhưng hết tồn không tự bị ngừng; không dùng stock gym để hiện khả dụng online.
- Đổi giá không sửa đơn cũ; giá đổi lúc submit yêu cầu xem lại, không tự thu/tạo tổng khác.
- Không đổi grams sau khi có batch hoặc order; thử đồng thời tạo tham chiếu và sửa grams để kiểm tra race.
- Ngừng SKU cuối cần xử lý gỡ công bố rõ; SKU ngừng không tự xóa batch/allocations/lịch sử.
- Một SKU nhiều lô vẫn cùng mặt hàng trên giỏ; số lượng được hiểu là số gói, không số gram.

Chưa chạy kiểm thử; chưa có migration. Phần tiếp theo là ProductBatches: ngày sản xuất, mốc hết hạn, giá vốn, khóa bán và quan hệ nhập thành phẩm.
