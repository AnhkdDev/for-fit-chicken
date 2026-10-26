# Yêu cầu nghiệp vụ MVP — Ức gà nấu chậm

Ngày cập nhật: 25/10/2026. Trạng thái: bản nháp để thiết kế; chưa phải đặc tả API.

Tài liệu tiếp theo: [Màn hình, quyền và luồng thao tác](mvp-screens-and-flows.md). Các lựa chọn thiết kế chưa chốt trong tài liệu đó không tự thay thế quyết định nghiệp vụ đã thống nhất.

Bản thiết kế dữ liệu để thảo luận: [Database/ERD v0.1](database-design.md), ngày 26/10/2026. Chưa có database/migration được áp dụng.

## 1. Mục tiêu và quyết định đã chốt

- Website ưu tiên quảng bá thương hiệu, responsive; có giỏ hàng và luồng đặt hàng cơ bản hoạt động thật.
- Trang quản trị phục vụ nhóm bốn người. Không có màn hình bán hàng cho nhân viên gym trong MVP.
- Nhóm tự sản xuất. Theo ngữ cảnh trao đổi, MVP bán thành phẩm có sẵn, không nhận đặt trước; cần đính chính nếu cụm “MVP chỉ bán hàng” mang nghĩa khác.
- Giao hàng trong khu vực Hà Nội. Nhóm tự tổ chức giao bên ngoài, không xây phân hệ giao vận hay tích hợp hãng vận chuyển.
- Khách đặt website bằng COD, không cần tài khoản. Đơn mới ở trạng thái chờ xác nhận; thành viên nhóm gọi lại rồi xác nhận thủ công. Quyết định này thay thế luồng tự nhận đơn trước đó.
- Đã chốt Telegram báo đơn mới vào nhóm nội bộ. Đề xuất lưu công việc gửi cùng transaction tạo đơn, worker gửi/retry sau commit; không cần RabbitMQ/Redis. Nhóm mở admin để xử lý, không xác nhận đơn trực tiếp trên Telegram. Xem [thiết kế thông báo](database/order-notifications.md).
- Chưa tính phí giao trên app. Không hiển thị phí bằng 0 như cam kết miễn phí; tiền hàng trên website chưa gồm phí giao, nhóm trao đổi khi gọi xác nhận.
- Gym theo mô hình ký gửi; khách mua sản phẩm có sẵn, chuyển tiền về tài khoản của nhóm qua QR riêng theo gym.
- QR và loa do nhóm vận hành ngoài app. Không tích hợp ngân hàng, giao dịch, webhook hoặc thiết bị loa cho kênh gym trong MVP.
- Thỏa thuận kinh doanh vẫn là hoa hồng gym cố định theo gói, nhóm chịu hàng hỏng; chưa xây phân hệ tính/trả hoa hồng, chốt đối soát tiền hoặc nhập tổng tiền gym trong MVP.
- Chưa xây quy trình hoàn tiền/trả hàng cho khách trong MVP. Giữ thao tác hủy đơn nội bộ tối thiểu để đóng đơn không xác nhận và giải phóng hàng nếu đã giữ.
- Nhóm trực tiếp kiểm đếm, nhập dữ liệu và xác minh chênh lệch.
- Kiểm đếm do người dùng chủ động tạo bất cứ khi nào cần, không bắt buộc chu kỳ cố định.
- Hàng ký gửi gym không dùng để cam kết tồn cho đơn online.
- Backend ASP.NET Core Web API, frontend React, SQL Server dùng chung; backend MVP hoàn thành trước frontend.
- Yêu cầu bổ sung ngày 27/10/2026: Clean Architecture 5 project, Unit of Work tại ranh giới use case; global rate limiting cho API nghiệp vụ và named policies theo nhóm. Xem [đặc tả kiến trúc backend](backend-architecture.md). Đây là yêu cầu triển khai, chưa phải tính năng đã code.
- Chưa dùng RabbitMQ hoặc Redis lock. Bảo vệ dữ liệu bằng transaction, ràng buộc database, concurrency control và idempotency phù hợp.

## 2. Vai trò — đề xuất cho MVP

- Chủ hệ thống: cấu hình, tài khoản, giá, báo cáo và toàn bộ nghiệp vụ trong phạm vi MVP.
- Vận hành: sản phẩm, đơn, lô, nhập/chuyển/thu hồi hàng, kiểm đếm theo quyền được cấp.
- Khách: truy cập công khai, giỏ hàng và gửi đơn COD không cần đăng ký. Không xây đăng nhập hoặc trang tài khoản khách trong MVP.
- Mỗi thành viên nội bộ có tài khoản riêng; ghi người thao tác. Quyền kiểm tra tại backend.
- Chưa xây cổng đối tác gym.

## 3. Phạm vi chức năng

### 3.1. Trang công khai

- Nội dung thương hiệu, sản phẩm, hình ảnh, quy cách, hướng dẫn sử dụng và bảo quản có căn cứ.
- Danh sách điểm bán gym, thông tin liên hệ và hợp tác.
- Giỏ hàng và checkout cơ bản. Chưa ưu tiên tích điểm, voucher phức tạp, đánh giá hoặc gợi ý sản phẩm.
- Không hiển thị lượng tồn gym như cam kết còn hàng hiện tại.
- Phạm vi chỉnh sửa nội dung trong admin giữ đơn giản; không xây trình dựng trang tổng quát.

### 3.2. Sản phẩm và thành phẩm

- Sản phẩm có SKU theo vị/quy cách, trạng thái bán, hình ảnh và giá.
- Nhập thành phẩm theo lô: SKU, ngày sản xuất, hạn dùng, số lượng, giá vốn phục vụ báo cáo.
- Nhóm tự sản xuất nhưng MVP chưa bao gồm kế hoạch sản xuất, công thức, kho nguyên liệu và định mức hao hụt.
- Thay đổi tên/giá/quy cách không làm thay đổi dữ liệu giao dịch lịch sử.

### 3.3. Kho chính

- Nhập, giữ hàng theo đơn, xuất bán, giao ký gửi, tiếp nhận thu hồi, hủy và điều chỉnh kiểm kê.
- Tách số đang có, đang giữ và không đủ điều kiện bán; không để số có thể bán âm.
- Ưu tiên xuất lô hết hạn trước trong các lô đủ điều kiện bán.
- Lưu biến động theo SKU/lô/địa điểm, thời điểm, chứng từ nguồn, lý do và người thực hiện.
- Hàng trả/thu hồi cần kiểm tra tình trạng trước khi được nhập lại tồn có thể bán.

### 3.4. Đơn website và đơn nhập thủ công

- Luồng: gửi đơn chờ xác nhận -> nhóm gọi lại -> xác nhận -> chuẩn bị -> đã bàn giao đi giao -> hoàn tất; có hủy đơn nội bộ tối thiểu kèm lý do.
- Nhân viên nhập đơn từ Zalo/Facebook/điện thoại qua cùng quy tắc nghiệp vụ; lưu nguồn đơn.
- Server tính tiền hàng và kiểm tra sản phẩm còn được phép nhận yêu cầu. Không tin đơn giá/tổng tiền do frontend gửi.
- Gửi lại cùng yêu cầu không tạo thêm đơn. Trang hoàn tất thông báo đã nhận yêu cầu, sẽ gọi xác nhận; không tuyên bố đơn đã được xác nhận hoặc thanh toán.
- Đã chốt ngày 26/10/2026: giỏ và đơn chờ xác nhận chưa giữ tồn; chỉ giữ đủ hàng khi thành viên xác nhận, trong cùng transaction đổi trạng thái. MVP có một kho chính.
- Lúc gửi đơn có thể kiểm tra lượng bán được hiện tại để báo sớm, nhưng không cam kết giữ hàng. Khi xác nhận phải kiểm tra lại; thiếu hàng thì không xác nhận hoặc xuất âm, nhóm liên hệ khách xử lý.
- Nếu nhiều đơn cùng chờ cho lượng tồn cuối, chỉ các đơn giữ đủ hàng mới được xác nhận. Không tự thay thế mặt hàng hoặc xác nhận một phần.
- Chỉ dùng COD cho đơn online. Trạng thái đơn riêng với ghi nhận đã thu tiền COD; cập nhật thu tiền thủ công, không giả định hoàn tất giao là đã nhận tiền về nhóm.
- Người dùng đồng ý ghi nhận tiền COD đã về nhóm trong app; giai đoạn đầu ghi nhận thu đủ tiền hàng, chưa thu một phần. Phí giao ngoài app không cộng lẫn vào khoản thu tiền hàng.
- Lưu snapshot tên, quy cách và đơn giá. Tổng tiền hàng chưa gồm phí giao; không hiển thị “miễn phí giao” hoặc tổng tiền phải trả cuối cùng khi phí chưa được thỏa thuận.
- Checkout tối thiểu: giỏ -> thông tin nhận -> kiểm tra tiền hàng và COD -> gửi yêu cầu -> mã đơn và lời nhắn sẽ gọi xác nhận.
- Chỉ lưu thông tin giao hàng tối thiểu: người nhận, số điện thoại, địa chỉ, ghi chú; trạng thái bàn giao/hoàn tất được cập nhật thủ công.
- Không có phân công shipper, định tuyến, theo dõi vị trí hoặc tích hợp giao vận.
- Hủy đơn chờ không làm thay đổi tồn nếu chưa giữ. Hủy trước xuất sau khi giữ giải phóng đúng một lần. Chưa làm nút khách tự hủy hoặc hoàn tiền/trả hàng đầy đủ.
- Không cho hủy đơn đã xuất rồi tự cộng lại tồn bán. Ngoại lệ sau giao xử lý thủ công ngoài app trong MVP; điều chỉnh kho nếu cần phải có chứng từ/lý do riêng.
- Không tích hợp cổng thanh toán online, không có đếm ngược thanh toán và không tự hủy do chưa trả tiền trong luồng COD này.

### 3.5. Ký gửi gym

- Quản lý gym và địa điểm. Chính sách hoa hồng xử lý ngoài app trong MVP.
- Phiếu giao theo SKU/lô/hạn dùng, số lượng và thời điểm bàn giao.
- Ghi nhận thu hồi, hàng hỏng, xử lý hủy và chênh lệch; tránh ghi giảm hai lần cùng lượng hàng.
- Không ghi từng lượt mua của khách gym; không tạo đơn bán lẻ giả định cho từng lượt.
- Hiển thị lượng thực tế tại lần kiểm đếm gần nhất cùng thời điểm đếm. Các biến động sau đó hiển thị riêng, không coi là tồn thực tế hiện tại.
- Không suy ra lô thực tế đã bán chỉ bằng giả định ưu tiên lô hết hạn trước; cần kết quả kiểm đếm/xác minh.

### 3.6. Kiểm đếm và xác minh

- Người có quyền chủ động tạo phiếu kiểm đếm theo gym; đề xuất trạng thái nháp -> xác nhận, có cơ chế hủy nháp.
- Ghi thời điểm thực tế đếm riêng với thời điểm nhập dữ liệu; lưu người kiểm đếm và người xác nhận.
- Ghi lượng thực tế theo SKU/lô, phân biệt hàng đủ điều kiện và hàng hỏng còn tại gym.
- Mốc kiểm đếm phải thống nhất với mốc giao/thu hồi để không bỏ sót hoặc tính hai lần biến động.
- Đề xuất chỉ cho chốt một cuộc kiểm đếm tại một mốc của một gym; lần chốt tiếp theo dùng mốc xác nhận trước đó.
- Công thức đối chiếu theo cùng phạm vi hàng: tồn đầu + giao thêm - thu hồi - hỏng đã ghi giảm - tồn cuối = lượng cần giải thích. Các khoản phải loại trừ nhau.
- Lượng cần giải thích chưa tự động là số bán. Tách số bán xác nhận, thiếu/thừa chưa rõ và các biến động khác.
- Thành viên nhóm đến xác minh, lưu lý do, kết luận và bằng chứng nếu có. Cho phép còn chênh lệch chưa giải quyết.
- Xác nhận kiểm đếm là nghiệp vụ số lượng; không chốt tiền hoặc tính hoa hồng trong MVP.
- Không tự phân bổ số bán xác nhận theo kỳ thành số bán từng ngày.

### 3.7. Tiền và hoa hồng gym — ngoài MVP

- Tiền khách chuyển trực tiếp về nhóm qua QR ngoài app. Nhóm tự đối chiếu và trả hoa hồng bên ngoài.
- Chưa xây cấu hình/tính/trả hoa hồng, kỳ chốt đối soát tiền, công nợ hoa hồng hoặc nhập tổng tiền gym theo kỳ.
- Giữ dữ liệu số lượng bán xác nhận, kiểm đếm và lịch sử để có thể bổ sung tài chính sau; không tạo sẵn các module chưa cần.
- Không suy số gói từ số tiền hoặc từ loa, không nhập từng giao dịch ngân hàng trong MVP.

### 3.8. Báo cáo và lịch sử

- Đơn online theo nguồn và trạng thái; tách giá trị đơn với tiền thực thu.
- Tồn kho chính; tồn gym tại mốc kiểm đếm; hạn dùng, hàng hỏng/thu hồi và chênh lệch.
- Số bán gym đã xác nhận theo kỳ; không giả định có thời điểm bán chính xác.
- Không có báo cáo hoa hồng/tiền gym trong MVP. Giá vốn và chi phí cơ bản phục vụ báo cáo cần thống nhất mức chi tiết khi thiết kế màn hình.
- Nêu rõ phạm vi và độ đầy đủ của số liệu. Chưa đủ chi phí thì không gọi là lợi nhuận thực tế.
- Chưa coi báo cáo quản trị nội bộ là hệ thống kế toán đầy đủ hoặc nghiệp vụ chia lợi nhuận thành viên.
- Lưu lịch sử chỉnh sửa và xuất dữ liệu cần thiết.

## 4. Tiêu chí nghiệm thu trọng yếu

1. Hai thành viên xác nhận các đơn cho lượng tồn cuối: chỉ xác nhận trong giới hạn tồn hợp lệ, không âm tồn; chỉ giữ hàng khi thành viên xác nhận đơn.
2. Gửi lại yêu cầu tạo đơn: cùng kết quả, không tạo đơn trùng. Gửi lặp xác nhận/hủy không giữ hoặc giải phóng trùng.
3. Khách gửi đơn: trạng thái chờ xác nhận, chưa phải đơn được nhóm xác nhận. Backend từ chối xác nhận nếu không đủ hàng hợp lệ.
4. Hàng tại gym không làm tăng lượng website được phép nhận đơn.
5. Hết hạn hoặc bị khóa bán: không nhận đơn từ lượng đó.
6. Tạo kiểm đếm bất kỳ lúc nào; hiển thị đúng thời điểm thực tế và lượng đếm gần nhất.
7. Giao hàng sau kiểm đếm không biến con số đếm cũ thành tồn thời gian thực.
8. Hàng thiếu chưa rõ không tự ghi là bán; không tự sinh dữ liệu tài chính từ kiểm đếm.
9. Hỏng rồi thu hồi cùng lượng hàng không làm giảm kho hai lần.
10. Chốt kiểm đếm bị gửi lặp hoặc xử lý đồng thời không ghi biến động trùng.
11. Không có QR/loa/ngân hàng kết nối, các nghiệp vụ giao, đếm và xác minh vẫn vận hành được.
12. Đổi giá không làm thay đổi đơn hàng lịch sử đã lưu.
13. Người không có quyền không đọc/sửa tài chính và chứng từ nội bộ, kể cả gọi API trực tiếp.
14. Khách không cần tài khoản, không nhập thông tin thẻ, không thanh toán trước; chỉ gửi yêu cầu COD.
15. Giao diện không hiển thị 30.000đ theo ảnh tham khảo, phí 0đ hoặc cam kết miễn phí; ghi rõ phí giao sẽ được trao đổi khi gọi xác nhận.

## 5. Điểm cần chốt trước khi hoàn tất database/API

- Phạm vi phục vụ cụ thể trong Hà Nội và cách kiểm tra địa chỉ được nhận đơn.
- Thời điểm giữ tồn và một kho chính đã chốt ngày 26/10/2026; còn thiết kế cách phân bổ lô và giải phóng an toàn.
- Quy trình khi không liên hệ được khách: giữ chờ/liên hệ lại hoặc nhân viên hủy có lý do; chưa tự đặt số lần gọi hay thời hạn.
- Cách tra cứu đơn an toàn nếu có nhu cầu; chưa mặc định có cổng tra cứu công khai.
- Chi tiết ghi nhận/sửa sai phiếu thu COD; nguyên tắc thu đủ tiền hàng thực về nhóm đã chốt.
- Cách ghi nhận số bán gym và xử lý thiếu/thừa sau xác minh.
- Cách nhập giá vốn lô và các chi phí để báo cáo có ý nghĩa.

## 6. Trình tự tiếp theo

1. Đi sâu ERD từng phần theo quyết định giữ hàng khi xác nhận, một kho chính và thu COD tối thiểu đã chốt; chưa cần giải quyết chính sách tài chính ngoài MVP.
2. Lập danh sách màn hình/thao tác và ma trận quyền; không cần viết React lúc này.
3. Thiết kế ERD, state machine, ràng buộc, chứng từ và quy tắc transaction.
4. Thiết kế hợp đồng API và tiêu chí kiểm thử gắn với nghiệp vụ.
5. Triển khai backend từng module, kiểm thử tích hợp trên SQL Server.
6. Bàn giao OpenAPI, hướng dẫn, dữ liệu mẫu và backend chạy được cho frontend.
