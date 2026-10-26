# Thiết kế database MVP — bản ERD v0.1

Thiết kế ngày 26/10/2026, cập nhật triển khai 27/10/2026. Entity, EF configurations và migration InitialSchema đã triển khai; database development hiện dùng SQL Server `KDADEPTRAI\KDADEPTRAI` theo yêu cầu thay LocalDB. Các use case nghiệp vụ còn cần triển khai. Xem [phạm vi đã kiểm chứng](database/implementation.md). Căn cứ: [nghiệp vụ](mvp-requirements.md), [màn hình/luồng](mvp-screens-and-flows.md). Sơ đồ: [Mermaid](erd.mmd). Từ điển trường: [DBML](database-schema.dbml).

## 1. Những quyết định cơ sở

| Quyết định | Cách thiết kế | Trạng thái |
| --- | --- | --- |
| Khách không có tài khoản | Thông tin nhận hàng snapshot trong Orders; không Customers/CustomerUsers | Đã chốt |
| COD, gọi xác nhận | Đơn PendingConfirmation; không payment gateway | Đã chốt |
| Báo đơn mới | Telegram nhóm nội bộ; NotificationJobs bền vững và worker gửi/retry là thiết kế đề xuất | Người dùng đã chọn Telegram ngày 26/10/2026 |
| Giỏ | Local trình duyệt, không Cart/CartItems phía server | Đề xuất tối giản |
| Giữ tồn | Chỉ khi nhân viên xác nhận, ghi phân bổ theo lô | Người dùng đồng ý ngày 26/10/2026 |
| Kho phục vụ online | Một kho chính trong MVP; mô hình cho thêm kho về sau | Người dùng đồng ý ngày 26/10/2026 |
| Thu COD | Ghi nhận tiền hàng đã về nhóm, tách hoàn tất giao; ban đầu thu đủ tiền hàng | Người dùng đồng ý ngày 26/10/2026; chi tiết thao tác còn được thiết kế |
| Kho chính | Balance có transaction, ledger chứng từ bất biến sau chốt | Thiết kế đề xuất |
| Gym | Snapshot kiểm đếm + biến động sau mốc, không balance được gọi là thời gian thực | Đã chốt bản chất nghiệp vụ |
| Hàng hỏng còn tại gym | Vẫn thuộc số vật lý, chuyển điều kiện Good -> Damaged; thu hồi sau đó là di chuyển, không hủy lần nữa | Làm rõ mô hình |
| Tài khoản nội bộ | ASP.NET Core Identity, hai role Owner/Operator ban đầu | Đề xuất |
| Tiền và hoa hồng gym | Không có bảng giao dịch ngân hàng/hoa hồng/đối soát tiền | Ngoài MVP |
| Phí giao | Không có cột ShippingFee mặc định 0; tiền đơn hiện là tiền hàng | Đã chốt chưa tính trong app |
| Sản xuất | Nhập lô thành phẩm, không nguyên liệu/công thức | Đã chốt phạm vi |

Tổng: 22 bảng ứng dụng cốt lõi (bổ sung NotificationJobs) + 1 bảng chi phí tùy chọn; các bảng Identity do framework quản lý riêng. Nhiều bảng là các dòng chi tiết cần cho quan hệ một-nhiều, không phải từng đó module hoặc microservice.

## 2. Quy ước SQL Server

- Khóa ứng dụng: bigint IDENTITY, FK cùng kiểu. Mã nghiệp vụ và slug có unique index riêng. Id nội bộ không phải bí mật/khóa tra cứu đơn public.
- Identity dùng IdentityUser<long>/IdentityRole<long>; các bảng ứng dụng tham chiếu AspNetUsers.Id. Có thể đổi sang GUID trước migration nếu có lý do, không trộn hai loại tùy tiện.
- Tiếng Việt dùng nvarchar; mã/enum dùng varchar giới hạn và check tập giá trị khi tạo migration.
- Số lượng gói dùng int dương; balance int không âm. Sản phẩm bán theo gói nguyên, chưa bán theo khối lượng lẻ.
- Giá bán/tiền thu/chi: decimal(18,0) VND. Giá vốn đơn vị có thể decimal(18,4) do phân bổ chi phí; dùng decimal C#, quy tắc làm tròn khi tổng hợp phải thống nhất.
- Date cho ngày sản xuất; đề xuất ExpiresAtUtc datetime2(3) để có mốc hết hạn rõ. Nếu nhãn chỉ ghi ngày, cần chốt quy tắc chuyển sang mốc giờ theo Việt Nam trước khi nhập, không mặc định dùng được tới cuối ngày.
- Các thời điểm khác datetime2(3) UTC; application phải truyền UTC, giao diện hiển thị Asia/Ho_Chi_Minh. Không dùng rowversion như thời gian.
- CreatedAtUtc, CreatedByUserId khi thao tác nội bộ; UpdatedAtUtc và rowversion ở aggregate có thể sửa. Một số trường audit chung được rút gọn trên sơ đồ để dễ đọc, DBML là danh sách trường bản nháp.
- Chứng từ/đơn/lịch sử không cascade delete. Danh mục ngừng hoạt động bằng IsActive/IsPublished; tài khoản khóa thay vì xóa. Xóa draft cần có chính sách, mặc định hủy draft.
- Chưa dùng nhiều schema SQL hoặc event sourcing; transaction nghiệp vụ và bảng lịch sử đủ cho MVP.

## 3. Nhóm danh mục và hình ảnh — 5 bảng

| Bảng | Nội dung | Ràng buộc chính |
| --- | --- | --- |
| MediaAssets | StorageKey, MIME, kích thước, alt mặc định, người tải | StorageKey unique; lưu key/url ảnh, không nhét ảnh base64 vào bảng |
| Products | Tên dòng sản phẩm, slug, mô tả, thành phần, cách dùng/bảo quản, công bố | Slug unique; nội dung dinh dưỡng tùy chọn, không mặc định bằng 0 |
| ProductVariants | SKU cụ thể: ProductId, SkuCode, tên quy cách, khối lượng, giá, thứ tự | Theo bản chi tiết 02: giá >0, active phải có khối lượng; SkuCode unique toàn hệ thống, Label unique trong Product |
| ProductImages | ProductId, VariantId tùy chọn, AssetId, alt, thứ tự | Variant nếu có phải thuộc cùng Product; thứ tự nhất quán |
| ProductBatches | VariantId, BatchCode, sản xuất, hết hạn, giá vốn, khóa bán | BatchCode unique trong hệ thống; hết hạn sau sản xuất; giá vốn null = chưa biết, không thay bằng 0 |

Ví dụ Product = “Ức gà tiêu đen”; hai Variant = gói 150g và 200g; mỗi Variant có nhiều Batch. Nếu ban đầu chỉ có một quy cách, vẫn một Variant cho mỗi Product để kho/đơn không phải đổi khóa khi thêm quy cách.

Batch không phải lượng nhập: cùng Batch có thể nhập thành phẩm nhiều đợt bằng nhiều phiếu. Không lưu QuantityProduced rồi coi đó là tồn.

## 4. Gym và kho — 5 bảng

| Bảng | Nội dung | Ràng buộc chính |
| --- | --- | --- |
| Gyms | Mã, tên, địa chỉ, liên hệ công khai và ghi chú nội bộ | GymCode unique; công bố tách hoạt động |
| StockLocations | Mã địa điểm, loại Warehouse/Gym, GymId tùy chọn | Một location cho mỗi gym MVP; loại Gym bắt buộc GymId, Warehouse không có GymId |
| WarehouseBalances | LocationId, BatchId, Condition, OnHand, Reserved | Chỉ location Warehouse; unique location/batch/condition; 0 <= Reserved <= OnHand |
| StockDocuments | Phiếu nhập/chuyển/thu hồi/hỏng/hủy/điều chỉnh/xuất đơn; thời điểm, trạng thái, người chốt | DocumentNo unique, OperationKey unique; chốt một lần, chốt rồi không sửa dòng |
| StockDocumentLines | BatchId, Quantity, nguồn/đích và điều kiện hàng | Quantity > 0; ít nhất nguồn hoặc đích; đủ trường condition khi có location |

Condition: Good, Quarantine, Damaged. Hết hạn là điều kiện động theo Batch.ExpiresAtUtc, không tự thay mọi balance thành Damaged. Chỉ Good có thể Reserved; Good chưa chắc được bán nếu lô hết hạn/khóa.

Mỗi dòng là một chuyển động từ nguồn -> đích; null chỉ biểu thị ngoài phạm vi tồn. StockDocumentLines của phiếu Posted là ledger, chưa cần thêm InventoryTransactions trùng dữ liệu.

| Loại phiếu | Ví dụ dòng | Tác động |
| --- | --- | --- |
| ProductionReceipt | null -> Kho/Good | Tăng tồn kho chính |
| GymDelivery | Kho/Good -> Gym/Good | Giảm kho chính, ghi giao ký gửi |
| GymRecall | Gym/Good hoặc Damaged -> Kho/Quarantine | Nhận về chờ kiểm tra |
| Reclassify | Cùng gym hoặc kho: Good -> Damaged | Chuyển tình trạng, tổng vật lý không giảm |
| Reclassify | Kho/Quarantine -> Kho/Good hoặc Damaged | Kết quả kiểm tra hàng thu hồi |
| WriteOff | Kho/Gym Damaged -> null | Thực tế đã loại bỏ, giảm vật lý đúng một lần |
| Adjustment | null -> Kho hoặc Kho -> null | Chỉnh lệch kiểm kê có lý do/Owner; không dùng thay luồng gym count |
| OrderIssue | Kho/Good -> null, gắn OrderId | Xuất đúng một lần khi bàn giao đơn |

MVP không có vận chuyển hàng đang đi giữa kho/gym: xác nhận phiếu khi bàn giao thực tế. Draft không đổi tồn. Lệnh chuyển trừ nguồn/tăng đích cùng transaction với chốt phiếu.

**Gym không có WarehouseBalances.** Giao/thu hồi/hỏng ghi lịch sử; lượng hiển thị là snapshot gần nhất, các biến động sau đó hiển thị riêng. Nếu tính số sổ sách từ snapshot + biến động phải ghi rõ chưa kiểm đếm, không bán online từ số này. Khi thu hồi, số thực tế được người tới gym xác nhận; lệch với sổ sách phải lưu ngoại lệ/xác minh, không âm thầm sửa snapshot hay chặn bằng giả định “stock realtime”.

## 5. Đơn COD — 5 bảng

| Bảng | Nội dung | Ràng buộc chính |
| --- | --- | --- |
| Orders | Mã đơn, nguồn, trạng thái, người nhận/điện thoại/địa chỉ snapshot, tiền hàng, ghi chú, mốc xử lý | OrderNumber unique; PaymentMethod=COD; không FK customer |
| OrderItems | VariantId, SKU/tên/quy cách snapshot, Qty, UnitPrice | Qty>0, giá>=0; subtotal tính từ dòng, không nhận giá tin cậy từ browser |
| OrderAllocations | OrderItemId, WarehouseBalanceId, Qty, Reserved/Issued/Released, giá vốn snapshot khi xuất | Đúng SKU, location Warehouse, Condition Good; không phân bổ vượt lượng dòng |
| OrderEvents | Đổi trạng thái, ghi chú gọi/chỉnh đơn, actor, thời gian | Append-only, hỗ trợ truy vết thay đổi nội dung có giới hạn |
| CodReceipts | OrderId, tiền hàng đã nhận, thời điểm/người ghi, tham chiếu, void bởi Owner nếu sai | Đề xuất một phiếu thu hiệu lực/đơn; đủ tiền hàng, không thu một phần |

Orders.ItemsSubtotal có thể lưu tổng snapshot; phải bằng SUM(Qty*UnitPrice) của Items trong transaction. OrderItems.LineTotal đề xuất computed persisted trong migration; DBML chỉ chú thích. Không có TotalPaid cập nhật tay bên cạnh receipt; trạng thái thu được derive từ receipt hiệu lực.

Đơn mới PendingConfirmation, chưa allocation. Khi confirm, phân bổ theo hạn dùng và tạo Reserved. Khi bàn giao, Reserved -> Issued, balance đồng thời giảm OnHand/Reserved, tạo OrderIssue; khi hủy trước xuất, Reserved -> Released và giảm Reserved. Completed không ghi thêm xuất.

Mỗi item có thể chia nhiều lô. Sau giải phóng có thể phân bổ lại bằng dòng mới; không ghi đè lịch sử cũ. Partial confirm/partial shipment ngoài MVP. Phiếu xuất chỉ một Posted/OrderId. Một hệ thống không được vừa gọi “xuất kho đơn” vừa “bàn giao đơn” trừ hai lần.

Khóa lô sau giữ hoặc hết hạn trước xuất phải chặn xuất, phân bổ lại đủ lô hợp lệ trong transaction hoặc để nhân viên xử lý. Giá bán trong OrderItems không đổi vì giá hiện tại thay đổi. Giá vốn snapshot không bị sửa theo Batch sau khi đã xuất; chưa biết giá vốn thì báo dữ liệu chưa đủ.

Người dùng đã đồng ý ghi nhận thu đủ tiền hàng COD và tách khỏi hoàn tất giao. Chi tiết đề xuất: một phiếu thu hiệu lực, chỉ ở HandedOver/Completed, không tự thu khi hoàn tất. Ghi sai: Owner void kèm lý do và tạo phiếu thay thế, không xóa/sửa số tiền lịch sử. Phí giao ngoài hệ thống không được cộng lẫn vào receipt này.

## 6. Kiểm đếm gym — 3 bảng

| Bảng | Nội dung | Ràng buộc chính |
| --- | --- | --- |
| GymStockCounts | GymId, PreviousCountId, CountedAtUtc, trạng thái, người đếm/chốt, mốc nguồn | Mỗi gym chỉ một nhánh lịch sử đã chốt, thời gian tăng; có thể có draft stale phải refresh |
| GymStockCountLines | CountId, BatchId, ExpectedGood/Quarantine/Damaged, ActualGood/Quarantine/Damaged | Unique count/batch; actual null trong draft = chưa nhập, 0 = thực đếm bằng 0; chốt cần đầy đủ |
| GymVarianceResolutions | CountLineId, Missing/Surplus, Sale/Loss/Other, Qty, lý do, actor và void nếu ghi sai | Chỉ ghi phân loại sau xác minh, không ghi ledger kho lần nữa |

Gym thực tế thường Good/Damaged; Quarantine giữ để phản ánh hàng không bán đang còn tại điểm. Không đưa Reserved vào gym count.

Đối chiếu theo toàn bộ vật lý từng batch:

```
ExpectedTotal = PreviousActualTotal + ExternalInbound - ExternalOutbound
ActualTotal   = ActualGood + ActualQuarantine + ActualDamaged
Missing       = max(ExpectedTotal - ActualTotal, 0)
Surplus       = max(ActualTotal - ExpectedTotal, 0)
```

Hỏng còn tại gym là chuyển Good -> Damaged, tổng không đổi; WriteOff/Recall mới là outbound. Vì vậy không trừ “hỏng đã ghi” thêm lần nữa khi ActualTotal còn bao gồm hàng hỏng. Đây là cách cụ thể hóa công thức nghiệp vụ trước đó bằng các tập số lượng không trùng.

- Expected từng condition tính bằng delta của dòng Posted; nhập count từ snapshot Good/Damaged và các movement sau mốc. Biến động đổi condition có thể làm expected condition âm do chưa ghi các lần bán; báo lệch để xác minh, không coi expected là lượng thực còn.
- Count phải bao gồm tất cả batch còn trên snapshot trước, batch có biến động trong kỳ và batch thực thấy; không bỏ batch hết hàng vì không nhập dòng. Có thể đếm thêm batch ngoài dự kiến.
- Lần đầu gym: nếu đã có lịch sử giao đầy đủ, đầu kỳ 0 và tính movement từ lúc mở điểm; nếu đưa dữ liệu từ ngoài vào phải có baseline count được Owner xác nhận, không tự coi mọi hàng đang có là surplus hoặc nhập 0.
- Chốt snapshot cập nhật mốc đã đếm, không cập nhật WarehouseBalances và không tạo “bán hàng” giả. Lần sau dùng Actual mới dù kỳ trước chưa xác minh hết.
- Resolution Sale chỉ dùng Missing. Surplus không được phân loại Sale. Tổng resolution hiệu lực theo một phía không vượt Missing/Surplus; phần còn lại = unresolved, không cần ghi dòng Unknown lặp.
- Xác minh kỳ cũ chỉ phân loại. Ví dụ đã đếm từ 20 xuống 10: sau đó xác nhận bán 8, thiếu 2; không trừ thêm 8 khỏi snapshot 10.
- Reclassify sau count có thể làm thay đổi sổ condition mà không đổi vật lý; mất mát đã phản ánh trong count không ghi thêm writeoff ngoại trừ thật sự còn hàng và loại bỏ ở mốc sau. Phải tham chiếu kỳ đã phát hiện khi cần tránh tính hai lần.

**Ranh giới thời gian:** CountedAt là thời điểm thực tế. Khi post count, serialise thao tác theo StockLocation của gym và kiểm tra tất cả phiếu nguồn trong khoảng. Mốc có thể lưu CutoffDocumentId nhưng IDENTITY không đại diện thứ tự commit; không dùng nó một mình để khẳng định đầy đủ. Không cho post/backdate movement có EffectiveAt <= latest CountedAt đã chốt. Nếu nhập muộn cần quy trình sửa mốc/recount của Owner có lịch sử; MVP đề xuất yêu cầu ghi đủ phiếu trước chốt và không tự sửa kỳ đã chốt. Count nháp được làm lại nếu nguồn đổi. Mốc mới dùng thời điểm lớn hơn mốc cũ; cần quy tắc rõ cho chứng từ bằng timestamp.

## 7. Hạ tầng dữ liệu và nội dung — 3 cốt lõi + 1 tùy chọn

| Bảng | Vai trò | Giới hạn |
| --- | --- | --- |
| SiteContent | SectionKey, schema version, draft JSON, published JSON, mốc/người publish | Chỉ key cố định HomeHero/Story/Usage/Faq/Contact/SiteSettings; JSON theo DTO schema; public chỉ đọc published |
| AuditLogs | Actor, action, entity type/id, thời gian, correlation, changed-fields JSON đã lọc | Chỉ bảng audit dùng tham chiếu entity logic; không dùng generic entity FK cho kho/đơn; không log secret/password/PII dư |
| IdempotencyRequests | ScopeHash, operation, key, request hash, OrderId khi submit, kết quả an toàn và thời gian hết hạn | Unique scope/operation/key; public result không chứa toàn bộ PII; lưu bền cùng transaction tạo đơn |
| OperatingExpenses (tùy chọn) | Ngày, loại, số tiền, ghi chú, actor/void | Chưa đưa vào migration cốt lõi nếu nhóm chưa cần nhập chi phí; không suy lợi nhuận đầy đủ |

SiteContent JSON chứa text và asset ID/key theo schema; vì JSON không có FK tự động, publish phải validate asset tồn tại, được phép công khai và không cho xóa asset đang được tham chiếu. Sau này nội dung phức tạp mới tách quan hệ, không thêm Blog/CMS bây giờ.

ASP.NET Identity quản lý user/role/user-role cùng claims/login/token theo cấu hình framework; không tự chế PasswordHash hoặc phân quyền bằng cột RoleName trong Orders. Thêm DisplayName, IsActive và các trường quản trị cần thiết vào ApplicationUser. Role không có liên hệ tài khoản gym.

## 8. Ràng buộc/index đề xuất

| Bảng | Unique / check / index đáng chú ý |
| --- | --- |
| Products/Variants/Batches | Slug, SkuCode, BatchCode; index Batches(VariantId,ExpiresAtUtc) để tìm lô |
| ProductImages | FK composite (ProductId,VariantId) -> Variants(ProductId,Id) khi có VariantId; tránh ảnh trỏ variant sản phẩm khác |
| Locations | LocationCode unique; filtered unique GymId IS NOT NULL; check kind/gym tương ứng |
| WarehouseBalances | Unique(LocationId,BatchId,Condition); OnHand>=0, Reserved>=0, Reserved<=OnHand, Condition<>Good => Reserved=0 |
| StockDocuments | OperationKey unique; index Status,EffectiveAtUtc; filtered unique OrderId khi Posted OrderIssue; OrderId chỉ dùng với OrderIssue |
| DocumentLines | FK nguồn/đích và batch; số lượng>0; không cho nguồn=đích và condition giống hệt; index source/batch và destination/batch |
| Orders/Items | OrderNumber unique; index Status/CreatedAt; index normalized phone nếu thực cần tìm; Qty>0, UnitPrice>=0 |
| Allocations | Index Item/State và Balance/State; unique active Item/Balance; batch/SKU khớp kiểm tra trong transaction |
| CodReceipts | Filtered unique OrderId WHERE VoidedAtUtc IS NULL; Amount>0; bằng tiền hàng order theo service |
| GymCounts | Unique GymId/CountedAt cho Posted; filtered unique PreviousCountId cho Posted không null; một root Posted mỗi Gym; unique draft không bắt buộc |
| CountLines/Resolutions | Unique Count/Batch; index Line/VoidedAt; qty>0; Actual đầy đủ khi header Posted kiểm tra service |
| IdempotencyRequests | Unique ScopeHash/Operation/Key; request hash cùng key phải giống; index ExpiresAt để dọn theo chính sách |

Giá trị code/enums mô tả bằng varchar trong DBML, migrations phải tạo CHECK cụ thể. Mọi FK mặc định NO ACTION. Rule liên bảng như location type, SKU allocation, tổng dòng, transitions, tổng resolution, điều kiện công bố không được bảo đảm chỉ bằng CHECK; phải thực hiện và test tại application transaction (hoặc SQL procedure nếu chọn), không tuyên bố schema tự bảo vệ tất cả.

## 9. Transaction/concurrency bắt buộc

1. **Gửi COD:** xác minh SKU/giá và dữ liệu -> insert unique idempotency + Orders + Items + Event -> commit. Replay cùng key trả mã/kết quả cũ, khác payload báo conflict. Scope public cần cookie ngẫu nhiên/opaque request scope được thiết kế cùng API, không khóa chỉ theo IP; không trả PII khi người khác biết key. Không retry tạo đơn mới bằng key mới khi kết quả request cũ chưa rõ.
2. **Confirm:** khóa/check order revision + trạng thái; đọc và cập nhật balance có điều kiện OnHand-Reserved>=Qty và batch còn hạn/không khóa; tạo allocations; update order/event; commit. Nhiều item phải đủ tất cả hoặc rollback.
3. **Ship:** khóa order, batch/balance/allocation; kiểm tra trạng thái và hạn lần nữa; đổi reserved->issued, trừ OnHand và Reserved, post duy nhất OrderIssue, event; commit. Nếu lỗi một dòng rollback tất cả.
4. **Cancel trước xuất:** lock/check state, giải phóng tất cả reserved chưa issue một lần, event; không cộng OnHand.
5. **Post phiếu kho:** lock địa điểm/batch/balance theo thứ tự ổn định; trừ nguồn/tăng đích/chốt phiếu cùng transaction. Gym dùng khóa location để kiểm tra mốc count; balance warehouse không vượt lượng chưa giữ.
6. **Post count:** lock gym location, check previous/latest và movement trong khoảng; chốt đầy đủ lines, chuyển header Posted; không writeoff/sale tự động. Concurrent post count/giao/thu hồi phải cùng protocol khóa.
7. **Resolve:** khóa count line, cộng allocations phân loại hiệu lực rồi kiểm tra giới hạn; insert resolution; không đổi tồn hiện tại.
8. **Thu COD:** lock order, kiểm tra status/amount, unique receipt hiệu lực; commit cùng audit. Void có lý do/quyền, được replay an toàn.

Khóa ở đây là khóa transaction của SQL Server trên dòng/aggregate, không C# lock, không Redis. Ví dụ dùng transaction short + cập nhật có điều kiện hoặc query có khóa phù hợp, đồng bộ một protocol trong service. Với balance mới chưa tồn tại phải bảo vệ insert bằng unique key và retry transaction có giới hạn. FEFO xử lý batch theo (ExpiresAtUtc,Id), cập nhật nhiều balance theo thứ tự nhất quán; deadlock retry hữu hạn cho toàn transaction, không gọi bên ngoài hoặc đợi điện thoại trong transaction.

Rowversion cho sửa form/order aggregate giúp phát hiện stale write, nhưng không thay thế điều kiện tồn/tổng multi-row. Batch khóa bán cần cùng protocol lock/revision lúc confirm/ship để tránh thay trạng thái giữa kiểm tra và commit. Lưu tổng Reserved trong balance là projection có transaction từ allocations; cần query reconciliation phát hiện lệch, không cho admin tự sửa số này.

## 10. Ví dụ xuyên suốt

1. Nhập 100 gói tiêu đen của Batch B1 vào Warehouse/Good: OnHand=100, Reserved=0.
2. Giao 30 sang gym: kho còn 70, gym có phiếu giao 30; chưa có số đếm hiện tại cho gym.
3. Khách gửi yêu cầu 5: kho vẫn 70/0, order pending.
4. Nhóm gọi xác nhận: kho 70/5, allocation reserved 5.
5. Bàn giao: kho 65/0, allocation issued, một phiếu xuất 5.
6. Gym kiểm đếm còn Good=18, Damaged=2, tổng 20; nếu không outbound khác, 10 gói cần giải thích. Hai gói hỏng đang còn không bị trừ thêm khỏi tổng.
7. Xác minh 8 đã bán, 2 chưa rõ: resolution Sale=8, unresolved=2; snapshot vẫn 20 vật lý tại mốc.
8. Thu hồi 2 hỏng: Gym/Damaged -> Warehouse/Quarantine, kho có 65 Good +2 Quarantine, website vẫn chỉ lấy Good hợp lệ. Không ghi thêm mất 2 lần nữa.

## 11. Nghiệm thu database khi triển khai

- Migration trên SQL Server thật: PK/FK/check/filtered unique đúng; không chỉ test EF InMemory.
- Hai confirm cạnh tranh lượng cuối: một transaction thắng trong giới hạn, tổng reserved và allocations khớp.
- Ship/cancel bị gửi lặp: một hiệu ứng; FK không có chứng từ mồ côi; unique stock issue và receipt hiệu lực.
- Hai người phân loại cùng discrepancy không vượt missing; giải thích kỳ cũ không giảm hàng kỳ mới.
- Count đồng thời giao/thu hồi và late effective movement: không bỏ sót hoặc tính hai lần. Batch omitted không được mặc định zero mà không xác nhận.
- Reclassify hỏng rồi recall/writeoff không nhân đôi giảm; reserved không được hủy/chuyển lén.
- Đổi giá, tên, cost không sửa snapshot lịch sử. Không biết cost khác với 0.
- Asset publish, quyền cost/PII, idempotency replay không lộ dữ liệu, FK NO ACTION được kiểm chứng.

Đã kiểm thử migration và một phần ràng buộc schema trên SQL Server thật; các kiểm thử transaction nghiệp vụ nhiều dòng trong danh sách vẫn cần thực hiện khi xây API. Xem implementation.md để phân biệt phạm vi đã kiểm chứng.

## 12. Kết quả vòng review và cách đi sâu

Ngày 26/10/2026 người dùng đồng ý cả ba điểm: giữ hàng khi xác nhận, một kho chính MVP, ghi nhận thu đủ tiền hàng COD độc lập giao hàng. Đây không phải phê duyệt tự động mọi trường hoặc quy tắc chi tiết của toàn bộ ERD.

Đi sâu từng phần: [01 — Sản phẩm](database/01-products.md), [02 — SKU, quy cách và giá](database/02-product-variants.md), [03 — Lô thành phẩm](database/03-product-batches.md), tiếp đến kho, đơn hàng và gym. Các bản chi tiết giữ bối cảnh thiết kế ban đầu; trạng thái migration hiện tại được ghi trong implementation.md.

Đã phân tích kho, chứng từ và đơn trong chat; các tệp chi tiết 04–06 chưa được lưu đầy đủ. Tệp mới: [07 — Phân bổ giữ hàng](database/07-order-allocations.md), [Thông báo Telegram](database/order-notifications.md). Không áp dụng lại thao tác ghi tệp 04 đã bị từ chối.

Chưa cần chốt font/ảnh hoặc phí giao để review ERD. Sau review: hoàn thiện từ điển/migrations SQL Server bằng EF Core, rồi đặc tả API; không tạo database production trong bước này.

## 13. Tài liệu kỹ thuật tham chiếu

- [EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency): concurrency token/rowversion và xử lý xung đột.
- [ASP.NET Core Identity model](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model): dùng model Identity và EF migrations thay vì tự xây lưu mật khẩu.
