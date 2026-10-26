# 07 — OrderAllocations: giữ và xuất hàng theo lô

Ngày 26/10/2026. Căn cứ [ERD tổng](../database-design.md), [DBML](../database-schema.dbml) và các phần kho/chứng từ/đơn đã phân tích trong chat. Những phần chat đó chưa có đầy đủ tệp chi tiết riêng; tài liệu này không khẳng định đã lưu các tệp 04–06.

Thiết kế để review, chưa migration. Đã chốt: một kho chính, chỉ giữ sau khi nhóm gọi xác nhận. Telegram báo đơn mới đã được người dùng chốt; thông báo không tạo allocation hoặc xác nhận đơn.

## 1. Vai trò và quan hệ

Một dòng OrderItems là nhu cầu mua một SKU; OrderAllocations chỉ rõ nhu cầu đó được cấp từ những balance/lô nào. WarehouseBalances đã có BatchId và location/condition nên allocation không cần lặp BatchId/SkuId.

```
Order -> OrderItem: 5 gói tiêu đen 150g
            -> Allocation A: giữ 3 từ lô A / kho chính / Good
            -> Allocation B: giữ 2 từ lô B / kho chính / Good
```

SKU phải giống nhau, không thay gói 150g bằng 200g, không thay vị. Một item có thể nhiều allocation. Không gán mỗi đơn đúng một BatchId vì đơn có nhiều SKU và mỗi SKU có thể nhiều lô.

## 2. Cột dữ liệu

| Cột | Kiểu SQL Server | Ý nghĩa |
| --- | --- | --- |
| Id | bigint IDENTITY PK | Mã dòng phân bổ |
| OrderItemId | bigint FK OrderItems | Nhu cầu cần đáp ứng |
| BalanceId | bigint FK WarehouseBalances | Nguồn hàng cụ thể |
| Quantity | int >0 | Số gói được giữ/cấp của dòng này |
| State | varchar(20) | Reserved / Issued / Released |
| UnitCostSnapshot | decimal(18,4), nullable | Giá vốn tại lúc xuất, không phải giá bán |
| ReservedAtUtc | datetime2(3) | Mốc bắt đầu giữ |
| IssuedAtUtc | datetime2(3), nullable | Mốc đã xuất |
| ReleasedAtUtc | datetime2(3), nullable | Mốc trả lượng giữ về khả dụng |
| ReleaseReason | nvarchar(500), nullable | Lý do hủy/chuyển phân bổ |

Không cần Version riêng ở allocation trong bản tối giản nếu mọi mutation buộc qua transaction order aggregate, update có điều kiện State và khóa hàng liên quan; Order.Version vẫn bảo vệ form. Không mở CRUD allocation độc lập. Người thực hiện và lý do nghiệp vụ lưu OrderEvents/AuditLogs, không suy từ ReservedAt.

## 3. Trạng thái và hiệu ứng

| Chuyển | Balance.OnHand | Balance.Reserved | Ý nghĩa |
| --- | --- | --- | --- |
| Tạo Reserved | Không đổi | Tăng Quantity | Đã cam kết cho đơn |
| Reserved -> Issued | Giảm Quantity | Giảm Quantity | Hàng rời kho theo phiếu xuất |
| Reserved -> Released | Không đổi | Giảm Quantity | Không dùng phân bổ này nữa |

Issued và Released là trạng thái kết thúc. Không Released -> Reserved bằng cách sửa ngược dòng cũ; muốn giữ lại tạo dòng mới. Không Issued -> Released để giả lập trả hàng.

CHECK proposal: Reserved không có IssuedAt/ReleasedAt; Issued có IssuedAt và không ReleasedAt; Released có ReleasedAt/reason và không IssuedAt. Giá vốn chỉ chụp lúc issue, null trước đó. Mốc chuyển không trước ReservedAt.

Đề xuất mỗi allocation được xử lý nguyên dòng, không xuất một phần của dòng và không giao một phần đơn trong MVP. Reallocation có thể giải phóng dòng cũ rồi tạo các dòng thay thế phù hợp, vẫn đủ toàn item trong một transaction.

## 4. Khi nào tạo allocation?

1. Khách gửi yêu cầu -> PendingConfirmation, chưa có allocation.
2. Telegram nhận thông báo -> không tác động tồn.
3. Nhóm gọi khách, thống nhất thông tin ngoài transaction.
4. Nhân viên bấm xác nhận -> server kiểm tra trạng thái/revision, chọn lô, tạo Reserved, tăng balance.Reserved, đổi order Confirmed, ghi event trong một transaction.
5. Một SKU thiếu thì rollback toàn bộ, order vẫn pending, không tồn tại các dòng giữ dở dang.

Nếu confirm bị retry hoặc hai người confirm cùng một order, trạng thái/revision và transaction ngăn tạo lại. Nếu hai order tranh lượng cuối, cập nhật có điều kiện balance bảo vệ tổng. Không dùng C# lock thay cho bảo vệ ở SQL.

## 5. Chọn lô và giới hạn số lượng

FEFO: chọn lô hết hạn trước trong tập đủ điều kiện, tie-break bằng BatchId để ổn định. Nguồn chỉ kho chính/Good, location hoạt động, lô chưa hết hạn/khóa; SKU/Product cho phép xác nhận. Không lấy gym/Quarantine/Damaged.

```
Free = OnHand - Reserved
Take = min(Free, lượng item còn cần)
```

Lặp lô đủ điều kiện tới khi đủ item hoặc kết luận thiếu. Đọc giá trị Free không đủ bảo vệ; update có điều kiện và protocol khóa/retry phải bảo đảm Free vẫn đủ tại lúc ghi. Batch khóa/hạn phải kiểm tra đồng bộ, không chỉ lúc query đầu.

Invariant theo item:

- Pending/Cancelled: không có Reserved hiệu lực.
- Confirmed/Preparing: tổng Reserved bằng Quantity item; không có Issued.
- HandedOver/Completed: tổng Issued bằng Quantity item; không có Reserved.
- Released là lịch sử, không cộng vào lượng đáp ứng item.

Mỗi balance: Reserved bằng tổng allocations State=Reserved. Kiểm tra tổng cross-row tại service transaction, FK/CHECK đơn lẻ không bảo đảm được.

## 6. Xuất hàng và hủy

**Bàn giao:** khóa order + allocations/batch/balance theo thứ tự thống nhất; kiểm tra trạng thái, đủ phân bổ, lô còn hợp lệ. Tạo/chốt đúng một OrderIssue khớp allocation, giảm OnHand/Reserved, đổi allocations Issued, chụp cost hiện hành, đổi order HandedOver và event trong cùng transaction. Completed không xuất thêm.

**Hủy trước xuất:** pending không có gì giải phóng; confirmed/preparing đổi mọi Reserved thành Released có lý do, giảm Reserved và đổi order Cancelled trong cùng transaction. Không cộng OnHand. Không xóa allocation.

Nếu ship và cancel đua nhau, order lock/state khiến chỉ một hành động hợp lệ được commit. Không kiểm tra trạng thái trước rồi ghi sau trong các transaction rời nhau.

## 7. Chuyển phân bổ khi lô khóa/hết hạn

Đơn cần 5, đang giữ lô A; A bị khóa. Không xuất A dù balance còn hàng.

- Đề xuất có lệnh nội bộ phân bổ lại, không phải sửa SKU/số lượng đơn.
- Tìm đủ nguồn hợp lệ cùng SKU; giải phóng allocations cũ và giữ nguồn mới cùng transaction, ghi event lý do. Không đủ thì rollback giữ nguyên dữ liệu cũ và chặn ship, nhóm liên hệ khách/hủy trước xuất.
- Không có trạng thái “đã phân bổ lại được 3/5” coi như confirm đầy đủ.
- Cờ khóa/hết hạn chặn ship độc lập, không tự trừ số vật lý hay tự mất Reserved.
- Hàng hỏng ảnh hưởng lượng giữ cũng cần phân bổ lại/hủy phù hợp trước chốt movement làm giảm nguồn; không để Reserved>OnHand hoặc xóa giữ không giải thích.

MVP không tự tìm/gửi thông báo cho khách hoặc tự đổi vị. Có thể hiển thị cảnh báo trong admin để người phụ trách xử lý.

## 8. Index, constraints và quyền

- FK OrderItemId, BalanceId; CHECK Quantity>0, State/mốc phù hợp.
- Filtered unique(OrderItemId,BalanceId) WHERE State='Reserved': chỉ một dòng giữ hiệu lực cho cặp; dòng lịch sử Issued/Released vẫn giữ. Nếu cùng nguồn được chọn hai lần trong một lần confirm thì gộp trước insert.
- Index(OrderItemId,State), index(BalanceId,State) cho tổng theo item và reconciliation balance. Không unique OrderItemId vì một item có thể nhiều lô.
- Validate SKU trên item khớp SKU của batch qua balance, đúng kho/Good. FK hiện có không tự chứng minh cùng SKU.
- Operator xác nhận/bàn giao/hủy theo quyền order; không tự POST allocation với nguồn tùy ý qua API CRUD. Giá vốn snapshot chỉ trả DTO cho người được xem.
- Đề xuất mọi thay đổi phân bổ lock order aggregate và cập nhật Order.Version; không để reallocation đã đổi nguồn nhưng frontend vẫn giữ revision cũ như không có thay đổi.

## 9. Ví dụ đầy đủ

Lô A có 3 free, B có 4 free, cùng SKU. Item Quantity=5:

```
Confirm: A Reserved 3 + B Reserved 2
Ship:    A Issued   3 + B Issued   2
```

Nếu hủy thay vì ship: hai dòng đều Released, tăng free trở lại 3 và 2, không tăng OnHand. Nếu A bị khóa: có thể thay A bằng lô C đủ 3 trong một transaction; dòng A chuyển Released, C tạo Reserved 3, B vẫn Reserved 2, tổng hiệu lực vẫn 5. Không cộng dòng A Released vào tổng 8.

## 10. Tiêu chí khi triển khai

1. Nhiều item/nhiều lô thiếu một phần rollback mọi lượng giữ.
2. Confirm double-click không tạo thêm allocation; hai order không over-reserve.
3. Ship/cancel/reallocate cạnh tranh không tạo lượng âm hoặc trạng thái pha trộn.
4. FEFO bỏ qua lô khóa/hết hạn, không lấy hàng gym và không đổi SKU.
5. Ship tạo một phiếu khớp allocations, cost snapshot đúng/null rõ, completed không xuất lần hai.
6. Historical Released không tính còn giữ; invariant item/balance khớp sau retry.
7. Reallocation đổi revision/event, lỗi thì không làm mất lượng giữ cũ.

Chưa chạy kiểm thử. Phần nghiệp vụ tiếp theo: CodReceipts và OrderEvents — thu tiền hàng thực tế và truy vết xử lý. Telegram được đặc tả riêng ở [Thông báo đơn](order-notifications.md).
