import { CookingPot, Package, ShoppingCart, Refrigerator, MessagesSquare } from 'lucide-react';
import './home-transition.css';

const highlights = [
  {
    icon: CookingPot,
    title: 'Nấu chậm Sous Vide',
    shortTitle: 'Nấu chậm',
    body: 'Mềm mọng, đậm đà',
  },
  {
    icon: Package,
    title: 'Đóng gói tiện lợi',
    shortTitle: 'Tiện lợi',
    body: 'Gọn cho bữa ăn mỗi ngày',
  },
  {
    icon: ShoppingCart,
    title: 'Đặt hàng dễ dàng',
    shortTitle: 'Đặt hàng',
    body: 'Nhóm gọi xác nhận đơn',
  },
  {
    icon: Refrigerator,
    title: 'Sẵn sàng thưởng thức',
    shortTitle: 'Thưởng thức',
    body: 'Theo hướng dẫn trên nhãn',
  },
  {
    icon: MessagesSquare,
    title: 'Lắng nghe bạn',
    shortTitle: 'Lắng nghe',
    body: 'Luôn đón nhận góp ý',
  },
];
export function HomeTransition() {
  return (
    <section className="home-highlights" aria-label="Điểm nổi bật của ForFit">
      <ul className="home-highlights-list" aria-label="Năm điểm nổi bật">
        {highlights.map(({ icon: Icon, title, shortTitle, body }) => (
          <li key={title}>
            <Icon aria-hidden="true" />
            <div>
              <h2>
                <span className="highlight-title-full">{title}</span>
                <span className="highlight-title-short">{shortTitle}</span>
              </h2>
              <p>{body}</p>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}
