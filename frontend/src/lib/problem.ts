export function problemMessage(problem: Record<string, unknown>, status: number) {
  const messages: Record<string, string> = {
    VERSION_CONFLICT: 'Dữ liệu đã thay đổi. Đóng hộp thoại, tải lại danh sách rồi thử lại.',
    RATE_LIMIT_EXCEEDED: 'Bạn thao tác hơi nhanh. Vui lòng chờ rồi thử lại.',
    LOGIN_FAILED: 'Không đăng nhập được. Kiểm tra tài khoản và mật khẩu.',
    CSRF_INVALID: 'Phiên bảo mật đã đổi. Vui lòng thử lại.',
    INSUFFICIENT_STOCK: 'Số lượng khả dụng đã thay đổi. Vui lòng kiểm tra lại giỏ hàng.',
  };
  const code = typeof problem.code === 'string' ? problem.code : '';
  const validation =
    problem.errors && typeof problem.errors === 'object'
      ? Object.values(problem.errors)
          .flat()
          .filter((v) => typeof v === 'string')
          .join(' ')
      : '';
  return (
    messages[code] ||
    (typeof problem.detail === 'string' && problem.detail.trim()) ||
    validation ||
    (typeof problem.title === 'string' && problem.title.trim()) ||
    (status === 403
      ? 'Tài khoản không có quyền thực hiện thao tác này.'
      : 'Không thể thực hiện yêu cầu. Vui lòng kiểm tra và thử lại.')
  );
}
