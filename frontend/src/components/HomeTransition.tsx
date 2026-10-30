import { CookingPot, Package, ShoppingCart, Refrigerator, MessagesSquare } from 'lucide-react';
import './home-transition.css';

const highlights = [
  { icon: CookingPot, title: 'Nấu chậm Sous Vide', body: 'Mềm mọng, đậm đà từng miếng' },
  { icon: Package, title: 'Đóng gói tiện lợi', body: 'Gọn gàng cho bữa ăn mỗi ngày' },
  { icon: ShoppingCart, title: 'Đặt hàng dễ dàng', body: 'Chọn vị, đặt đơn, nhóm gọi xác nhận' },
  { icon: Refrigerator, title: 'Sẵn sàng thưởng thức', body: 'Dùng ngay theo hướng dẫn trên nhãn' },
  { icon: MessagesSquare, title: 'Lắng nghe bạn', body: 'Góp ý giúp chúng mình tốt hơn' },
];
export function HomeTransition() {
  return (
    <section className="home-highlights" aria-label="Điểm nổi bật của ForFit">
      <ul className="home-highlights-list">
        {highlights.map(({ icon: Icon, title, body }) => (
          <li key={title}>
            <Icon aria-hidden="true" />
            <div><h2>{title}</h2><p>{body}</p></div>
          </li>
        ))}
      </ul>
    </section>
  );
}
