import { useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, ArrowRight, ArrowUpRight, ChefHat, Dumbbell, ShoppingBag } from 'lucide-react';
import { money } from '../lib/api';
import { useShop } from '../lib/state';
import { Empty, ErrorBox, Loading } from './ui';
import { QuickBuy } from './QuickBuy';
import { BrandArtwork } from './BrandArtwork';

export function StoryShowcase() {
  const { products, sections, error, loading, reload } = useShop();
  const story = sections.find((s) => s.sectionKey === 'Story')?.content;
  const summary = (story?.body?.trim() ||
    'Bốn người, một ý tưởng về ức gà nấu chậm. Chúng mình bắt đầu từ những bữa ăn thường ngày, mong đưa ForFit đến gần bạn hơn — từ website đến các phòng gym.')
    .replace(/\s+/g, ' ');
  const excerpt = summary.length > 240
    ? `${summary.slice(0, 240).replace(/\s+\S*$/, '').trimEnd()}…`
    : summary;
  const rail = useRef<HTMLUListElement>(null);
  const [selected, setSelected] = useState<number>();
  const featured = [...products].sort((a, b) => Number(b.isFeatured) - Number(a.isFeatured));
  function scroll(direction: number) {
    const node = rail.current;
    if (node)
      node.scrollBy({
        left: direction * node.clientWidth * 0.8,
        behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches
          ? 'instant'
          : 'smooth',
      });
  }
  return (
    <section className="story-discovery" id="cau-chuyen" aria-labelledby="discovery-title">
      <div className="container discovery-layout">
        <div className="discovery-copy">
          <span className="eyebrow">01 / KHÁM PHÁ CÂU CHUYỆN FORFIT</span>
          <h2 id="discovery-title">
            {story?.title ?? (
              <>
                Bắt đầu từ
                <br />
                một ý tưởng nhỏ.
                <br />
                <em>Đến bữa ăn mỗi ngày.</em>
              </>
            )}
          </h2>
          <p className="discovery-excerpt">{excerpt}</p>
          <Link className="button" to="/cau-chuyen">
            Xem thêm <ArrowRight size={17} />
          </Link>
          <span className="discovery-signature">
            Chậm trong cách nấu.
            <br />
            Gần gũi trong cách sống.
          </span>
        </div>
        <div className="discovery-scene">
          <div className="discovery-orbit" aria-hidden="true" />
          <Link
            to="/cau-chuyen"
            className={`discovery-main ${story?.assetIds?.[0] ? 'has-photo' : ''}`}
            aria-label="Khám phá câu chuyện của 4fit"
          >
            {story?.assetIds?.[0] ? (
              <img
                src={`/api/media/${story.assetIds[0]}`}
                alt="Câu chuyện ForFit Chicken"
                loading="lazy"
              />
            ) : (
              <>
                <span className="eyebrow">FOR FIT CHICKEN</span>
                <BrandArtwork />
                <span className="discovery-main-caption">MỘT PHẦN TRONG NGÀY CỦA BẠN.</span>
              </>
            )}
            <span className="discovery-play">
              <ArrowUpRight size={24} />
            </span>
          </Link>
          <span className="discovery-location">HÀ NỘI / NƠI MÌNH BẮT ĐẦU</span>
        </div>
        <div className="discovery-notes">
          {[
            {
              asset: story?.assetIds?.[1],
              Icon: ChefHat,
              title: 'Từ một ý tưởng',
              body: 'Ức gà nấu chậm, dành cho bữa ăn thường ngày.',
            },
            {
              asset: story?.assetIds?.[2],
              Icon: Dumbbell,
              title: 'Đến gần bạn hơn',
              body: 'Kết nối với các phòng gym và cộng đồng tập luyện.',
            },
          ].map(({ asset, Icon, title, body }, i) => (
            <Link className={`discovery-note note-${i}`} to="/cau-chuyen" key={title}>
              {asset ? (
                <img src={`/api/media/${asset}`} alt={title} loading="lazy" />
              ) : (
                <div className="discovery-note-art">
                  <Icon size={46} strokeWidth={1} />
                  <span>{i === 0 ? 'SLOW COOKED' : 'FOR YOUR EVERYDAY'}</span>
                </div>
              )}
              <div>
                <h3>{title}</h3>
                <p>{body}</p>
              </div>
            </Link>
          ))}
        </div>
      </div>
      {selected !== undefined && (
        <QuickBuy productId={selected} close={() => setSelected(undefined)} />
      )}
    </section>
  );
}
