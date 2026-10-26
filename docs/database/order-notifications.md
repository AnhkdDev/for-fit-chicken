# Thông báo đơn mới qua Telegram

Ngày 26/10/2026. Người dùng đã chọn Telegram làm kênh nhận thông báo nội bộ. Chỉ chốt kênh; retry, bảng job và vận hành bên dưới là đề xuất thiết kế. Chưa tạo bot, cấu hình nhóm, gửi tin hoặc chạy worker.

## 1. Phạm vi

- Khách gửi yêu cầu COD hợp lệ -> thông báo nhóm Telegram có mã đơn, số gói, tiền hàng và link admin.
- Link yêu cầu đăng nhập; mã đơn hoặc tin nhắn không thay quyền truy cập. Không đưa token/PII vào URL.
- Không thao tác xác nhận/hủy/thu tiền trực tiếp trong Telegram. Không cần webhook/polling nhận lệnh cho luồng chỉ gửi sau khi đã cấu hình chat.
- Telegram không thay thế nguồn dữ liệu Orders. API gửi thành công không chứng minh có người đọc, bật chuông hay đã gọi khách.
- Nhận người phụ trách và nhắc đơn chậm đã được đề xuất trong chat nhưng chưa coi là người dùng chốt; chưa tự thêm AssignedTo fields hoặc scheduler nhắc.

## 2. Bảng NotificationJobs

| Trường | Ý nghĩa |
| --- | --- |
| Id | Khóa job |
| OrderId | Đơn liên quan |
| EventType | MVP OrderRequested |
| Channel | MVP Telegram |
| TargetChatId | Định danh nhóm nhận snapshot, bigint có thể âm |
| PayloadJson | Snapshot tối thiểu mã đơn/số gói/tiền hàng; không tên/điện thoại/địa chỉ |
| Status | Pending, Processing, Sent, Failed |
| AttemptCount | Số lần thử |
| NextAttemptAtUtc | Lần thử tiếp theo |
| LeaseToken, LeaseUntilUtc | Claim job có thời hạn để phối hợp worker |
| LastError | Lỗi đã lọc secret và dữ liệu nhạy cảm |
| TelegramMessageId | Mã tin nếu API gửi trả thành công |
| CreatedAtUtc, SentAtUtc | Mốc tạo và API chấp nhận |

Unique(OrderId,EventType,Channel,TargetChatId) ngăn tạo nhiều job cho cùng sự kiện/nhóm. Index(Status,NextAttemptAtUtc) và lease phục vụ claim/retry. Channel/event có CHECK, không dùng làm platform đa kênh quá mức ngay từ đầu.

## 3. Transaction và retry

1. Tạo Orders/Items/Event/Idempotency result và NotificationJob trong cùng transaction SQL. Lỗi insert job khi bật chức năng là lỗi transaction nội bộ; không được commit đơn rồi bỏ job âm thầm.
2. Chỉ sau commit worker mới claim và gọi Telegram ngoài transaction. Client không đợi Telegram; Telegram lỗi không đổi đơn đã lưu thành thất bại.
3. Claim phải atomic, có lease và attempt token. Worker cập nhật kết quả chỉ khi vẫn giữ đúng token; không giữ lock SQL trong lúc gọi mạng.
4. Crash/restart: lease hết hạn cho retry job chưa xong, không mất job. Request gửi đơn lặp replay đơn cũ, không tạo job mới.
5. Lỗi tạm retry backoff có giới hạn, 429 theo retry_after. Token/quyền nhóm lỗi vĩnh viễn -> Failed và hiển thị để nhóm cấu hình lại; không retry nóng vô hạn.
6. Manual retry cùng job sau khắc phục, có audit; không tạo bản sao sự kiện mới. Sau nhiều lỗi cần cơ chế vận hành nhìn thấy lỗi, không khẳng định thông báo không bao giờ mất.

Telegram có thể nhận tin nhưng response mất; retry có thể gửi trùng. Unique job/token giảm trùng phía app nhưng không bảo đảm exactly-once phía Telegram. Tin luôn có mã đơn để nhận diện; không giảm kiểm tra idempotency đơn vì đã có thông báo.

## 4. Cấu hình và vận hành

- BotToken ở secret store/env backend, không DBML plaintext, frontend, Git hoặc log URL gọi API chứa token. ChatId không phải mật khẩu nhưng chỉ admin nội bộ quản lý.
- Một nhóm nội bộ ban đầu; không tự cấp bot quyền admin ngoài những gì cần gửi tin. Trước mở bán phải thử quyền và cấu hình nhóm nhận cụ thể.
- Worker cần môi trường chạy nền liên tục hoặc cơ chế đánh thức đáng tin cậy. Hosting sleep thì không thể hứa báo ngay, dù không ai mở web.
- Link admin dùng domain HTTPS thực tế, không localhost trong production. Payload không chứa nội dung chưa kiểm soát gây định dạng/link injection; escape khi render.
- Nếu đơn đã hủy/xử lý trước khi worker gửi, nội dung phải nêu trạng thái hiện hành hoặc không phát lời kêu gọi gọi khách cũ sai; quy tắc cụ thể chốt khi làm template.
- Admin có trạng thái gửi/lỗi và nút retry theo quyền; nhóm bật thông báo trên thiết bị. Dashboard đơn pending vẫn là fallback, không phải cần người trực liên tục.

## 5. Nghiệm thu

- API submit đơn thành công khi Telegram đang lỗi, job vẫn tồn tại để thử lại.
- Crash trước commit không có nửa đơn/nửa job; crash sau commit job được xử lý khi worker trở lại.
- Hai worker không claim cùng lease; token cũ không ghi đè kết quả mới. Timeout nhận diện có thể lặp tin, không tạo thêm đơn.
- Không lộ token/PII qua tin, log hoặc API public. Link mở đúng đơn nhưng vẫn cần quyền.
- Failed hiển thị, retry được sau khắc phục; thông báo không giữ/xuất hàng hoặc xác nhận đơn.

Tài liệu kỹ thuật đã đối chiếu trong chat: [Telegram sendMessage](https://core.telegram.org/bots/api#sendmessage), [tạo bot](https://core.telegram.org/bots/tutorial#obtain-your-bot-token).
