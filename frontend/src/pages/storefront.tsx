import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Link, NavLink, Outlet, useLocation, useParams } from 'react-router-dom';
import {
  ArrowDown,
  ArrowUpRight,
  Check,
  ChevronDown,
  Clock3,
  Dumbbell,
  MapPin,
  Menu,
  Minus,
  Plus,
  Search,
  ShieldCheck,
  ShoppingBag,
  Trash2,
  Truck,
  X,
} from 'lucide-react';
import { api, money, safeLink, send } from '../lib/api';
import { useShop } from '../lib/state';
import type { Product } from '../lib/types';
import { Arrow, Empty, ErrorBox, Loading } from '../components/ui';
import { BrandLogo } from '../components/BrandLogo';
import { HomeProducts } from '../components/HomeProducts';
import { HomeTransition } from '../components/HomeTransition';
import { StoryShowcase } from '../components/StoryShowcase';

const nav = [
  ['/', 'Trang chủ'],
  ['/san-pham', 'Sản phẩm'],
  ['/cau-chuyen', 'Câu chuyện'],
  ['/diem-ban', 'Điểm bán'],
];
function Logo() {
  return <BrandLogo />;
}
export function StoreLayout() {
  const [menu, setMenu] = useState(false);
  const { cart, toast, sections } = useShop();
  const { pathname } = useLocation();
  useEffect(() => {
    setMenu(false);
  }, [pathname]);
  useEffect(() => {
    function key(e: KeyboardEvent) {
      if (e.key === 'Escape') setMenu(false);
    }
    window.addEventListener('keydown', key);
    return () => window.removeEventListener('keydown', key);
  }, []);
  const contact = sections.find((s) => s.sectionKey === 'Contact')?.content;
  return (
    <>
      <a className="skip-link" href="#main">
        Đến nội dung
      </a>
      <div className="announcement">
        MỘT BỮA NGON NHỎ. MỘT NGÀY CHỦ ĐỘNG HƠN.{' '}
        <span>
          KHÁM PHÁ FORFIT <ArrowUpRight size={12} />
        </span>
      </div>
      <header className="site-header">
        <div className="container header-inner">
          <Logo />
          <nav className={menu ? 'main-nav open' : 'main-nav'} aria-label="Điều hướng chính">
            {nav.map(([url, label]) => (
              <NavLink key={url} to={url} end={url === '/'}>
                {label}
              </NavLink>
            ))}
          </nav>
          <div className="header-actions">
            <Link
              to="/gio-hang"
              className="cart-link"
              aria-label={`Giỏ hàng ${cart.reduce((n, c) => n + c.quantity, 0)} sản phẩm`}
            >
              <ShoppingBag size={21} />
              <span>{cart.reduce((n, c) => n + c.quantity, 0)}</span>
            </Link>
            <button
              className="icon-button mobile-menu"
              onClick={() => setMenu(!menu)}
              aria-expanded={menu}
              aria-label={menu ? 'Đóng menu' : 'Mở menu'}
            >
              {menu ? <X /> : <Menu />}
            </button>
          </div>
        </div>
      </header>
      <main id="main" className="storefront-main">
        <Outlet />
      </main>
      <footer>
        <div className="container footer-top">
          <div>
            <Logo />
            <p>
              Ăn ngon. Sống chủ động.
              <br />
              Ức gà nấu chậm dành cho nhịp sống của bạn.
            </p>
          </div>
          <div>
            <h4>Khám phá</h4>
            {nav.slice(1).map(([u, t]) => (
              <Link key={u} to={u}>
                {t}
              </Link>
            ))}
          </div>
          <div>
            <h4>Đặt hàng & hỗ trợ</h4>
            <Link to="/gio-hang">Giỏ hàng của bạn</Link>
            <span>Thanh toán khi nhận hàng (COD)</span>
            <span>Giao hàng tại Hà Nội</span>
          </div>
          <div>
            <h4>{contact?.title ?? 'Kết nối với ForFit'}</h4>
            <p className="plain-text">
              {contact?.body ?? 'Thông tin liên hệ sẽ được cập nhật trước khi mở bán.'}
            </p>
            {contact?.linkLabel && (
              <a href={safeLink(contact.linkUrl, '/diem-ban')}>{contact.linkLabel} ↗</a>
            )}
          </div>
        </div>
        <div className="container footer-bottom">
          <span>© {new Date().getFullYear()} ForFit Chicken</span>
          <span>Chậm trong cách nấu. Nhanh trong nhịp sống.</span>
          <Link to="/quan-tri">Dành cho đội ngũ ↗</Link>
        </div>
      </footer>
      {toast && (
        <div className="toast" role="status">
          <Check size={18} />
          {toast}
        </div>
      )}
    </>
  );
}
export function Home({
  displayConfiguration,
  displayItems,
  displayPreview = false,
}: {
  displayConfiguration?: import('../lib/types').Section['content'];
  displayItems?: import('../lib/types').Product[];
  displayPreview?: boolean;
} = {}) {
  const { sections, loading: homeLoading } = useShop();
  const hero = sections.find((s) => s.sectionKey === 'HomeHero')?.content;
  const [slide, setSlide] = useState(0);
  const slides = hero?.assetIds ?? [];
  const activeSlide = slides.length ? slide % slides.length : 0;
  const asset = slides[activeSlide];
  function changeSlide(delta: number) {
    setSlide((activeSlide + delta + slides.length) % slides.length);
  }
  return (
    <>
      {hero && (
        <section
          className={`hero ${asset ? 'custom-hero' : ''}`}
          aria-label="Banner thương hiệu"
          tabIndex={slides.length > 1 ? 0 : undefined}
          onKeyDown={(e) => {
            if (slides.length > 1 && e.key === 'ArrowRight') changeSlide(1);
            if (slides.length > 1 && e.key === 'ArrowLeft') changeSlide(-1);
          }}
        >
          {asset ? (
            <img
              key={asset}
              className="hero-image"
              src={`/api/media/${asset}`}
              alt="Hình ảnh thương hiệu ForFit Chicken"
              fetchPriority="high"
              decoding="async"
            />
          ) : (
            <div className="hero-placeholder" role="status">
              {homeLoading ? 'Đang tải banner…' : 'Chưa có banner được công bố'}
            </div>
          )}
          <div className="hero-shade" />
          <div className="container hero-content">
            <div className="hero-copy">
              <span className="eyebrow">
                <span className="line" /> SLOW COOKED. WELL LIVED.
              </span>
              <h1>
                {hero?.title ?? (
                  <>
                    Chậm một chút.
                    <br />
                    Ngon <em>mỗi ngày.</em>
                  </>
                )}
              </h1>
              <p>
                {hero?.body ??
                  'Ức gà nấu chậm, thêm một lựa chọn cho bữa ăn của người bận rộn. Dành thời gian cho những điều bạn yêu.'}
              </p>
              <div className="button-row">
                <a className="button" href={safeLink(hero?.linkUrl)}>
                  {hero?.linkLabel ?? 'Khám phá sản phẩm'}
                  <Arrow />
                </a>
                <Link to="/cau-chuyen" className="text-link">
                  Câu chuyện của chúng mình <ArrowUpRight size={17} />
                </Link>
              </div>
            </div>
            <div className="hero-foot">
              <span>FOR YOUR EVERYDAY MOMENT</span>
              <a href="#cau-chuyen" aria-label="Cuộn đến câu chuyện">
                <ArrowDown size={17} />
              </a>
            </div>
          </div>
          {slides.length > 1 && (
            <div className="hero-carousel-controls" aria-label="Chuyển ảnh banner">
              <button type="button" aria-label="Banner trước" onClick={() => changeSlide(-1)}>
                ←
              </button>
              {slides.map((id, i) => (
                <button
                  type="button"
                  key={id}
                  aria-label={`Xem banner ${i + 1}`}
                  aria-pressed={activeSlide === i}
                  onClick={() => setSlide(i)}
                >
                  {i + 1}
                </button>
              ))}
              <button type="button" aria-label="Banner tiếp theo" onClick={() => changeSlide(1)}>
                →
              </button>
              <span aria-live="polite">
                {activeSlide + 1}/{slides.length}
              </span>
            </div>
          )}
          <span className="hero-index">
            01 <span>/ FORFIT EVERYDAY</span>
          </span>
        </section>
      )}
      {!hero && homeLoading && (
        <div className="hero-loading" role="status">
          Đang tải banner…
        </div>
      )}
      <HomeTransition />
      <HomeProducts
        configuration={displayConfiguration}
        items={displayItems}
        preview={displayPreview}
      />
      <StoryShowcase />
    </>
  );
}
const faqs = [
  [
    'Đặt hàng trên website như thế nào?',
    'Chọn sản phẩm, thêm vào giỏ và điền thông tin nhận hàng. Sau khi gửi đơn, nhóm sẽ gọi lại để xác nhận trước khi chuẩn bị.',
  ],
  [
    'Có cần tạo tài khoản không?',
    'Không cần. Bạn có thể đặt hàng trực tiếp với tên, số điện thoại và địa chỉ tại Hà Nội.',
  ],
  [
    'Thanh toán và giao hàng ra sao?',
    'Hiện hỗ trợ thanh toán khi nhận hàng (COD), giao tại Hà Nội. Nhóm trao đổi chi tiết giao nhận khi gọi xác nhận. Website chưa tính phí giao hàng.',
  ],
  [
    'Sử dụng và bảo quản như thế nào?',
    'Hãy đọc hướng dẫn trên nhãn và trang chi tiết của sản phẩm bạn chọn. Thông tin bảo quản và hạn dùng phụ thuộc từng sản phẩm, lô hàng.',
  ],
];
function Faq() {
  const { sections } = useShop();
  const custom = sections.find((s) => s.sectionKey === 'Faq')?.content;
  return (
    <section className="section faq-section">
      <div className="container faq-grid">
        <div>
          <span className="eyebrow">CÓ THỂ BẠN ĐANG TÒ MÒ</span>
          <h2>
            Một vài
            <br />
            <em>điều nho nhỏ.</em>
          </h2>
        </div>
        <div>
          {custom && (
            <div className="faq-custom">
              <h3>{custom.title}</h3>
              <p className="plain-text">{custom.body}</p>
            </div>
          )}
          {faqs.map(([q, a]) => (
            <details key={q}>
              <summary>
                {q}
                <Plus size={18} />
              </summary>
              <p>{a}</p>
            </details>
          ))}
        </div>
      </div>
    </section>
  );
}
export function ProductCard({ p }: { p: Product }) {
  const { add } = useShop();
  const v =
    p.variants.find((v) => v.isActive && v.available > 0) ?? p.variants.find((v) => v.isActive);
  return (
    <article className="product-card">
      <Link to={`/san-pham/${p.id}`} className="product-image">
        {p.images[0] ? (
          <img src={p.images[0].url} alt={p.images[0].altText ?? p.name} loading="lazy" />
        ) : (
          <div className="product-placeholder">
            <ShoppingBag size={55} strokeWidth={0.7} />
            <span>FORFIT CHICKEN</span>
          </div>
        )}
        <span className="product-tag">{p.isFeatured ? 'FORFIT SELECTED' : 'SLOW COOKED'}</span>
        <span className="product-arrow">
          <ArrowUpRight size={21} />
        </span>
      </Link>
      <div className="product-meta">
        <span>{v?.netWeightGrams ? `${v.netWeightGrams} g / GÓI` : 'ỨC GÀ NẤU CHẬM'}</span>
        <span>{v?.available ? 'Đang có hàng' : 'Tạm hết hàng'}</span>
      </div>
      <h3>
        <Link to={`/san-pham/${p.id}`}>{p.name}</Link>
      </h3>
      <p>{p.shortDescription}</p>
      <div className="product-bottom">
        <b>{v ? money(v.sellingPrice) : 'Đang cập nhật'}</b>
        <button
          className="small-button"
          disabled={!v || v.available <= 0}
          onClick={() => v && add(v.id)}
        >
          Thêm vào giỏ <Plus size={16} />
        </button>
      </div>
    </article>
  );
}
export function Products() {
  const { products, loading, error, reload } = useShop();
  const [search, setSearch] = useState('');
  const [sort, setSort] = useState('featured');
  const filtered = useMemo(() => {
    const list = products.filter((p) =>
      p.name.toLocaleLowerCase('vi').includes(search.toLocaleLowerCase('vi')),
    );
    const price = (p: Product) =>
      Math.min(...p.variants.filter((v) => v.isActive).map((v) => v.sellingPrice));
    return list.sort((a, b) =>
      sort === 'low'
        ? price(a) - price(b)
        : sort === 'high'
          ? price(b) - price(a)
          : Number(b.isFeatured) - Number(a.isFeatured),
    );
  }, [products, search, sort]);
  return (
    <div className="container page">
      <div className="page-intro">
        <span className="eyebrow">THE EVERYDAY COLLECTION</span>
        <h1>
          Bữa ngon, <em>theo ý bạn.</em>
        </h1>
        <p>
          Khám phá ức gà nấu chậm của ForFit. Chọn sản phẩm phù hợp với khẩu vị và nhịp sống của
          bạn.
        </p>
      </div>
      <div className="catalog-toolbar">
        <label className="search-field">
          <Search size={18} />
          <input
            placeholder="Tìm một món ngon…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            aria-label="Tìm sản phẩm"
          />
        </label>
        <label className="sort-field">
          Sắp xếp{' '}
          <select value={sort} onChange={(e) => setSort(e.target.value)}>
            <option value="featured">Nổi bật</option>
            <option value="low">Giá tăng dần</option>
            <option value="high">Giá giảm dần</option>
          </select>
          <ChevronDown size={14} />
        </label>
      </div>
      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorBox text={error} retry={reload} />
      ) : filtered.length ? (
        <div className="product-grid">
          {filtered.map((p) => (
            <ProductCard key={p.id} p={p} />
          ))}
        </div>
      ) : (
        <Empty title={search ? 'Chưa tìm thấy món phù hợp' : 'Sản phẩm đang được chuẩn bị'}>
          <p>
            {search
              ? 'Thử tìm bằng tên khác nhé.'
              : 'Nhóm sẽ cập nhật danh mục khi sẵn sàng mở bán.'}
          </p>
        </Empty>
      )}
    </div>
  );
}
export function ProductDetail() {
  const { id } = useParams();
  const { add } = useShop();
  const [p, setP] = useState<Product>();
  const [error, setError] = useState('');
  const [selected, setSelected] = useState<number>();
  const [image, setImage] = useState(0);
  useEffect(() => {
    setP(undefined);
    setError('');
    api<Product>(`/products/${id}`)
      .then((p) => {
        setP(p);
        setSelected(p.variants.find((v) => v.isActive)?.id);
      })
      .catch((e) => setError(e.message));
  }, [id]);
  if (error)
    return (
      <div className="container page">
        <ErrorBox text={error} />
        <Link to="/san-pham" className="text-link">
          Về sản phẩm <Arrow />
        </Link>
      </div>
    );
  if (!p) return <Loading />;
  const v = p.variants.find((v) => v.id === selected);
  return (
    <div className="container page">
      <div className="breadcrumb">
        <Link to="/san-pham">Sản phẩm</Link> / {p.name}
      </div>
      <div className="detail-grid">
        <div>
          <div className="detail-image">
            {p.images[image] ? (
              <img src={p.images[image].url} alt={p.images[image].altText ?? p.name} />
            ) : (
              <ShoppingBag size={80} strokeWidth={0.7} />
            )}
          </div>
          <div className="thumbs">
            {p.images.map((im, i) => (
              <button
                key={im.id}
                className={image === i ? 'selected' : ''}
                onClick={() => setImage(i)}
                aria-label={`Xem ảnh ${i + 1}`}
              >
                <img src={im.url} alt="" />
              </button>
            ))}
          </div>
        </div>
        <div className="detail-copy">
          <span className="eyebrow">FORFIT / SLOW COOKED</span>
          <h1>{p.name}</h1>
          <p>{p.shortDescription}</p>
          <h2>{v && money(v.sellingPrice)}</h2>
          <div className="variant-options">
            {p.variants
              .filter((v) => v.isActive)
              .map((x) => (
                <button
                  key={x.id}
                  className={selected === x.id ? 'selected' : ''}
                  onClick={() => setSelected(x.id)}
                >
                  {x.label} · {x.netWeightGrams}g
                </button>
              ))}
          </div>
          <p className="muted">
            {v?.available ? `Khả dụng online: ${v.available} gói` : 'Sản phẩm tạm hết hàng'}
          </p>
          <button className="button full" disabled={!v?.available} onClick={() => v && add(v.id)}>
            Thêm vào giỏ hàng <Plus size={19} />
          </button>
          <div className="detail-notes">
            <span>
              <Truck size={17} /> Giao tại Hà Nội
            </span>
            <span>
              <ShieldCheck size={17} /> COD · Gọi xác nhận trước
            </span>
          </div>
          {[
            ['Thông tin sản phẩm', p.description],
            ['Thành phần', p.ingredients],
            ['Thông tin dinh dưỡng', p.nutritionText],
            ['Hướng dẫn sử dụng', p.usageInstructions],
            ['Hướng dẫn bảo quản', p.storageInstructions],
          ].map(
            ([title, body]) =>
              body && (
                <details key={title}>
                  <summary>
                    {title}
                    <Plus size={17} />
                  </summary>
                  <p className="plain-text">{body}</p>
                </details>
              ),
          )}
        </div>
      </div>
    </div>
  );
}
export function Story() {
  const { sections } = useShop();
  const s = sections.find((s) => s.sectionKey === 'Story')?.content;
  const paragraphs = (s?.body ??
    'ForFit bắt đầu từ nhóm bốn người cùng xây dựng ý tưởng ức gà nấu chậm. Chúng mình chọn bắt đầu bằng sản phẩm, lắng nghe khách hàng và đưa sản phẩm đến gần cộng đồng tập luyện qua các phòng gym.')
    .split(/\r?\n/).map((paragraph) => paragraph.trim()).filter(Boolean);
  return (
    <>
      <div className="container page story-page">
        <header className="story-page-heading">
        <span className="eyebrow">NHỮNG NGƯỜI PHÍA SAU BỮA ĂN</span>
        <h1>
          {s?.title ?? (
            <>
              Từ một ý tưởng nhỏ,
              <br />
              đến những bữa ăn <em>thường ngày.</em>
            </>
          )}
        </h1>
        <p className="story-page-subtitle">Bốn người, một ý tưởng nhỏ và mong muốn mang bữa ăn tiện lợi đến gần bạn hơn.</p>
        </header>
        <div className="story-page-grid">
          <blockquote className="story-quote">
            <span className="eyebrow">ĐIỀU CHÚNG MÌNH THEO ĐUỔI</span>
            “Chậm trong cách nấu.
            <br />
            <em>Chủ động trong cách sống.</em>”
          </blockquote>
          <div className="story-page-copy">
            <h2>Bắt đầu từ bữa ăn thường ngày</h2>
            {paragraphs.slice(0, 2)
              .map((paragraph, index) => <p key={index}>{paragraph}</p>)}
          </div>
        </div>
        <div className="story-page-copy story-page-continuation">
            {paragraphs.slice(2).map((paragraph, index) => <p key={index}>{paragraph}</p>)}
            <p>
              Bạn có thể tìm hiểu sản phẩm trực tuyến, đặt hàng không cần tài khoản hoặc ghé các
              điểm bán được công bố. Nhóm sẽ trực tiếp gọi xác nhận mỗi đơn online trước khi chuẩn
              bị.
            </p>
            <Link className="button" to="/san-pham">
              Khám phá sản phẩm <Arrow />
            </Link>
          </div>
        <div className="story-page-gallery">
        {s?.assetIds?.map((id) => (
          <img
            className="story-wide"
            key={id}
            src={`/api/media/${id}`}
            alt="Câu chuyện thương hiệu ForFit"
            loading="lazy"
            decoding="async"
          />
        ))}
        </div>
      </div>
      <Faq />
    </>
  );
}
export function GymPage() {
  const { gyms, loading, error, reload } = useShop();
  const [search, setSearch] = useState('');
  const filtered = gyms.filter((g) =>
    `${g.name} ${g.publicAddress}`.toLocaleLowerCase('vi').includes(search.toLocaleLowerCase('vi')),
  );
  return (
    <div className="container page">
      <div className="page-intro">
        <span className="eyebrow">FORFIT, GẦN BẠN HƠN</span>
        <h1>
          Ghé tập.
          <br />
          Ghé lấy <em>bữa ngon.</em>
        </h1>
        <p>
          Tìm ForFit tại các phòng gym ở Hà Nội. Liên hệ điểm bán trước khi ghé để kiểm tra sản phẩm
          đang có.
        </p>
      </div>
      <label className="search-field wide">
        <MapPin size={19} />
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Tìm phòng gym, địa chỉ…"
          aria-label="Tìm điểm bán"
        />
      </label>
      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorBox text={error} retry={reload} />
      ) : filtered.length ? (
        <div className="gym-grid">
          {filtered.map((g) => (
            <article className="gym-card" key={g.id}>
              <div className="gym-card-image">
                {g.coverAssetId ? (
                  <img src={`/api/media/${g.coverAssetId}`} alt={g.name} loading="lazy" />
                ) : (
                  <Dumbbell size={60} strokeWidth={0.8} />
                )}
                <span className="product-tag">ĐIỂM BÁN FORFIT</span>
              </div>
              <div>
                <h3>{g.name}</h3>
                <p>
                  <MapPin size={16} />
                  {g.publicAddress}
                </p>
                <p>{g.publicDescription}</p>
                <div className="button-row">
                  <a
                    className="text-link"
                    href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(g.publicAddress)}`}
                    target="_blank"
                    rel="noreferrer"
                  >
                    Chỉ đường <ArrowUpRight size={17} />
                  </a>
                  {g.publicPhone && (
                    <a className="text-link" href={`tel:${g.publicPhone.replace(/[^+0-9]/g, '')}`}>
                      Liên hệ điểm bán
                    </a>
                  )}
                </div>
              </div>
            </article>
          ))}
        </div>
      ) : (
        <Empty title="Điểm bán đang được cập nhật">
          <p>Bạn vẫn có thể khám phá và đặt hàng trực tiếp trên website.</p>
          <Link className="text-link" to="/san-pham">
            Xem sản phẩm <Arrow />
          </Link>
        </Empty>
      )}
    </div>
  );
}
function useCartRows() {
  const { cart, products } = useShop();
  return cart.map((c) => {
    const p = products.find((p) => p.variants.some((v) => v.id === c.variantId));
    return { ...c, p, v: p?.variants.find((v) => v.id === c.variantId) };
  });
}
export function Cart() {
  const { cart, setCart, loading, error, reload } = useShop();
  const rows = useCartRows();
  const total = rows.reduce((s, r) => s + (r.v?.sellingPrice ?? 0) * r.quantity, 0);
  const invalid = rows.some((r) => !r.v || !r.v.isActive || r.quantity > r.v.available);
  return (
    <div className="container page">
      <div className="page-intro compact">
        <span className="eyebrow">MỘT BƯỚC GẦN HƠN ĐẾN BỮA NGON</span>
        <h1>
          Giỏ hàng <em>của bạn.</em>
        </h1>
      </div>
      {loading ? (
        <Loading />
      ) : error ? (
        <ErrorBox text={error} retry={reload} />
      ) : !cart.length ? (
        <Empty title="Giỏ hàng đang đợi một món ngon">
          <Link className="button" to="/san-pham">
            Khám phá sản phẩm <Arrow />
          </Link>
        </Empty>
      ) : (
        <div className="cart-grid">
          <div>
            {rows.map((r) => (
              <article className="cart-row" key={r.variantId}>
                {r.p?.images[0] ? (
                  <img src={r.p.images[0].url} alt={r.p.name} />
                ) : (
                  <div className="cart-image-placeholder">
                    <ShoppingBag />
                  </div>
                )}
                <div>
                  <h3>{r.p?.name ?? 'Sản phẩm không còn khả dụng'}</h3>
                  <p>{r.v?.label}</p>
                  <strong>{money(r.v?.sellingPrice ?? 0)}</strong>
                  {(!r.v || r.quantity > r.v.available) && (
                    <small className="warning-text">Vui lòng cập nhật hoặc bỏ sản phẩm này.</small>
                  )}
                  <div className="quantity">
                    <button
                      aria-label="Giảm số lượng"
                      onClick={() =>
                        setCart(
                          cart.map((c) =>
                            c.variantId === r.variantId
                              ? { ...c, quantity: Math.max(1, c.quantity - 1) }
                              : c,
                          ),
                        )
                      }
                    >
                      <Minus size={14} />
                    </button>
                    <span>{r.quantity}</span>
                    <button
                      aria-label="Tăng số lượng"
                      disabled={!r.v || r.quantity >= Math.min(100, r.v.available)}
                      onClick={() =>
                        setCart(
                          cart.map((c) =>
                            c.variantId === r.variantId ? { ...c, quantity: c.quantity + 1 } : c,
                          ),
                        )
                      }
                    >
                      <Plus size={14} />
                    </button>
                  </div>
                </div>
                <button
                  className="icon-button remove"
                  aria-label={`Bỏ ${r.p?.name ?? 'sản phẩm'}`}
                  onClick={() => setCart(cart.filter((c) => c.variantId !== r.variantId))}
                >
                  <Trash2 size={18} />
                </button>
              </article>
            ))}
          </div>
          <aside className="order-summary">
            <span className="eyebrow">ĐƠN HÀNG CỦA BẠN</span>
            <h3>Một bữa ngon sắp đến.</h3>
            <div>
              <span>Tiền hàng dự kiến</span>
              <strong>{money(total)}</strong>
            </div>
            <p>Website chưa tính phí giao hàng. Nhóm sẽ trao đổi chi tiết khi gọi xác nhận.</p>
            {invalid ? (
              <ErrorBox text="Một số sản phẩm không còn đủ hàng. Hãy điều chỉnh giỏ trước khi đặt." />
            ) : (
              <Link className="button full" to="/dat-hang">
                Tiếp tục đặt hàng <Arrow />
              </Link>
            )}
            <Link className="text-link centered" to="/san-pham">
              Chọn thêm món
            </Link>
          </aside>
        </div>
      )}
    </div>
  );
}
export function Checkout() {
  const { cart, setCart, loading, error: storeError } = useShop();
  const rows = useCartRows();
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<{ orderNumber: string; itemsSubtotal: number }>();
  const [review, setReview] = useState(false);
  const [form, setForm] = useState({
    recipientName: '',
    recipientPhone: '',
    recipientAddress: '',
    customerNote: '',
  });
  useEffect(() => {
    window.scrollTo({ top: 0, behavior: 'instant' });
  }, [review, result]);
  const attempt = useRef<{ payload: string; key: string } | undefined>(undefined);
  const total = rows.reduce((s, r) => s + (r.v?.sellingPrice ?? 0) * r.quantity, 0);
  const invalid = rows.some((r) => !r.v || !r.v.isActive || r.quantity > r.v.available);
  async function place(e: FormEvent) {
    e.preventDefault();
    if (!review) {
      setReview(true);
      return;
    }
    setBusy(true);
    setError('');
    const payload = {
      ...form,
      items: cart.map((c) => ({ variantId: c.variantId, quantity: c.quantity })),
    };
    const serialized = JSON.stringify(payload);
    if (attempt.current?.payload !== serialized)
      attempt.current = { payload: serialized, key: crypto.randomUUID() };
    try {
      const r = await send<{ orderNumber: string; itemsSubtotal: number }>(
        '/orders',
        'POST',
        payload,
        { 'Idempotency-Key': attempt.current!.key },
      );
      setResult(r);
      setCart([]);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  if (result)
    return (
      <div className="container page confirmation">
        <span className="confirmation-icon">
          <Check size={40} />
        </span>
        <span className="eyebrow">CẢM ƠN BẠN ĐÃ CHỌN FORFIT</span>
        <h1>
          Đã nhận <em>yêu cầu đặt hàng.</em>
        </h1>
        <p>
          Mã đơn: <strong>{result.orderNumber}</strong>
        </p>
        <p>
          Tiền hàng: <strong>{money(result.itemsSubtotal)}</strong>
        </p>
        <p>
          Nhóm sẽ gọi lại để xác nhận thông tin, lượng hàng và giao nhận. Đơn chưa được xác nhận ở
          bước này.
        </p>
        <Link className="button" to="/">
          Về trang chủ <Arrow />
        </Link>
      </div>
    );
  if (loading) return <Loading />;
  if (storeError)
    return (
      <div className="container page">
        <ErrorBox text={storeError} />
      </div>
    );
  if (!cart.length)
    return (
      <div className="container page">
        <Empty title="Bạn chưa có sản phẩm trong giỏ">
          <Link className="button" to="/san-pham">
            Chọn sản phẩm <Arrow />
          </Link>
        </Empty>
      </div>
    );
  return (
    <div className="container page">
      <Link className="text-link" to="/gio-hang">
        ← Quay lại giỏ hàng
      </Link>
      <div className="page-intro compact">
        <span className="eyebrow">COD · KHÔNG CẦN TÀI KHOẢN</span>
        <h1>
          {review ? 'Kiểm tra & ' : 'Thông tin '}
          <em>đặt hàng.</em>
        </h1>
      </div>
      <form onSubmit={place} className="cart-grid">
        <div className="checkout-form">
          {review ? (
            <>
              <h3>Kiểm tra trước khi gửi</h3>
              <dl className="review-list">
                <dt>Người nhận</dt>
                <dd>{form.recipientName}</dd>
                <dt>Điện thoại</dt>
                <dd>{form.recipientPhone}</dd>
                <dt>Địa chỉ tại Hà Nội</dt>
                <dd>{form.recipientAddress}</dd>
                <dt>Ghi chú</dt>
                <dd>{form.customerNote || 'Không có'}</dd>
              </dl>
              <button type="button" className="text-link" onClick={() => setReview(false)}>
                ← Sửa thông tin
              </button>
            </>
          ) : (
            <>
              <h3>Gửi bữa ngon đến bạn</h3>
              <label>
                Họ và tên
                <input
                  required
                  maxLength={150}
                  autoComplete="name"
                  value={form.recipientName}
                  onChange={(e) => setForm({ ...form, recipientName: e.target.value })}
                  placeholder="Tên người nhận"
                />
              </label>
              <label>
                Số điện thoại
                <input
                  required
                  type="tel"
                  inputMode="tel"
                  pattern="(0[0-9]{9}|\+84[0-9]{9})"
                  maxLength={12}
                  autoComplete="tel"
                  value={form.recipientPhone}
                  onChange={(e) => setForm({ ...form, recipientPhone: e.target.value })}
                  placeholder="Số điện thoại để nhóm gọi xác nhận"
                />
              </label>
              <label>
                Địa chỉ nhận hàng tại Hà Nội
                <textarea
                  required
                  maxLength={600}
                  autoComplete="street-address"
                  value={form.recipientAddress}
                  onChange={(e) => setForm({ ...form, recipientAddress: e.target.value })}
                  placeholder="Số nhà, đường, phường/xã…"
                />
              </label>
              <label>
                Ghi chú <span className="muted">(không bắt buộc)</span>
                <textarea
                  maxLength={1000}
                  value={form.customerNote}
                  onChange={(e) => setForm({ ...form, customerNote: e.target.value })}
                  placeholder="Điều bạn muốn nhắn với ForFit"
                />
              </label>
            </>
          )}
        </div>
        <aside className="order-summary">
          <h3>Tóm tắt đơn hàng</h3>
          {rows.map((r) => (
            <div key={r.variantId}>
              <span>
                {r.p?.name ?? 'Không khả dụng'}
                <small>
                  {r.v?.label} × {r.quantity}
                </small>
              </span>
              <b>{money((r.v?.sellingPrice ?? 0) * r.quantity)}</b>
            </div>
          ))}
          <div className="summary-total">
            <span>Tiền hàng dự kiến</span>
            <strong>{money(total)}</strong>
          </div>
          <p>
            Thanh toán khi nhận hàng (COD). Nhóm sẽ gọi lại xác nhận trước khi chuẩn bị. Website
            chưa tính phí giao hàng.
          </p>
          {error && <ErrorBox text={error} />}{' '}
          {invalid && (
            <ErrorBox text="Giỏ hàng có sản phẩm không đủ khả dụng. Quay lại để cập nhật." />
          )}
          <button className="button full" disabled={busy || invalid}>
            {busy ? 'Đang gửi yêu cầu…' : review ? 'Xác nhận đặt hàng COD' : 'Kiểm tra đơn hàng'}
            <Arrow />
          </button>
          <small>Thông tin nhận hàng chỉ dùng để xử lý đơn. Không cần tạo tài khoản.</small>
        </aside>
      </form>
    </div>
  );
}
export function NotFound() {
  return (
    <div className="container page">
      <Empty title="Trang này chưa có ở đây">
        <Link className="button" to="/">
          Về trang chủ <Arrow />
        </Link>
      </Empty>
    </div>
  );
}

