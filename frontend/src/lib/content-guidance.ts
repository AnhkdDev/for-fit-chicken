// Keep these notes aligned with the actual public consumers, not the CMS schema alone.
export const contentGuidance: Record<
  string,
  { placement: string; writing: string; links: boolean; images: boolean; archived?: boolean }
> = {
  HomeHero: {
    placement:
      'Hiển thị ở banner đầu trang chủ. Tiêu đề, mô tả và nút chính dùng chung cho tất cả slide; ảnh thay đổi theo thứ tự đã chọn.',
    writing:
      'Gợi ý: tiêu đề khoảng 6–10 từ, mô tả 1–2 câu ngắn. Website tự xuống dòng theo màn hình; không chèn khoảng trắng để căn chữ. Nút phụ cố định “Câu chuyện của chúng mình” rút thành “Câu chuyện” trên điện thoại; dòng tiếng Anh cũng cố định, không chỉnh tại đây.',
    links: true,
    images: true,
  },
  Story: {
    placement:
      'Dùng chung cho câu chuyện cuối trang chủ và trang /cau-chuyen. Trang chủ chỉ lấy đoạn giới thiệu tối đa 240 ký tự; trang câu chuyện hiển thị toàn bộ nội dung.',
    writing:
      'Viết đầy đủ câu chuyện ở đây, xuống dòng để tách đoạn. Ảnh đầu là ảnh chính trang chủ; ảnh 2–3 là ảnh phụ. Trang câu chuyện hiển thị tất cả ảnh. Slogan, nhãn trang trí, chữ trên thẻ ảnh phụ, lời dẫn và tiêu đề phụ trong trang câu chuyện là nội dung cố định. Nút “Xem thêm” luôn mở /cau-chuyen; trường đường dẫn/nút cũ không được sử dụng và được giữ nguyên khi lưu.',
    links: false,
    images: true,
  },
  Faq: {
    placement:
      'Tiêu đề và nội dung hiển thị trong khối bổ sung ở cuối trang /cau-chuyen, không nằm trên trang chủ.',
    writing:
      'Đây là một đoạn thông tin bổ sung, không phải trình sửa từng câu hỏi. Các câu hỏi mở/đóng bên dưới hiện cố định trong giao diện. Ảnh và nút cũ không hiển thị, được giữ nguyên khi lưu.',
    links: false,
    images: false,
  },
  Contact: {
    placement: 'Hiển thị ở cột liên hệ trong chân trang của các trang khách hàng.',
    writing:
      'Nhập thông tin liên hệ thật. Có thể đặt chữ nút và đường dẫn https:// đến kênh liên hệ. Ảnh cũ không được dùng ở chân trang và được giữ nguyên khi lưu.',
    links: true,
    images: false,
  },
  Usage: {
    placement:
      'Mục lưu trữ: hiện chưa có vị trí hiển thị trên giao diện khách hàng. Lưu hoặc công bố mục này không thêm một khu vực vào trang chủ.',
    writing:
      'Để sửa hướng dẫn khách đang đọc, vào Sản phẩm → chọn sản phẩm → Thông tin chi tiết, dinh dưỡng & bảo quản → Hướng dẫn sử dụng. Dữ liệu cũ tại đây vẫn được giữ.',
    links: false,
    images: false,
    archived: true,
  },
  SiteSettings: {
    placement:
      'Mục lưu trữ: giao diện và thông tin SEO hiện chưa đọc cấu hình này. Công bố ở đây không thay logo, màu sắc, menu hoặc tiêu đề tìm kiếm.',
    writing:
      'Logo, màu, bố cục, năm bản quyền và nội dung cố định cần được cập nhật trong mã nguồn. Không nhập khóa R2, Telegram, mật khẩu hoặc thông tin bí mật vào nội dung website.',
    links: false,
    images: false,
    archived: true,
  },
};
