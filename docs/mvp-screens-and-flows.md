# Màn hình, quyền và luồng thao tác MVP

Ngày: 25/10/2026. Căn cứ: [Yêu cầu MVP](mvp-requirements.md).

Kế hoạch triển khai giao diện: [Frontend MVP](frontend-plan.md), lập ngày 26/10/2026 từ khảo sát website tham khảo và phạm vi hiện tại.

Đây là thiết kế chức năng để chuẩn bị ERD/API, chưa phải giao diện React. Đường dẫn chỉ là đề xuất. Những lựa chọn chưa được người dùng chốt được đánh dấu “đề xuất”. Không thêm chức năng chỉ vì website tham khảo có sẵn.

## 1. Cơ sở thiết kế

- Website giới thiệu thương hiệu là trọng tâm; có giỏ và gửi yêu cầu COD không tài khoản.
- Đơn website phải được nhóm gọi lại và xác nhận thủ công.
- Đã chốt ngày 26/10/2026: chỉ giữ hàng khi nhân viên xác nhận. Đơn chờ và giỏ chưa giữ hàng; MVP một kho chính.
- Chưa tính phí giao: hiển thị tiền hàng, không hiển thị tổng cuối cùng hoặc phí 0đ; trao đổi phí ngoài app khi gọi.
- Nhóm tự tổ chức giao bên ngoài. App cập nhật bàn giao/hoàn tất và thu tiền COD thủ công.
- Gym không dùng phần mềm bán lẻ; QR/loa, hoa hồng và đối soát tiền ngoài MVP.
- Kiểm đếm bất cứ lúc nào theo thao tác người dùng; không mặc định lịch hoặc job tạo kiểm đếm.

## 2. Màn hình công khai

| Mã | Màn hình / đường dẫn đề xuất | Dữ liệu và thao tác | Trạng thái cần thiết |
| --- | --- | --- | --- |
| P01 | Trang chủ `/` | Banner, lợi ích, câu chuyện, sản phẩm nổi bật, cách dùng/bảo quản, điểm bán, liên hệ; đi đến sản phẩm/giỏ | Ảnh lỗi có thay thế, nội dung chưa công bố không xuất hiện |
| P02 | Sản phẩm `/san-pham` | Danh sách SKU đang công bố, ảnh, quy cách, giá; tìm tên, xem chi tiết, thêm giỏ | Đang tải, rỗng, lỗi, ngừng nhận yêu cầu |
| P03 | Chi tiết `/san-pham/:slug` | Nội dung sản phẩm, SKU/quy cách, giá, số lượng; thêm giỏ, liên hệ | SKU ngừng bán, không tồn tại, số lượng không hợp lệ |
| P04 | Giỏ hàng — drawer hoặc `/gio-hang` | Sửa số lượng, xóa, xem tiền hàng và tiếp tục | Giỏ rỗng; sản phẩm/giá thay đổi; chưa giữ tồn |
| P05 | Thông tin nhận hàng — bước checkout | Tên, điện thoại, địa chỉ Hà Nội, ghi chú; sang bước kiểm tra | Thiếu/sai trường, địa chỉ cần xác minh khi gọi; không hỏi email bắt buộc hay đăng ký |
| P06 | Kiểm tra yêu cầu COD — bước checkout | Danh sách, số lượng, đơn giá, tiền hàng, địa chỉ; sửa thông tin, gửi yêu cầu | Đang gửi, lỗi xác thực, giá đổi cần xem lại, quá nhiều yêu cầu, mạng không rõ kết quả |
| P07 | Đã nhận yêu cầu — bước cuối | Mã đơn, lời nhắn sẽ gọi xác nhận, bản tóm tắt vừa gửi; về trang chủ/sản phẩm | Không gọi là đã thanh toán/đã xác nhận; tải lại không tạo đơn mới |
| P08 | Điểm bán `/diem-ban` | Danh sách gym đang hợp tác, địa chỉ, liên hệ | Không lộ dữ liệu nội bộ hoặc cam kết tồn hiện tại |
| P09 | Giới thiệu/liên hệ | Nội dung thương hiệu, hướng dẫn và liên kết liên hệ ngoài | Chưa xây ticket/chatbot hoặc form thu lead riêng |

P04–P07 có thể dùng chung một drawer nhiều bước như mẫu tham khảo; đây là các bước chức năng, không bắt buộc là bốn trang. Giỏ có thể lưu cục bộ trên trình duyệt, đề xuất không cần lưu giỏ trên server hoặc đồng bộ nhiều thiết bị trong MVP.

Không có màn hình khách đăng nhập, tài khoản, tự hủy, hoàn tiền hoặc tra cứu đơn công khai trong MVP. Mã đơn không phải quyền truy cập: nếu sau này bổ sung tra cứu phải có cơ chế bảo vệ thông tin. Trang nhận yêu cầu chỉ trả dữ liệu tối thiểu cho phiên vừa gửi, không mở API đọc đơn bằng mã đoán được.

## 3. Màn hình nội bộ

| Mã | Màn hình | Nội dung và thao tác chính |
| --- | --- | --- |
| A01 | Đăng nhập `/admin/login` | Tài khoản cá nhân nội bộ, đăng xuất; báo lỗi chung khi đăng nhập thất bại |
| A02 | Tổng quan | Đơn chờ gọi, đang chuẩn bị/đã bàn giao chưa hoàn tất, hàng sắp hết hạn, chênh lệch gym chưa xử lý; liên kết tới danh sách |
| A03 | Danh sách đơn | Lọc nguồn, trạng thái, ngày; tìm mã/điện thoại; mở chi tiết, tạo đơn thủ công |
| A04 | Chi tiết/tạo đơn | Người nhận, nguồn, dòng hàng, snapshot giá, tiền hàng, trạng thái, ghi chú gọi; xác nhận, chuẩn bị, bàn giao, hoàn tất, ghi thu tiền, hủy trước xuất |
| A05 | Sản phẩm và SKU | Tạo/sửa nội dung, ảnh, quy cách, giá, thứ tự, nổi bật, công bố/ngừng bán; không xóa cứng đối tượng đã có lịch sử |
| A06 | Lô và nhập thành phẩm | Mã lô, SKU, ngày sản xuất/hạn dùng, số lượng; tạo/chốt phiếu nhập; giá vốn chỉ chủ hệ thống xem/sửa |
| A07 | Kho chính và lịch sử | Tồn vật lý, đã giữ, không đủ điều kiện bán, có thể bán theo lô; xem biến động, tạo phiếu hỏng/hủy/điều chỉnh |
| A08 | Danh sách/chi tiết gym | Thông tin công khai và nội bộ; hàng đếm gần nhất kèm thời điểm; giao/thu hồi sau mốc; lịch sử phiếu |
| A09 | Giao hàng ký gửi | Chọn gym, SKU/lô, số lượng; lưu nháp, xác nhận bàn giao; kiểm tra không lấy phần đang giữ cho đơn |
| A10 | Thu hồi/hỏng gym | Ghi tình trạng, số lượng và thao tác vật lý; hàng thu hồi chờ kiểm tra, liên kết hỏng trước đó để tránh giảm hai lần |
| A11 | Kiểm đếm gym | Bấm tạo mới, chọn gym/mốc thực tế; nhập lượng theo lô và tình trạng; xem chênh lệch, lưu nháp, xác nhận |
| A12 | Xác minh chênh lệch | Xem phần chưa giải thích; ghi số bán xác nhận, thiếu/thừa chưa rõ, lý do/kết luận; không tự tạo hoa hồng/tiền |
| A13 | Báo cáo cơ bản | Đơn và tiền COD đã ghi nhận; tồn/lô/hạn dùng, hỏng/thu hồi, số bán gym xác nhận theo khoảng kiểm đếm; xuất bảng theo quyền |
| A14 | Nội dung website | Sửa các trường cố định của trang chủ, giới thiệu, liên hệ và hướng dẫn; không phải page builder hoặc CMS bài viết đầy đủ |
| A15 | Tài khoản và quyền | Chủ hệ thống tạo/khóa tài khoản nội bộ, cấp vai trò; không đăng ký admin công khai |
| A16 | Nhật ký thao tác | Chủ hệ thống xem ai sửa/xác nhận/hủy, thời điểm và đối tượng; không ghi mật khẩu hoặc dữ liệu bí mật |

Không cần 16 trang rời: nhập lô là form của màn hình kho; tạo đơn dùng cùng component chi tiết; giao/thu hồi/kiểm đếm là tab/form trong chi tiết gym. Không làm màn hình hoa hồng, tiền gym, ngân hàng, shipper hoặc sản xuất nguyên liệu.

## 4. Phân quyền đề xuất

Hai vai trò nội bộ: `Owner` (chủ hệ thống) và `Operator` (vận hành). Đây là cấu hình khởi đầu, không tương ứng bắt buộc với từng người trong nhóm.

| Thao tác | Khách | Operator | Owner |
| --- | --- | --- | --- |
| Xem nội dung và gửi yêu cầu COD | Có | Có | Có |
| Xem đơn nội bộ và thông tin người nhận | Không | Có | Có |
| Tạo/sửa đơn chờ, gọi và xác nhận | Không | Có | Có |
| Chuẩn bị/bàn giao/hoàn tất, ghi nhận thu COD | Không | Có | Có |
| Hủy trước xuất, kèm lý do | Không | Có | Có |
| Xem sản phẩm/lô/tồn | Chỉ thông tin công khai | Có, không giá vốn | Có |
| Sửa giá, công bố sản phẩm, sửa nội dung | Không | Không | Có |
| Nhập thành phẩm và giao/thu hồi gym | Không | Có | Có |
| Tạo và xác nhận kiểm đếm | Không | Có | Có |
| Ghi xác minh, xác nhận số bán gym | Không | Có | Có |
| Chốt điều chỉnh kho do thiếu/thừa hoặc sửa sai chứng từ | Không | Lập đề nghị | Có |
| Ghi nhận hỏng/hủy có lý do | Không | Có | Có |
| Xem/sửa giá vốn, chi phí và báo cáo tài chính | Không | Không | Có |
| Xem/xuất báo cáo vận hành | Không | Có | Có |
| Quản lý tài khoản, xem nhật ký tổng hợp | Không | Không | Có |

Các thao tác Operator được xác nhận phải vẫn có dấu vết; không yêu cầu Owner duyệt mọi thao tác thường ngày. API kiểm tra quyền cả chức năng lẫn trường dữ liệu, đặc biệt giá vốn. Xuất dữ liệu áp dụng cùng quyền với màn hình, không đưa trường bị ẩn vào file.

## 5. Trạng thái đơn và tác động kho

| Trạng thái | Ý nghĩa | Kho theo đề xuất giữ lúc xác nhận |
| --- | --- | --- |
| `PendingConfirmation` | Đã nhận yêu cầu, chờ gọi | Chưa giữ, chưa xuất |
| `Confirmed` | Đã gọi, thống nhất đơn và đủ hàng | Giữ đủ lượng theo lô trong transaction xác nhận |
| `Preparing` | Đang chuẩn bị | Vẫn giữ; chưa ghi xuất lần nữa |
| `HandedOver` | Đã bàn giao ra khỏi kho để đi giao | Trừ vật lý và giải phóng lượng giữ tương ứng đúng một lần |
| `Completed` | Đã xác nhận giao xong | Không trừ kho thêm |
| `Cancelled` | Hủy trước xuất, ghi lý do | Giải phóng nếu đã giữ, không tăng vật lý |

Chuyển chính: PendingConfirmation -> Confirmed -> Preparing -> HandedOver -> Completed.

PendingConfirmation/Confirmed/Preparing có thể sang Cancelled nếu chưa xuất. Không cho HandedOver/Completed sang Cancelled để tự hoàn kho. Giao thất bại ghi chú/cảnh báo xử lý còn mở, không đánh dấu Completed; xử lý thực tế ngoài app và ghi chứng từ tiếp nhận kho riêng nếu có. Chưa xây state machine hoàn/trả hàng đầy đủ.

- Xác nhận đơn cùng kiểm tra giữ tồn là một transaction. Gửi lặp hoặc hai người cùng xác nhận không giữ hàng hai lần.
- Lô hết hạn/khóa bán sau khi giữ: phải kiểm tra lại lúc chuẩn bị/xuất, thay phân bổ hợp lệ an toàn hoặc báo không thể xuất. Không xuất lô không đủ điều kiện chỉ vì đã giữ trước đó.
- Chỉ sửa sản phẩm/số lượng/giá của đơn chờ trước xác nhận; lưu lịch sử và chỉ áp dụng nội dung đã trao đổi với khách. Sau xác nhận chưa cho sửa tự do làm lệch lượng giữ; thay đổi trước xuất xử lý hủy/tạo lại có tham chiếu trong MVP.
- Tránh tạo đồng thời nút “xuất kho” riêng và “bàn giao” cùng trừ cho một đơn; bàn giao liên kết đúng một phiếu xuất.

## 6. Ghi nhận tiền COD tối thiểu

- Tách `Uncollected` và `Collected` khỏi trạng thái đơn. Đây là tiền hàng về nhóm, không phải tự xác nhận tiền ship hoặc tiền shipper đang giữ.
- Chỉ ghi thu tiền thủ công khi nhóm thực nhận, ghi số tiền hàng, ngày nhận, người nhập và tham chiếu/ghi chú. Không tự đánh dấu thu khi bấm hoàn tất giao.
- Đã chốt thu đủ tiền hàng trong MVP; chưa xử lý thu một phần, trả góp, hoàn tiền hoặc đối chiếu COD nhiều đợt. Khoản khác tiền hàng chuyển chủ hệ thống xử lý, không tự ghi đủ.
- Đề xuất chỉ cho ghi thu ở HandedOver hoặc Completed. Chống thao tác lặp; điều chỉnh ghi sai có lịch sử và quyền Owner, không chỉnh lặng lẽ.
- Người dùng đồng ý nguyên tắc thu đủ tiền COD đã về nhóm ngày 26/10/2026; chi tiết quyền/void phiếu vẫn là đề xuất thiết kế. Không lấy tổng đơn PendingConfirmation làm doanh thu.

## 7. Luồng kiểm đếm và xác minh gym

1. Mở gym -> Tạo kiểm đếm. Chọn thời điểm thực tế, hệ thống xác định mốc đã chốt trước và biến động liên quan.
2. Nhập lượng đếm theo lô; hàng hỏng đang nằm tại gym được ghi ở phần riêng, không nhầm với hàng đã thu hồi/hủy.
3. Xem lượng cần giải thích và dữ liệu nguồn; chỉnh nháp khi chưa xác nhận.
4. Xác nhận kiểm đếm: lưu snapshot thực tế và khoảng đối chiếu. Chưa biết nguyên nhân thiếu vẫn chốt được số đã đếm; không tự ghi số bán.
5. Sau xác minh thực tế, ghi phần bán xác nhận hoặc nguyên nhân khác. Phần chưa rõ vẫn mở; tổng phân loại không vượt phần cần giải thích.
6. Lần kiểm đếm sau dùng snapshot mới, kể cả chênh lệch kỳ trước chưa xác minh xong. Xử lý kỳ trước không được trừ kho lần nữa: thiếu đã thể hiện trong snapshot, phân loại sau chỉ giải thích thiếu.

Kiểm soát mốc: đề xuất không cho chốt ngược trước lần đã xác nhận gần nhất. Nếu có biến động liên quan được nhập thêm sau khi mở nháp, yêu cầu tính lại trước chốt. Mốc bằng nhau cần thứ tự chứng từ nhất quán. Không cho thêm biến động có hiệu lực trong khoảng kiểm đếm đã chốt mà không qua xử lý sửa sai có lịch sử của Owner.

Giao hàng ký gửi trong MVP có thể xác nhận một lần khi thực tế bàn giao, giảm kho chính và ghi hàng giao gym cùng transaction; chưa xây kho đang vận chuyển. Phiếu nháp không trừ kho.

## 8. Các hành trình cần nghiệm thu

| Mã | Tình huống | Kết quả cần thấy |
| --- | --- | --- |
| U01 | Khách không tài khoản gửi COD | Một đơn chờ xác nhận, mã tham chiếu và lời nhắn sẽ gọi; chưa thanh toán |
| U02 | Khách bấm gửi lại do mạng chậm | Không phát sinh đơn trùng; khi kết quả chưa rõ, retry cùng khóa yêu cầu |
| U03 | Giá đổi khi đang checkout | Hiển thị tiền hàng mới và yêu cầu xem lại trước gửi |
| U04 | Hai người xác nhận hai đơn cho lượng cuối | Chỉ đơn đủ hàng được xác nhận, không âm tồn |
| U05 | Khách từ chối khi gọi | Hủy có lý do; chưa giữ thì không tác động kho |
| U06 | Hủy Confirmed/Preparing | Giải phóng giữ đúng một lần, không cộng vật lý |
| U07 | Bàn giao rồi hoàn tất đơn | Một lần xuất kho, hoàn tất không xuất lần hai |
| U08 | Hoàn tất giao nhưng tiền chưa về nhóm | Giữ trạng thái chưa thu; báo cáo không cộng tiền thực nhận |
| U09 | Kiểm đếm gym bất kỳ lúc nào | Lưu đúng thời điểm thực tế, chưa rõ chênh lệch vẫn ghi được số đếm |
| U10 | Giao thêm sau kiểm đếm | Hiện riêng; không trình bày cộng số đó thành tồn thực tế hiện tại |
| U11 | Xác minh thiếu kỳ trước sau kỳ mới | Cập nhật phân loại cũ, không trừ kho lần hai |
| U12 | Hàng hỏng được thu hồi sau khi đã ghi hỏng | Chuyển vị trí đúng lượng hỏng; không giảm thêm hàng tốt hoặc hủy hai lần |
| U13 | Operator gọi thẳng API giá vốn/tài khoản | Bị từ chối hoặc loại trường theo quyền, không chỉ ẩn UI |

## 9. Giới hạn báo cáo

- Đơn: phân biệt giá trị yêu cầu, giá trị đã xác nhận, tiền hàng giao hoàn tất và COD thực nhận; không gộp thành một con số doanh thu.
- Gym: báo số lượng bán xác nhận theo khoảng kiểm đếm. Không có tổng doanh thu/tiền thu gym vì không nhập tiền hoặc chi tiết giá giao dịch.
- Không báo “lợi nhuận toàn hệ thống” khi thiếu hoa hồng, chi phí giao và tiền gym. Giá vốn/chi phí nếu xây chỉ là dữ liệu quản trị có nhãn giới hạn rõ.
- Cảnh báo hạn dùng dựa trên lô còn ghi nhận tại kho; với gym nêu mốc kiểm đếm, không tuyên bố còn hàng chính xác.

## 10. Những lựa chọn cần chốt trước khi triển khai phần liên quan

1. Thời điểm giữ hàng tại Confirmed và một kho chính đã chốt ngày 26/10/2026; tiếp tục thiết kế phân bổ lô.
2. Ma trận Owner/Operator, khả năng chỉnh giá đơn chờ và quyền điều chỉnh kho.
3. Ghi nhận tiền hàng COD: đã chốt nhận đủ tiền thủ công, chưa thu một phần; chi tiết điều chỉnh sai vẫn cần thiết kế.
4. Mức chi tiết giá vốn và chi phí; không đưa tài chính gym trở lại MVP.
5. Cách xác minh địa chỉ phục vụ trong Hà Nội: chưa thêm tự động bản đồ/phí khu vực; nhân viên kiểm tra khi gọi theo đề xuất.

## 11. Bước thiết kế tiếp theo

Thiết kế ERD dựa trên các nhóm: tài khoản nội bộ; nội dung; sản phẩm/SKU; lô; kho và biến động; đơn và dòng hàng; giữ/phân bổ lô; ghi thu COD; gym; giao/thu hồi/hỏng; kiểm đếm và phân loại chênh lệch; nhật ký. Chưa tạo bảng cho tài khoản khách, ngân hàng, hoa hồng, báo giá giao vận hoặc nguyên liệu. Sau ERD mới cụ thể hóa OpenAPI và triển khai backend.
