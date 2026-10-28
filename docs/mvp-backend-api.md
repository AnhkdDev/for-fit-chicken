# Hợp đồng backend MVP và hướng dẫn frontend

Cập nhật 27/10/2026. Đây là phạm vi API đã triển khai trong source, không phải mô tả frontend đã xây.
Xem [tiến độ kiểm chứng](mvp-backend-progress.md), [bàn giao](HANDOFF.md), [OpenAPI máy đọc](openapi/mvp.json)
và [auth/cookie/CSRF](admin-auth-api.md). Swagger trực tiếp ở `/swagger` trong Development.

## Quy ước chung

- Frontend React/API cùng origin HTTPS qua proxy. Mọi POST/PUT/DELETE cần cookie CSRF và header
  `X-CSRF-TOKEN`, kể cả checkout công khai. Lấy token từ GET `/api/csrf`; sau login lấy lại token.
- Admin cần cookie HttpOnly và policy AdminStaff (Owner hoặc Operator). Các thao tác tài chính,
  publish nội dung, khởi tạo kho, giá vốn, void và notification monitor giới hạn Owner.
- Enum JSON là chuỗi tên C# (Good, Confirmed, ...), không gửi số. Tiền VND số nguyên decimal(18,0),
  giá vốn tối đa 4 số lẻ. Không dùng float để tính tiền. Giá/tổng đơn luôn do server lấy lại từ SKU.
- UTC date-time gửi ISO 8601 có `Z`. Ngày sản xuất `yyyy-MM-dd` theo ngày địa phương Việt Nam.
  Không tự suy ra giờ hết hạn từ nhãn chỉ ghi ngày; người nhập phải xác định mốc giờ đúng trước khi gửi UTC.
- `version` là base64 rowversion. Lấy từ GET/response mới nhất và gửi nguyên chuỗi khi sửa.
  409 VERSION_CONFLICT nghĩa là tải lại, không ghi đè. Nếu response chỉ trả dòng con, GET parent để lấy version mới.
- List dùng `page`/`size`; mặc định 1/20, size 1–100. Response `{items,total,pageNumber,pageSize}`.
- Không có API xóa lịch sử nghiệp vụ hoặc sửa trực tiếp balance/allocation. Tắt danh mục bằng isActive/isPublished.
- Admin/checkout trả no-store; không đưa recipient/password/cookie/bot token vào log/frontend analytics.

## Khởi tạo dữ liệu trên máy mới

1. Đọc HANDOFF, restore/build, áp dụng migration nếu database mới. Không dùng EnsureCreated cho DB ứng dụng.
2. Người dùng chạy script bootstrap Owner, login và lấy CSRF token mới.
3. POST `/api/admin/inventory/warehouse` (Owner) để tạo MAIN; gọi lại không tạo kho thứ hai.
4. Tạo Product bản nháp, tạo Variant, upload ảnh, gắn ảnh, GET parent lấy version rồi công bố Product.
5. Tạo Batch rồi nhập thành phẩm bằng ProductionReceipt. Chỉ batch không có số lượng không làm tăng tồn.
6. Đặt guest/manual order, gọi xác nhận ngoài app, confirm trên app để giữ hàng.
7. Tạo gym để tự có địa điểm ký gửi, giao bằng GymDelivery; Owner kiểm đếm mở đầu trước các kỳ Regular.

## Công khai và media

| Method / route | Chức năng |
| --- | --- |
| GET `/api/csrf` | Lấy CSRF token cho guest/admin |
| GET `/api/products` | Chỉ Product active/published, SKU active, ảnh và khả dụng kho MAIN |
| GET `/api/products/{id}` | Chi tiết sản phẩm được công bố |
| GET `/api/gyms` | Điểm bán công khai, không có liên hệ/ghi chú nội bộ hoặc tồn |
| GET `/api/content` | Chỉ bản PublishedJson, không bản nháp |
| GET `/api/media/{id}` | Ảnh công khai nếu được tham chiếu từ nội dung đã publish; admin có thể xem ảnh chưa công bố |
| POST `/api/media` | Admin upload multipart field `file`, PNG/JPEG/WebP tối đa 10MB, cần CSRF |

Media lưu ngoài DB/web root; key ngẫu nhiên, kiểm tra signature file và giới hạn byte; không nhận SVG/HTML.
Không có xử lý resize/crop hay kiểm định chất lượng ảnh. `Media:RootPath` cho phép chọn thư mục lưu bền.
Mặc định `<working-directory>/App_Data/media`, dotnet run dùng working directory API project; thư mục này được Git ignore.
Production cần volume bền/backup ảnh riêng. Không xóa ảnh vật lý nếu DB còn tham chiếu; hiện không mở API xóa asset.
Body nội dung được xem là plain text; frontend không chèn raw HTML bằng dangerouslySetInnerHTML.

## Catalog quản trị

| Method / route | Chức năng |
| --- | --- |
| GET/POST `/api/admin/products` | Danh sách / tạo Product nháp |
| GET/PUT `/api/admin/products/{id}` | Xem / sửa Product với version |
| POST `/api/admin/products/{id}/variants` | Tạo SKU |
| PUT `/api/admin/products/{id}/variants/{variantId}` | Sửa giá, nhãn, active, thứ tự với version SKU |
| POST `/api/admin/products/{id}/images` | `{version,image:{assetId,variantId,altText,sortOrder}}` |
| DELETE `/api/admin/products/{id}/images/{imageId}` | Body `{version}` của Product |

Product input: name, slug, shortDescription, description, ingredients, nutritionText,
usageInstructions, storageInstructions, isActive, isPublished, isFeatured, sortOrder, version.
Slug chỉ lowercase ASCII/số/dấu gạch nối. Muốn publish phải active, có SKU hoạt động và ảnh.
SKU input: skuCode, label, netWeightGrams, sellingPrice, isActive, sortOrder, version. SKU code chuẩn hóa uppercase,
không đổi sau khi tạo; weight không đổi sau khi có lô/đơn. Giá/tên thay đổi không sửa snapshot các đơn cũ.
Không xóa ảnh cuối của Product đang publish; unpublish trước.

## Lô, kho và chứng từ

| Method / route | Quyền / chức năng |
| --- | --- |
| POST `/api/admin/inventory/warehouse` | Owner, khởi tạo MAIN idempotent |
| GET `/api/admin/inventory/locations` | Staff, MAIN và địa điểm gym |
| GET/POST `/api/admin/inventory/batches` | Staff, list / tạo lô |
| PUT `/api/admin/inventory/batches/{id}/block` | Staff, `{version,blocked,reason}`; khóa cần lý do |
| GET/PUT `/api/admin/inventory/batches/{id}/cost` | Owner, giá vốn hiện tại / `{version,unitCost}`; null là chưa biết |
| GET `/api/admin/inventory/balances` | Staff, tồn kho MAIN theo lô và condition; không chứa giá vốn |
| GET/POST `/api/admin/inventory/documents` | Staff, list / chốt một phiếu mới |

Batch input: variantId, batchCode, manufacturedOn, expiresAtUtc, notes. Không sửa SKU/ngày/hạn lô đã tạo;
tạo lô đúng mới và chứng từ điều chỉnh theo quy trình khi cần. Giá vốn sửa chỉ áp dụng lần xuất sau.
Block không tự hủy allocation; đơn giữ lô bị khóa phải reallocate hoặc cancel, không được handover.

POST document ví dụ nhập thành phẩm:

```json
{
  "operationKey": "3a39b276-8b61-41df-9206-6d6791dd447b",
  "documentType": "ProductionReceipt",
  "reason": "Nhập thành phẩm ca sáng",
  "lines": [{ "batchId": 1, "quantity": 20, "fromLocationId": null, "fromCondition": null,
              "toLocationId": 1, "toCondition": "Good", "note": null }],
  "correctsDocumentId": null
}
```

ID trong ví dụ chỉ minh họa; lấy ID thật từ API. Cùng operationKey và payload phát lại phiếu cũ;
đổi payload cùng key trả 409. Gửi UUID mới cho một thao tác nghiệp vụ mới. Không dùng operationKey chung nhiều phiếu.

| documentType | Nguồn → đích hợp lệ |
| --- | --- |
| ProductionReceipt | Ngoài hệ thống → MAIN/Good |
| GymDelivery | MAIN/Good → gym/Good; lô chưa hết hạn/khóa, hàng chưa reserved |
| GymRecall | Gym/condition đã có → MAIN/Quarantine |
| Reclassify | Cùng location, đổi condition; trả về Good chỉ Owner từ Quarantine và lô đủ điều kiện |
| WriteOff | Location/condition → ngoài hệ thống; không tiêu hàng đang reserved |
| Adjustment | Owner: ngoài → MAIN/Quarantine hoặc MAIN → ngoài |
| OrderIssue | Chỉ sinh từ handover, không gửi qua API thủ công |

Chứng từ chốt ngay (Posted), không CRUD draft. Server gán thời gian, không cho ghi lùi.
Sửa sai bằng phiếu mới có `correctsDocumentId`, Owner, lý do; không tự đảo phiếu và không sửa OrderIssue qua API kho.
Mỗi phiếu tối đa 100 dòng, một dòng 1–100000 gói. Toàn phiếu rollback nếu một dòng thất bại.
Khả dụng online = MAIN/Good onHand-reserved, đồng thời lô/SKU/product/location đủ điều kiện.

## Đơn COD và tiền

| Method / route | Chức năng |
| --- | --- |
| POST `/api/orders` | Guest checkout, CSRF + header Idempotency-Key UUID |
| POST `/api/admin/orders` | Staff nhập đơn Facebook/Zalo/Phone/Other; CSRF + Idempotency-Key |
| GET `/api/admin/orders` | Staff, filter status/search (mã đơn hoặc phone), page/size |
| GET/PUT `/api/admin/orders/{id}` | Staff, xem / sửa recipient/internalNote khi PendingConfirmation |
| GET `/api/admin/orders/{id}/events` | Staff, lịch sử chuyển trạng thái và chỉnh thông tin |
| POST `/api/admin/orders/{id}/actions/{command}` | Staff, `{version,reason}`; command = confirm/prepare/handover/complete/cancel/reallocate |
| GET/POST `/api/admin/orders/{id}/receipts` | Owner, lịch sử / nhận đủ tiền hàng COD |
| POST `/api/admin/orders/receipts/{id}/void` | Owner, `{reason}`; sửa sai, không phải hoàn tiền khách |

Checkout body:

```json
{ "recipientName": "Nguyễn Văn A", "recipientPhone": "0901234567", "recipientAddress": "Địa chỉ Hà Nội",
  "customerNote": null, "items": [{ "variantId": 1, "quantity": 2 }] }
```

Không gửi giá/tổng từ giỏ hàng. Cần 1–20 SKU khác nhau, mỗi SKU 1–100 gói, phone `0`+9 số hoặc `+84`+9 số.
Giỏ lưu trình duyệt; SKU trùng phải gộp trước. Checkout kiểm tra lượng hiện có nhưng chưa reserved;
team gọi xác nhận sau. Website chỉ đặt SKU đang công bố; đơn thủ công có thể dùng SKU active chưa công bố.
Admin create body `{source:"Facebook",order:{...checkoutBody}}`. Trạng thái ban đầu vẫn PendingConfirmation.
Địa chỉ tự do, phạm vi giao Hà Nội được nhóm xác minh khi gọi; app không tính quãng đường/phí giao hay gọi hãng vận chuyển.

Response guest 201: orderNumber, status, itemsSubtotal, createdAtUtc. Không recipient, id nội bộ hoặc giá vốn.
Không mở tra cứu đơn công khai bằng mã đơn. Muốn xem/cập nhật thông tin dùng admin có quyền.
Retry mạng dùng cùng key và cùng payload; thay payload cùng key trả 409. Không có purge tự động;
khóa hiện được giữ và không được tái sử dụng, dù ExpiresAtUtc có mốc tối thiểu 7 ngày.

Action/state:

| action | Điều kiện / hiệu ứng |
| --- | --- |
| confirm | Pending → Confirmed, FEFO theo expiry/BatchId, giữ đủ toàn đơn hoặc rollback |
| prepare | Confirmed → Preparing |
| reallocate | Confirmed/Preparing, giải phóng dòng cũ rồi giữ lại FEFO nguyên tử; thiếu thì rollback cả release |
| handover | Confirmed/Preparing → HandedOver, kiểm tra lại lô, trừ onHand/reserved, chụp cost, sinh một OrderIssue |
| complete | HandedOver → Completed, không tự tạo khoản thu COD |
| cancel | Pending/Confirmed/Preparing → Cancelled, cần reason; release lượng giữ, không giảm onHand |

Không giao từng phần, không hủy sau handover, không có luồng trả hàng/refund trong bản này.
Gọi lại cùng action đã đạt đúng state trả hiện trạng, không phát sinh hiệu ứng lần hai; reallocate là thao tác mới cần version hiện tại.
Nhận COD body `{version,receivedAtUtc,reference}`; chỉ HandedOver/Completed, tự nhận đủ itemsSubtotal.
Một khoản thu hiệu lực/đơn; thu lại khi đã có khoản hiệu lực trả khoản cũ. Void có lịch sử rồi mới ghi khoản đúng mới.

## Gym, kiểm đếm và xác minh

| Method / route | Chức năng |
| --- | --- |
| GET/POST `/api/admin/gyms` | Staff, danh sách / tạo gym và location ký gửi |
| PUT `/api/admin/gyms/{id}` | Staff, sửa với version |
| GET `/api/admin/gyms/{id}/stock` | Snapshot kiểm đếm mới nhất + bookEstimate và version địa điểm |
| GET/POST `/api/admin/gyms/{id}/counts` | Lịch sử / chốt kiểm đếm |
| GET/POST `/api/admin/gyms/count-lines/{id}/resolutions` | Xem / ghi xác minh chênh lệch |
| POST `/api/admin/gyms/resolutions/{id}/void` | Owner, `{reason}`, không khôi phục hàng vật lý |

Gym input: gymCode, name, publicAddress, publicPhone, publicDescription, internalContactName,
internalPhone, internalNotes, coverAssetId, isActive, isPublished, version. Public response chỉ chứa thông tin công khai.
Mã gym bất biến, một gym có một location. Ngừng hoạt động vẫn cho thu hồi/xử lý lượng tồn đang có.

`stock` luôn `isRealtime:false`; `lastCountedAtUtc` là thời điểm snapshot, `lastCountLines` là số thực tế đã đếm.
`bookEstimate` = snapshot + ledger sau snapshot, chưa trừ bán chưa ghi nhận; không được hiển thị là tồn thực thời gian thực
và không dùng hứa hàng online. Trước kiểm đếm đầu tiên, snapshot chưa tồn tại dù đã có ledger giao hàng.

Count body `{version,lines:[{batchId,actualGood,actualQuarantine,actualDamaged,note}],note}`.
Version lấy từ `stock`, không lấy version Gym. Dòng bắt buộc đủ mọi lô đã biết, kể cả số đếm 0; không mặc định lô bỏ sót bằng 0.
Server gán thời điểm và chốt trong transaction. Đầu tiên là OpeningBaseline, chỉ Owner, expected null, không suy bán.
Các lần sau Regular có expected/actual theo condition, không bắt lịch cố định. Form cũ sau movement/count khác trả 409.

Resolution body: version dòng kiểm đếm, direction Missing/Surplus, category Sale/Loss/Other, quantity,
reason, evidenceReference, operationKey UUID. Cùng key/nội dung chỉ tạo một xác minh. Tổng xác minh hiệu lực không vượt
chênh lệch từng hướng. Sale chỉ Missing, không vượt thiếu ròng/thiếu Good; lệch điều kiện hàng phải xác minh trước.
Giải thích chênh lệch không tạo movement hoặc tiền. Hỏng tại gym nên ghi Reclassify Good→Damaged trước kiểm đếm,
thu hồi Damaged→MAIN/Quarantine là di chuyển, không hủy lần hai. Từ chối recall/writeoff nếu sổ không đủ, yêu cầu kiểm đếm lại.

## Nội dung, báo cáo và thông báo

| Method / route | Quyền / chức năng |
| --- | --- |
| GET `/api/admin/content` | Staff, draft cùng version |
| PUT `/api/admin/content/{key}` | Staff, lưu draft |
| POST `/api/admin/content/{key}/publish` | Owner, `{version}` |
| GET `/api/admin/reports/sales?fromUtc=...&toUtc=...` | Owner, khoảng UTC [from,to), tối đa 366 ngày |
| GET `/api/admin/notifications` | Owner, trạng thái job, không bot token/payload PII |
| POST `/api/admin/notifications/{id}/retry` | Owner, chỉ job Failed; xác minh trước vì có thể gửi trùng |

Content keys: HomeHero/Story/Usage/Faq/Contact/SiteSettings. Schema v1: title, body (plain text), assetIds (≤20),
linkLabel, linkUrl (đường dẫn bắt đầu `/` không `//` hoặc HTTPS), version. Publish kiểm tra asset tồn tại;
GET public chỉ PublishedJson. Chưa phải CMS HTML tự do, builder, nhiều ngôn ngữ hoặc slideshow có lịch.

Report phân biệt OrdersRequested theo CreatedAt, OrdersCompleted/CompletedItemsValue theo CompletedAt,
CodReceived theo ReceivedAt và chỉ khoản chưa void, KnownIssuedCost theo IssuedAt và snapshotcost.
IssuedPackagesWithUnknownCost công khai phần giá vốn chưa biết; không tự coi là 0.
Đây không phải lợi nhuận ròng; không gồm hoa hồng, tiền gym, phí giao, hỏng hàng và chi phí vận hành.

## Telegram và cấu hình thật

Mặc định `Telegram:Enabled=false`: đơn vẫn tạo nhưng không enqueue khi chưa cấu hình. Để bật, cung cấp từ môi trường
`Telegram__Enabled=true`, `Telegram__BotToken`, `Telegram__TargetChatId` (số signed, nhóm thường âm).
Không ghi token vào appsettings/docs/git hoặc đầu ra lệnh. Restart API sau khi đổi cấu hình; enabled mà thiếu thông tin thì startup từ chối.
Bot cần có quyền gửi tin trong nhóm; token/chat thật chưa được cung cấp nên chưa nghiệm thu gửi Telegram thật.

Khi bật, tạo NotificationJob cùng transaction đơn. Payload chỉ mã đơn, tiền hàng, số gói; nhóm mở admin để xem thông tin khách.
Worker claim atomic SQL, lease 2 phút, HTTP ngoài transaction, tối đa 6 lần, backoff và retry_after.
Poll 3 giây/job. Crash/timeout có thể gây gửi trùng; Sent nghĩa Telegram nhận, không chứng minh người đã đọc.
Fencing lease ngăn worker cũ ghi đè worker mới; không hứa exactly-once cho dịch vụ ngoài.

Nguồn giao thức: [Telegram Bot API sendMessage](https://core.telegram.org/bots/api#sendmessage).
Test dùng sender giả và HTTP handler giả, không gửi thông báo ra nhóm thật.

## Lỗi và concurrency

Ngoài lỗi auth/CSRF, BusinessException trả ProblemDetails với code, status 400/403/404/409/503.
Các code thường gặp: INVALID_INPUT, NOT_FOUND, VERSION_CONFLICT, INSUFFICIENT_STOCK, BATCH_NOT_SELLABLE,
ALLOCATION_MISMATCH, COUNT_LINES_MISSING, GYM_COUNT_REQUIRED, VARIANCE_EXCEEDED, IDEMPOTENCY_CONFLICT,
BUSINESS_BUSY, RATE_LIMIT_EXCEEDED. Không đưa SQL exception/connection string ra response.

Mọi ghi nghiệp vụ dùng scoped Unit of Work và `sp_getapplock` Exclusive transaction-scoped,
resource `ForFitChicken.BusinessWrite.v1`, timeout 10 giây, trong cùng database. Phù hợp nhóm nhỏ;
đây là giới hạn throughput có chủ ý, không phải nhiều worker tùy ý hoặc Redis/distributed transaction.
Không tự replay toàn use case khi lỗi; client giữ key idempotency nếu có và request lại bằng scope mới.

## Việc ngoài phạm vi đã triển khai

Frontend React thật, triển khai hosting/domain, dữ liệu/ảnh thương hiệu, credential Owner và Telegram thật;
quản lý tài khoản Operator/reset password, refund/trả hàng, giao từng phần, nguyên liệu/công thức,
bank QR/hoa hồng gym/chi phí kế toán, shipping fee/API giao hàng, rich CMS, object storage/CDN và backup production.
Không gọi backend này là hệ thống kế toán hoàn chỉnh hoặc đã sẵn sàng production chỉ từ kết quả test.
