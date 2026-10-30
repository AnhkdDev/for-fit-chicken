export function productSlug(name: string) {
  return name
    .trim()
    .toLowerCase()
    .replace(/đ/g, 'd')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 200)
    .replace(/-+$/, '');
}
export const sectionNames: Record<string, string> = {
  HomeHero: 'Banner trang chủ',
  Story: 'Câu chuyện thương hiệu',
  Usage: 'Hướng dẫn sử dụng',
  Faq: 'Câu hỏi thường gặp',
  Contact: 'Thông tin liên hệ',
  SiteSettings: 'Thông tin chung',
};
export const orderNames: Record<string, string> = {
  PendingConfirmation: 'Chờ gọi xác nhận',
  Confirmed: 'Đã xác nhận',
  Preparing: 'Đang chuẩn bị',
  HandedOver: 'Đã bàn giao',
  Completed: 'Hoàn tất',
  Cancelled: 'Đã hủy',
};
