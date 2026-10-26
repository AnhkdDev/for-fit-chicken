# 01 — Products: thông tin sản phẩm

Ngày 26/10/2026. Thiết kế chi tiết để thảo luận; chưa có migration SQL Server. Tham chiếu [ERD tổng thể](../database-design.md) và [DBML](../database-schema.dbml).

## 1. Ranh giới đã thống nhất và đề xuất mới

Người dùng đã đồng ý giữ hàng khi xác nhận, một kho chính, thu đủ tiền hàng COD độc lập giao hàng. Những quyết định đó không tự phê duyệt mọi chi tiết Products dưới đây. Trường/state/validation trong tài liệu này là đề xuất cơ sở để review từng phần.

## 2. Một Product đại diện cho gì?

Đề xuất mỗi hương vị có câu chuyện/thành phần riêng là một Product, ví dụ “Ức gà tiêu đen”, “Ức gà thảo mộc”. Mỗi cách đóng gói là ProductVariant/SKU, ví dụ gói 150g và gói 200g. Tên/vị/quy cách chỉ là dữ liệu minh họa, chưa phải danh mục thật của nhóm.

```
Products: Ức gà tiêu đen
  ProductVariants: gói 150g, gói 200g
    ProductBatches: các đợt thành phẩm của từng SKU
```

Không cần thêm bảng Flavors trong MVP khi chưa có nhu cầu quản lý hương vị độc lập. Nếu nội dung/thành phần theo vị khác nhau, không gộp mọi vị thành một Product rồi để frontend đoán dữ liệu.

## 3. Cấu trúc đề xuất

| Cột | SQL Server | Bắt buộc ở DB? | Ý nghĩa |
| --- | --- | --- | --- |
| Id | bigint IDENTITY PK | Có | Khóa nội bộ ổn định, không dùng tên làm khóa |
| Name | nvarchar(200) | Có | Tên sản phẩm, ví dụ Ức gà tiêu đen |
| Slug | varchar(200) UNIQUE | Có | Đường dẫn ổn định, ví dụ uc-ga-tieu-den |
| ShortDescription | nvarchar(600) | Không | Mô tả ngắn ở danh mục và đầu trang chi tiết |
| Description | nvarchar(max) | Không | Câu chuyện và giới thiệu dài |
| Ingredients | nvarchar(max) | Không | Thành phần và thông tin liên quan do nhóm cung cấp |
| NutritionText | nvarchar(max) | Không | Nội dung dinh dưỡng có căn cứ, gồm đơn vị/cơ sở tính rõ; thiếu thì không hiện |
| UsageInstructions | nvarchar(max) | Không | Cách dùng; không chứa quy trình kỹ thuật chưa được xác định |
| StorageInstructions | nvarchar(max) | Không | Hướng dẫn bảo quản chung; không thay hạn dùng của từng lô |
| IsActive | bit DEFAULT 1 | Có | Có được tiếp tục kinh doanh sản phẩm hay không |
| IsPublished | bit DEFAULT 0 | Có | Nội dung đã được phép hiển thị công khai hay chưa |
| IsFeatured | bit DEFAULT 0 | Có | Được chọn vào khu vực sản phẩm nổi bật |
| SortOrder | int DEFAULT 0 | Có | Thứ tự mặc định; sort phụ bằng Id để ổn định |
| FirstPublishedAtUtc | datetime2(3) | Không | Mốc publish đầu để bảo vệ slug; unpublish không xóa mốc |
| CreatedAtUtc | datetime2(3) | Có | Thời điểm tạo UTC |
| UpdatedAtUtc | datetime2(3) | Có | Thời điểm thay đổi gần nhất UTC |
| Version | rowversion | Có | Phát hiện hai người sửa cùng bản cũ; không phải timestamp |

Mới tạo bản nháp chỉ cần Name và Slug, trạng thái chưa công bố; các trường nội dung hoàn thiện trước publish. Thông tin nhạy cảm như giá vốn không thuộc bảng này. Actor được lưu qua audit thay vì lặp mọi người sửa trong tất cả cột.

NutritionText đơn giản giúp không ép các con số chưa có. Nếu sau này cần so sánh/lọc protein, calo thì thiết kế cấu trúc Nutrition riêng; không parse số từ văn bản để tính tự động. Mọi khác biệt dinh dưỡng theo quy cách phải ghi rõ cơ sở tính (ví dụ theo 100g), không giả định hai gói có tổng giống nhau.

## 4. Những dữ liệu không đặt tại Products

| Không đặt cột | Nơi đúng | Lý do |
| --- | --- | --- |
| Price | ProductVariants.SellingPrice | Giá phụ thuộc quy cách |
| Weight / PackageSize | ProductVariants | Một sản phẩm có thể có nhiều quy cách |
| StockQuantity | WarehouseBalances + lô | Không thể dùng một tổng chung cho kho và gym |
| ManufacturedOn / Expiry | ProductBatches | Các lô có ngày/hạn khác nhau |
| UnitCost | ProductBatches và snapshot xuất | Không phải thông tin marketing |
| Image1, Image2, Image3 | ProductImages -> MediaAssets | Một-nhiều, thứ tự và ảnh theo SKU |
| Protein mặc định 0 | Chưa nhập thì null/ẩn | Chưa biết không có nghĩa bằng 0 |

Không lưu luôn giá thấp nhất/tổng tồn ở Products trong MVP; query/projection lấy đúng dữ liệu hiện hành. Không gắn banner toàn website vào mỗi Product; hero thuộc SiteContent.

## 5. Hiển thị, ngừng bán và hết hàng là ba việc khác nhau

| Trạng thái | Active | Published | Kết quả |
| --- | --- | --- | --- |
| Nháp | 1 | 0 | Nội bộ chỉnh sửa, chưa xuất hiện công khai |
| Đang công bố | 1 | 1 | Hiện công khai; nhận yêu cầu còn phụ thuộc SKU và hàng hợp lệ |
| Tạm ẩn | 1 | 0 | Gỡ khỏi public, vẫn xem lịch sử nội bộ |
| Ngừng kinh doanh | 0 | 0 | Không nhận mới/xác nhận đơn chờ có sản phẩm này; giữ toàn bộ lịch sử |

Đề xuất invariant: Published => Active; thao tác ngừng kinh doanh đồng thời đặt Published=0. Featured không phải trạng thái công bố; bật Featured trên draft không làm hiện ra public.

Hết hàng không tự đặt Active=0 hoặc Published=0: có thể giữ trang giới thiệu và hiển thị tạm hết hàng. Lượng bán được lấy từ kho chính và lô hợp lệ, không từ gym. Chính sách chặn gửi yêu cầu khi hết hàng sẽ cụ thể hóa ở phần đơn; khi xác nhận luôn kiểm tra lại.

Public chỉ trả Product active+published và variant active; không cho người biết ID truy cập bản nháp qua detail/search. Unpublish không tự hủy đơn đã tồn tại. Ngừng kinh doanh chặn yêu cầu mới/confirm pending; đơn đã confirm không bị hủy tự động. Chặn xuất vì an toàn dùng khóa lô và quy trình kho, không nhầm với nút ẩn nội dung marketing.

## 6. Tạo/sửa và điều kiện publish đề xuất

1. Owner tạo bản nháp, tên trim 2–200 ký tự, slug hợp lệ.
2. Nhập mô tả, thông tin sử dụng/bảo quản; tạo ít nhất một SKU active và giá hợp lệ.
3. Gắn ít nhất một ảnh thực có thể hiển thị cho sản phẩm/SKU active; lưu alt và thứ tự.
4. Khi publish: backend kiểm tra Active, mô tả ngắn, thành phần, cách dùng/bảo quản, SKU và ảnh. Không bắt buộc có tồn để xuất bản trang giới thiệu.
5. Dinh dưỡng chỉ publish nếu có nội dung được nhóm xác nhận; không tạo số liệu hoặc đánh giá cho đủ giao diện.
6. Thay đổi tên/ảnh/nội dung làm thay đổi catalog hiện hành, không ghi đè snapshot đơn cũ.

Đề xuất publish có validation ở service vì SQL không tự CHECK được “có ít nhất một SKU và ảnh hợp lệ”. Khi ngừng SKU cuối hoặc bỏ ảnh cuối cũng phải kiểm tra invariant hoặc chủ động unpublish trong cùng nghiệp vụ; không để API xóa ảnh phá điều kiện công bố.

MVP chưa có duyệt nhiều cấp hoặc draft/published content riêng cho từng Product: sửa sản phẩm đã publish có hiệu lực ngay. Admin phải ghi rõ điều này, có preview và revision check. SiteContent toàn trang có luồng publish riêng theo ERD, không mặc định Product cũng có versioned CMS.

## 7. Slug và nội dung

- Slug chữ thường ASCII không dấu, số và gạch nối; dài tối đa 200; không khoảng trắng hoặc segment rỗng. Sinh gợi ý từ tên nhưng backend kiểm tra unique, không chỉ frontend.
- Tên không unique cứng: có thể cảnh báo trùng, còn SKU có mã định danh riêng. Slug unique kể cả sản phẩm ngừng kinh doanh để không tái sử dụng URL gây nhầm lịch sử.
- Đề xuất không cho đổi slug sau lần publish đầu trong MVP; FirstPublishedAtUtc đã được bổ sung vào DBML để thực thi quy tắc này. Set lần đầu khi publish, không reset khi ẩn/ngừng kinh doanh.
- Description/Ingredients/Usage/Storage nhận text hoặc Markdown theo contract; không nhận HTML/script tùy ý. Khi hiển thị rich text phải sanitize/giới hạn cú pháp.
- Public DTO không trả audit, cost, trường nội bộ; không dùng EF entity trực tiếp làm response.

## 8. Ảnh và quan hệ

```
Products 1 -- N ProductVariants
Products 1 -- N ProductImages N -- 1 MediaAssets
ProductImages N -- 0..1 ProductVariants
```

ProductImages.VariantId null = ảnh chung; có ID = ảnh của một quy cách. FK composite bảo đảm SKU thuộc đúng Product. Cover chọn ảnh áp dụng theo SortOrder,Id; không thêm IsCover gây nhiều cờ true. Thứ tự ưu tiên ảnh riêng SKU rồi fallback ảnh chung phải được chốt tại phần ProductImages; không xóa file MediaAssets còn được tham chiếu.

## 9. Quyền và concurrency

- Theo ma trận đề xuất: Owner tạo/sửa/công bố/ngừng sản phẩm; Operator xem thông tin vận hành, không đổi giá hoặc nội dung.
- API update nhận Version của bản đã đọc; nếu stale, trả conflict để tải lại, không tự ghi đè thay đổi của người khác.
- Các thay đổi child ảnh/SKU có thể ảnh hưởng điều kiện publish phải khóa/check revision aggregate Products trong cùng transaction, không chỉ kiểm tra revision riêng child.
- Lưu actor, thời gian, action và trường đổi trong AuditLogs. Không hard-delete Product đã được dùng; giai đoạn MVP đề xuất không có nút xóa cứng ngay cả draft để đơn giản hóa.

## 10. Ví dụ dữ liệu minh họa

```
Id: 101
Name: Ức gà tiêu đen
Slug: uc-ga-tieu-den
ShortDescription: Ức gà nấu chậm vị tiêu đen, đóng gói tiện dùng.
IsActive: true
IsPublished: false
IsFeatured: true
SortOrder: 10
FirstPublishedAtUtc: null
```

Đây vẫn là bản nháp, không xuất hiện ngoài website dù Featured=true. Sau khi có SKU/ảnh/nội dung và publish thành công mới xuất hiện. Chưa cần nhập lô mới tạo được danh mục.

## 11. Tiêu chí kiểm chứng khi code

- Hai request cùng slug chỉ một bản được tạo; duplicate key được chuyển thành lỗi nghiệp vụ.
- Product nháp không xuất hiện qua list, detail, search hoặc featured public.
- Không publish khi thiếu nội dung/ảnh/SKU theo quy tắc đã chọn; xóa/ngừng child không làm vỡ invariant.
- Sửa bản cũ báo conflict; audit có người sửa; child mutation bảo vệ aggregate revision.
- Đổi tên không đổi snapshot đơn cũ; hết hàng không tự ẩn sản phẩm; ngừng kinh doanh không xóa lịch sử.
- Slug sau lần publish đầu không đổi, unpublish không reset FirstPublishedAtUtc.
- Không được phát sinh stock/price trực tiếp từ Products; dữ liệu thiếu không được hiển thị thành giá trị 0 giả.

Chưa chạy các test này; đây là tiêu chí cho bước triển khai. Phần tiếp theo: ProductVariants, mã SKU và quy cách/giá bán.
