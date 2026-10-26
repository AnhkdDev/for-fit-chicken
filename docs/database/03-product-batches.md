# 03 — ProductBatches: lô thành phẩm

Ngày 26/10/2026. Tiếp theo [02 — SKU](02-product-variants.md). Đề xuất thiết kế chi tiết; chưa tạo database/migration. Tham chiếu [ERD tổng](../database-design.md) và [DBML](../database-schema.dbml).

## 1. Một lô là gì?

Lô là nhóm thành phẩm cùng SKU, có cùng thông tin sản xuất/hạn dùng và được nhận diện để truy vết. Chỉ gộp khi thực tế nhóm coi đó là cùng một lô và các thông tin thống nhất; không gộp chỉ vì cùng SKU hoặc cùng ngày.

```
Product: Ức gà tiêu đen
  SKU: Gói 150g
    Batch A: thông tin sản xuất/hạn dùng của đợt A
    Batch B: thông tin sản xuất/hạn dùng của đợt B
```

Một Batch thuộc đúng một SKU, không dùng chung một BatchId cho gói 150g và gói 200g. Một mẻ thực tế có nhiều quy cách thì tạo các batch thành phẩm riêng theo SKU; có thể đặt mã cùng tiền tố. Chưa xây bảng mẻ sản xuất, nguyên liệu hoặc công thức trong MVP.

## 2. Lô khác phiếu nhập kho

ProductBatches lưu danh tính và thông tin của thành phẩm. Phiếu nhập/StockDocumentLines lưu bao nhiêu gói thực tế đi vào kho và khi nào.

Ví dụ số lượng minh họa:

1. Tạo Batch A của SKU tiêu đen 150g: chưa tăng tồn.
2. Chốt phiếu nhập 60 gói Batch A: tăng tồn 60.
3. Chốt phiếu nhập thêm 40 gói cùng Batch A: tăng thêm 40.
4. Giao 30 gói sang gym: kho chính còn 70 nếu chưa có biến động khác.

Không tạo Batch mới cho mỗi phiếu nhập nếu thực tế vẫn là cùng lô; không cộng lại tổng “sản lượng lô” sau mỗi phiếu. Không có các cột StockQuantity/RemainingQuantity/ProducedQuantity làm nguồn tồn trong ProductBatches.

## 3. Cột dữ liệu đề xuất

| Cột | SQL Server | Bắt buộc | Ý nghĩa |
| --- | --- | --- | --- |
| Id | bigint IDENTITY PK | Có | Định danh lô nội bộ |
| VariantId | bigint FK ProductVariants | Có | Một SKU cụ thể |
| BatchCode | varchar(60) UNIQUE | Có | Mã lô dùng trên phiếu, tra cứu và đối chiếu bao bì |
| ManufacturedOn | date | Có | Ngày sản xuất thực tế theo lịch Việt Nam |
| ExpiresAtUtc | datetime2(3) | Có | Mốc thời gian từ đó không được bán/xuất theo quy tắc đã xác định |
| UnitCost | decimal(18,4) | Không | Giá vốn quản trị cho một gói của lô; chưa biết là null |
| IsSaleBlocked | bit DEFAULT 0 | Có | Chặn bán/phân bổ/xuất bán dù chưa hết hạn |
| BlockReason | nvarchar(500) | Có khi bị khóa | Lý do khóa |
| BlockedAtUtc | datetime2(3) | Có khi bị khóa | Thời điểm khóa hiện hành |
| BlockedByUserId | bigint FK AspNetUsers | Có khi bị khóa | Người khóa hiện hành |
| Notes | nvarchar(1000) | Không | Ghi chú nội bộ phục vụ truy vết |
| CreatedAtUtc | datetime2(3) | Có | Thời điểm tạo bản ghi |
| CreatedByUserId | bigint FK AspNetUsers | Có | Người tạo |
| UpdatedAtUtc | datetime2(3) | Có | Thời điểm thay đổi gần nhất |
| Version | rowversion | Có | Phát hiện cập nhật stale và phối hợp khóa trạng thái |

Không thêm IsExpired/IsOutOfStock vì những giá trị này thay đổi theo thời gian và số liệu kho. Không thêm SalePrice: giá catalog thuộc SKU. Một số trường mới như mốc/người khóa và UpdatedAt đã được bổ sung vào DBML ở vòng thiết kế này.

## 4. Mã lô và ràng buộc SKU

- BatchCode unique toàn hệ thống, gồm chữ hoa/số/gạch nối theo quy ước nhóm; trim/uppercase trước lưu, giới hạn 60 ký tự.
- Ví dụ định dạng `UG-TD-150-20261026-A`; đây là mã minh họa, không bắt buộc parse ngày/khối lượng từ chuỗi.
- Có thể có nhiều lô cùng SKU cùng ngày, nên không unique(VariantId,ManufacturedOn).
- Không dùng lại mã cũ sau khi hết hàng/hết hạn. Id là khóa quan hệ; mã là định danh nghiệp vụ.
- VariantId và BatchCode đề xuất cố định sau tạo. Nếu nhập sai trước khi có phiếu, Owner có thể hủy dùng bản chưa nhập và tạo bản đúng; không tái gán batch đã dùng sang SKU khác.
- Khi tạo batch và khi nhập hàng, kiểm tra SKU phù hợp. Sản phẩm chưa public vẫn được nhập thành phẩm; công bố marketing không phải điều kiện có tồn kho.

## 5. Ngày sản xuất và hạn dùng

Hệ thống lưu hạn dùng đã được nhóm xác định từ quy trình sản phẩm, không tự quyết thực phẩm bảo quản được bao lâu. Tài liệu không đề xuất số ngày bảo quản hoặc nhiệt độ xử lý.

- ManufacturedOn nhập như ngày thực tế Việt Nam, không convert qua UTC làm lệch ngày.
- ExpiresAtUtc lưu một mốc UTC chính xác; giao diện nhập/hiển thị theo giờ Việt Nam.
- Quy tắc: tại thời điểm `now >= ExpiresAtUtc`, lô không đủ điều kiện bán hoặc xuất bán.
- Nếu nhãn chỉ ghi ngày, cần chốt ý nghĩa ngày đó và quy tắc chuyển thành mốc trước triển khai; không ngầm chọn 00:00 hoặc 23:59:59. App có thể yêu cầu người có quyền nhập rõ mốc hết hạn trước khi chốt lô.
- Validate mốc hết hạn sau ngày sản xuất theo quy tắc timezone đã chốt. Ngày tạo bản ghi khác ngày sản xuất.
- Không dùng CreatedAtUtc để suy ngày sản xuất; không tự kéo dài hạn khi nhập lại hàng thu hồi.
- Đề xuất MVP không tạo batch sản xuất trong tương lai vì không nhận đặt trước. Nhập dữ liệu lịch sử cho phép ngày sản xuất cũ; không vì đã hết hạn mà cấm ghi nhận tồn hỏng/thu hồi thực tế, chỉ cấm luồng nhập Good để bán.

Không cần cron/job đổi cờ hết hạn mới ngăn bán: query nghiệp vụ kiểm tra thời gian thực tại lúc phân bổ và xuất. Job cảnh báo chỉ phục vụ vận hành, không phải lớp bảo vệ duy nhất.

## 6. Giá vốn theo lô

- UnitCost là chi phí quản trị một gói, không phải tổng chi phí cả lô và không phải giá bán.
- Ví dụ minh họa: chi phí được nhóm phân bổ cho 100 gói là 2.000.000đ -> đơn giá vốn 20.000đ/gói. Cần thống nhất những khoản được đưa vào chi phí phân bổ; app chưa quản lý công thức/nguyên liệu để tự tính.
- UnitCost có thể null khi chưa xác định; null khác 0. Giá trị đã nhập phải >=0, trường hợp 0 cần có căn cứ/ghi chú, không default 0 cho nhanh.
- Đề xuất Owner được bổ sung/chỉnh UnitCost có audit để dùng cho các lần xuất tiếp theo; không tự tính lại chi phí các lần đã xuất.
- Khi xuất đơn, copy UnitCost tại thời điểm đó vào OrderAllocations.UnitCostSnapshot. Null lúc xuất giữ null trong snapshot, báo cáo ghi thiếu dữ liệu thay vì lấy giá vốn mới hiện tại để điền ngầm.
- Nếu có nhiều đợt nhập cùng batch mà chi phí khác, MVP vẫn một đơn giá quản trị hiện hành; cần nhóm chốt cách phân bổ. Không tự biến thành FIFO theo giá mua hoặc tự bình quân mà không có nghiệp vụ.
- Chưa có luồng hồi tố/tái tính giá vốn; cần số liệu lịch sử đầy đủ thì thiết kế quy trình bổ sung riêng. Không tuyên bố lợi nhuận chính xác khi snapshot hoặc chi phí còn thiếu.

Rule này làm rõ ghi chú cũ “khóa master sau dùng”: khóa thông tin danh tính/sản xuất, nhưng giá vốn được chỉnh cho tương lai có audit và snapshot bảo vệ lịch sử.

## 7. Khóa lô khác hết hạn, hỏng và ngừng SKU

| Trạng thái | Nguồn dữ liệu | Ý nghĩa |
| --- | --- | --- |
| Hết hạn | So sánh ExpiresAtUtc với thời điểm xử lý | Không bán/xuất bán, vẫn còn số vật lý nếu chưa hủy |
| Khóa bán | IsSaleBlocked | Không phân bổ/xuất bán dù chưa hết hạn |
| Hỏng một số gói | Điều kiện từng lượng ở kho/gym | Chuyển lượng đó Good -> Damaged, không nhất thiết khóa toàn lô |
| Hết hàng | Số liệu balance và allocations | Không còn lượng hợp lệ, không xóa batch |
| Ngừng SKU | ProductVariants.IsActive | Dừng kinh doanh SKU; không thay đánh giá chất lượng lô |

Khóa/hết hạn không tự xóa hoặc writeoff số lượng. Vẫn cho thao tác thu hồi, cách ly, kiểm đếm, hủy thực tế có chứng từ phù hợp; không ngăn người dùng ghi nhận hàng vật lý chỉ vì lô không được bán.

Đề xuất khóa lô chặn cả xác nhận đơn mới, xuất đơn, giao thêm Good sang gym và đưa Quarantine về Good có thể bán. Các hàng đã nằm ở gym cần nhóm chủ động thu hồi/trao đổi ngoài app; app không tự bảo đảm nhân viên gym ngừng bán hoặc tự gửi thông báo cho họ.

Quyền đề xuất: Operator và Owner được khóa khi phát hiện vấn đề, bắt buộc lý do; chỉ Owner mở khóa sau xác minh, audit đầy đủ. Mở khóa không làm lô hết hạn thành hợp lệ. Khi mở khóa có thể clear cột lý do/người/mốc khóa hiện hành, lịch sử giữ trong AuditLogs; không sửa audit cũ.

## 8. Lô bị khóa/hết hạn trong lúc đơn đã giữ hàng

Ví dụ: đơn đã giữ 5 gói lô A, sau đó lô A bị khóa.

1. Không cho bấm bàn giao để xuất lô A.
2. Không tự hủy đơn hoặc tự giải phóng giữ hàng mà không ghi nhận.
3. Nhân viên có thể phân bổ lại sang lô hợp lệ cùng SKU nếu đủ; giải phóng giữ cũ và giữ mới trong cùng transaction, lưu lịch sử.
4. Nếu không đủ, giữ đơn ở trạng thái cần xử lý, nhóm liên hệ khách; hủy trước xuất sẽ giải phóng đúng một lần.
5. Không tự thay SKU/vị/quy cách khác vì có hàng.

Các bước kiểm tra: khi nhận yêu cầu có thể báo khả dụng, khi confirm phải đủ lượng hợp lệ, khi chuẩn bị/xuất phải kiểm tra lại mốc và khóa. FEFO chọn lô hết hạn trước trong tập hợp đủ điều kiện; không phải cứ lô cũ nhất là được xuất.

Query FEFO đề xuất dùng index(VariantId,ExpiresAtUtc,Id) và lọc balances Good khả dụng ở kho chính. Không tạo filtered index kiểu ExpiresAt > GETUTCDATE để coi như index tự chạy theo thời gian; điều kiện thời gian nằm trong query.

## 9. Sửa lô, lịch sử và concurrency

| Trường | Đề xuất |
| --- | --- |
| VariantId / BatchCode | Cố định sau tạo, không đổi danh tính lô |
| ManufacturedOn / ExpiresAtUtc | Chỉnh lỗi khi chưa có phiếu Posted, có audit; sau dùng khóa sửa thông thường |
| UnitCost | Owner chỉnh cho xuất tương lai, không viết lại snapshot quá khứ |
| Notes | Sửa có audit |
| IsSaleBlocked và thông tin khóa | Qua hành động khóa/mở riêng có quyền/lý do |

Nếu phát hiện hạn dùng nhập sai sau đã nhập kho: khóa lô ngay, xác minh thực tế; chỉnh hồi tố là xử lý ngoại lệ cần thiết kế riêng, không có nút kéo dài hạn thông thường. Không tạo lô giả mới rồi chuyển hàng để lách hết hạn. Chính sách hiệu chỉnh ngày sau dùng chưa được coi là đã có trong MVP.

Update form dùng rowversion. Khóa/mở lô và confirm/ship/giao gym phải phối hợp transaction trên cùng batch row, không chỉ SELECT rồi xử lý trên dữ liệu cũ. Giao dịch đọc lại/khóa lô và balances theo thứ tự ổn định, ngắn, không chờ người dùng trong transaction.

Kiểm tra “chưa có phiếu Posted” trước sửa ngày phải đồng bộ với thao tác post phiếu và tạo allocation; tránh một người đổi hạn cùng lúc người khác đang xuất. Với identity/date, đề xuất chặn sửa cả khi có allocation Reserved/Issued dù chưa có phiếu xuất; rule phiếu Posted một mình chưa đủ.

## 10. Admin và public

- Admin chọn SKU, nhập mã lô/ngày/hạn/giá vốn theo quyền -> tạo batch -> tạo/chốt phiếu nhập riêng.
- Danh sách hiển thị SKU/quy cách, mã lô, ngày/hạn, trạng thái khóa/hết hạn, lượng ở kho lấy từ balances; giá vốn chỉ Owner.
- Phần gym hiển thị batch theo lần kiểm đếm gần nhất và thời điểm; không gom lượng gym thành số có thể bán online.
- Public không cần trả toàn bộ danh sách lô, giá vốn, ghi chú hoặc người khóa. Chỉ trả khả dụng catalog phù hợp; FEFO phân bổ do server quyết định.
- Trang chi tiết không đưa một hạn dùng của lô hiện tại vào mô tả chung như cam kết cho mọi đơn tương lai.

## 11. Tiêu chí kiểm chứng khi triển khai

1. BatchCode trùng bị chặn; cùng SKU/cùng ngày vẫn tạo được hai batch khác mã khi thực tế cần.
2. Tạo batch không tăng tồn; hai phiếu nhập cùng batch tăng đúng tổng đã chốt; submit phiếu lặp không tăng thêm.
3. Không xuất Good cho lô đã hết hạn/khóa; vẫn ghi thu hồi/cách ly/hủy phù hợp.
4. Khóa lô sau confirm chặn ship; phân bổ lại giữ và giải phóng đúng một transaction, không đổi SKU.
5. Hết hạn tại đúng mốc dùng điều kiện >=; kiểm thử UI chuyển UTC/giờ Việt Nam không lệch ngày.
6. Giá vốn null không thành 0; thay UnitCost không đổi snapshot đã xuất hoặc tự bù snapshot thiếu.
7. Chỉ Owner mở khóa/đổi cost; Operator không xem giá vốn qua API/export.
8. Sửa ngày đồng thời post/allocate không tạo lô đã dùng nhưng metadata bị sửa không kiểm soát.
9. Dữ liệu gym không được dùng để cam kết hàng online; expired batch không mất khỏi lịch sử.

Chưa thực hiện các kiểm thử; đây là tài liệu thiết kế. Điểm còn cần chốt thực tế trước migration/API: ý nghĩa hạn trên nhãn và mốc cutoff, chính sách giá vốn, quyền mở khóa. Phần tiếp theo: StockLocations và WarehouseBalances — một kho chính, điều kiện hàng và lượng giữ.
