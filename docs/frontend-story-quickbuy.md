# Câu chuyện và mua nhanh trang chủ

29/10/2026. Theo yêu cầu người dùng: cụm câu chuyện + trưng bày sản phẩm ngay dưới, bấm sản phẩm mở popup.

- Câu chuyện lấy Story CMS title/body/assetIds; không dùng ảnh người dân/vùng nguyên liệu A Sín làm câu chuyện ForFit.
- Ảnh đầu là ảnh chính; ảnh thứ hai/ba cho khung phụ. Chưa có ảnh thật thì dùng logo và thẻ nội dung minh họa bằng CSS,
  không bịa chứng nhận, đánh giá hay thống kê. CTA dẫn /cau-chuyen.
- Dải ngang lấy sản phẩm public đã publish. Không seed sản phẩm vào DB; loading/error/empty cần hiển thị rõ.
- Bấm ảnh/tên sản phẩm mở dialog mua nhanh. GET detail lại để lấy SKU/giá/khả dụng mới, không cho mua từ snapshot đang tải.
- Chọn SKU + số gói; tính cả lượng đã có trong giỏ, tối đa min(100,available), tổng 20 phân loại. Không giữ hàng khi thêm giỏ.
- Chỉ lưu variantId/quantity; giá cuối do server tính khi đặt COD. Không gửi đơn/Telegram chỉ từ nút thêm giỏ.
- Dialog native: focus trap/Escape/đóng backdrop/khôi phục focus; mobile một cột, ảnh không đẩy CTA khỏi tầm với quá nhiều.
- Không gift box, gateway, QR hay shipping mới. Trang chi tiết sản phẩm độc lập vẫn có đường dẫn.

Kiểm chứng và trạng thái xem frontend-progress.md. Môi trường fixture không phải dữ liệu bán hàng thật.

Đã kiểm tra browser desktop/mobile: chọn SKU 200g 48000 VND + 3 gói, thêm SKU khác 37000 VND + 2 gói,
giỏ 5 gói/218000 VND; hết hàng disabled; còn5 nhưng nhập6 disabled; Escape trả focus card; nút rail cuộn ngang.
Ảnh xem trước quick-buy-desktop.jpg / quick-buy-mobile.jpg / story-products-desktop.jpg ở frontend-previews.
