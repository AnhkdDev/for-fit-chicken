import { ProductImage } from './ProductImage';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useShop } from '../lib/state';
import { money } from '../lib/api';
import type { Product, Section } from '../lib/types';
import { QuickBuy } from './QuickBuy';
import { Modal } from './ui';
import './home-products.css';
import { orderedProductImages, selectedProducts } from '../lib/home-display';
export function HomeProducts({
  configuration,
  items,
  preview = false,
}: {
  configuration?: Section['content'];
  items?: Product[];
  preview?: boolean;
}) {
  const shop = useShop();
  const config =
    configuration ?? shop.sections.find((s) => s.sectionKey === 'HomeProducts')?.content;
  const source = items ?? shop.products;
  const ids = config?.productIds ?? source.filter((p) => p.isFeatured).map((p) => p.id);
  const products = selectedProducts(source, ids);
  const [buy, setBuy] = useState<number>();
  const [zoom, setZoom] = useState<{ url: string; alt: string }>();
  if (config?.enabled === false || !products.length)
    return preview ? (
      <p className="display-empty">Khu sản phẩm đang tắt hoặc chưa có sản phẩm hợp lệ được chọn.</p>
    ) : null;
  return (
    <section className="home-products" id="san-pham-noi-bat" tabIndex={-1}>
      <div className="container">
        <header>
          <span className="eyebrow featured-products-label">Sản phẩm nổi bật của 4fitchicken</span>
          <h2>{config?.title || 'Bữa ngon, gần bạn hơn.'}</h2>
          {config?.body && <p>{config.body}</p>}
        </header>
        {products.map((p) => (
          <ProductShowcase
            key={`${p.id}:${config?.productMainImages?.[String(p.id)] ?? 0}`}
            product={p}
            mainAssetId={config?.productMainImages?.[String(p.id)]}
            preview={preview}
            choose={() => setBuy(p.id)}
            zoom={setZoom}
          />
        ))}
        <Link
          className="text-link"
          to="/san-pham"
          onClick={preview ? (e) => e.preventDefault() : undefined}
        >
          {config?.linkLabel || 'Tất cả sản phẩm'} →
        </Link>
      </div>
      {buy !== undefined && !preview && (
        <QuickBuy productId={buy} close={() => setBuy(undefined)} />
      )}{' '}
      {zoom && (
        <Modal title="Ảnh sản phẩm" close={() => setZoom(undefined)}>
          <img className="display-zoom" src={zoom.url} alt={zoom.alt} />
        </Modal>
      )}
    </section>
  );
}
function ProductShowcase({
  product: p,
  mainAssetId,
  preview,
  choose,
  zoom,
}: {
  product: Product;
  mainAssetId?: number;
  preview: boolean;
  choose: () => void;
  zoom: (image: { url: string; alt: string }) => void;
}) {
  const [image, setImage] = useState(0);
  const [sample, setSample] = useState(false);
  const orderedImages = orderedProductImages(p, mainAssetId);
  const current = orderedImages[image] ?? orderedImages[0];
  const variants = p.variants.filter((v) => v.isActive);
  const available = variants.some((v) => v.available > 0);
  const price = variants.length ? Math.min(...variants.map((v) => v.sellingPrice)) : undefined;
  return (
    <article className="product-showcase">
      <div className="showcase-gallery">
        <div className="showcase-main">
          {current ? (
            <>
              <ProductImage key={current.url} src={current.url} alt={current.altText || p.name} />
              <button
                type="button"
                className="showcase-zoom"
                aria-label={`Phóng to ảnh ${p.name}`}
                onClick={() => zoom({ url: current.url, alt: current.altText || p.name })}
              >
                Phóng to ↗
              </button>
            </>
          ) : (
            <span>Đang cập nhật ảnh</span>
          )}
        </div>
        {p.images.length > 1 && (
          <div className="showcase-thumbs" aria-label={`Các ảnh của ${p.name}`}>
            {orderedImages.map((im, i) => (
              <button
                key={im.id}
                aria-label={`Xem ảnh ${i + 1} của ${p.name}`}
                aria-pressed={image === i}
                onClick={() => setImage(i)}
              >
                <img src={im.url} alt={im.altText || `Góc ảnh ${i + 1}`} loading="lazy" />
              </button>
            ))}
          </div>
        )}
      </div>
      <div className="showcase-copy">
        <span className="eyebrow">ỨC GÀ NẤU CHẬM</span>
        <h3>{p.name}</h3>
        <p className="showcase-description">{p.shortDescription}</p>
        <p>{variants.length === 1 ? variants[0].label : `${variants.length} quy cách lựa chọn`}</p>
        <strong className="showcase-price">
          {price === undefined
            ? 'Đang cập nhật giá'
            : `${variants.length > 1 ? 'Từ ' : ''}${money(price)}`}
        </strong>
        {!available && <p className="showcase-soldout">Tạm hết hàng</p>}
        <div className="button-row">
          <button
            className="button"
            disabled={!available}
            onClick={() => (preview ? setSample(true) : choose())}
          >
            Chọn mua →
          </button>
          <Link
            className="text-link"
            to={`/san-pham/${p.id}`}
            onClick={preview ? (e) => e.preventDefault() : undefined}
          >
            Xem chi tiết
          </Link>
        </div>
        {preview && sample && (
          <div className="preview-product-info">
            <b>Xem thử lựa chọn — không tạo giỏ/đơn</b>
            {variants.map((v) => (
              <p key={v.id}>
                {v.label} · {money(v.sellingPrice)} · Khả dụng {v.available}
              </p>
            ))}
            <button type="button" onClick={() => setSample(false)}>
              Đóng lựa chọn
            </button>
          </div>
        )}
      </div>
    </article>
  );
}

