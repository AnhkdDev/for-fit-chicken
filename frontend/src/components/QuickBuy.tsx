import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { ArrowUpRight, Check, Minus, Plus, ShoppingBag, Truck } from 'lucide-react';
import { api, money } from '../lib/api';
import { useShop } from '../lib/state';
import type { Product } from '../lib/types';
import { ErrorBox, Loading, Modal } from './ui';

export function QuickBuy({ productId, close }: { productId: number; close: () => void }) {
  const { cart, add, updatePublicProduct } = useShop();
  const [product, setProduct] = useState<Product>();
  const [variantId, setVariantId] = useState<number>();
  const [quantity, setQuantity] = useState('1');
  const [image, setImage] = useState(0);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [retry, setRetry] = useState(0);
  const [added, setAdded] = useState(false);
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    setAdded(false);
    setProduct(undefined);
    api<Product>(`/products/${productId}`)
      .then((p) => {
        if (!active) return;
        setProduct(p);
        updatePublicProduct(p);
        setVariantId(
          (
            p.variants.find((v) => v.isActive && v.available > 0) ??
            p.variants.find((v) => v.isActive)
          )?.id,
        );
        setQuantity('1');
        setImage(0);
      })
      .catch((e) => {
        if (active) setError(e.message);
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [productId, retry]);
  const variant = product?.variants.find((v) => v.id === variantId);
  const inCart = cart.find((c) => c.variantId === variantId)?.quantity ?? 0;
  const remaining = Math.max(0, Math.min(100, variant?.available ?? 0) - inCart);
  const amount = Number(quantity);
  const valid = Number.isInteger(amount) && amount >= 1 && amount <= remaining;
  const images = product?.images ?? [];
  const description = product?.shortDescription || product?.description || '';
  function submit(e: FormEvent) {
    e.preventDefault();
    if (!variant || !valid || added) return;
    if (add(variant.id, amount, variant)) setAdded(true);
    else setError('Chưa thêm được vào giỏ. Kiểm tra số lượng hoặc tải lại sản phẩm.');
  }
  return (
    <Modal title="Một bữa ngon, theo cách của bạn." close={close} className="quick-buy-modal">
      {loading ? (
        <Loading />
      ) : !product ? (
        <ErrorBox text={error || 'Chưa tải được sản phẩm.'} retry={() => setRetry((x) => x + 1)} />
      ) : (
        <div className="quick-buy-grid">
          <div className="quick-buy-gallery">
            <div className="quick-buy-image">
              {images[image] ? (
                <img src={images[image].url} alt={images[image].altText ?? product.name} />
              ) : (
                <ShoppingBag size={80} strokeWidth={0.7} />
              )}
            </div>
            {images.length > 1 && (
              <div className="thumbs">
                {images.map((im, i) => (
                  <button
                    key={im.id}
                    type="button"
                    className={image === i ? 'selected' : ''}
                    aria-label={`Xem ảnh sản phẩm ${i + 1}`}
                    onClick={() => setImage(i)}
                  >
                    <img src={im.url} alt="" />
                  </button>
                ))}
              </div>
            )}
            <span className="quick-buy-caption">FORFIT CHICKEN / SLOW COOKED</span>
          </div>
          <form className="quick-buy-copy" onSubmit={submit}>
            <span className="eyebrow">TỪ CÂU CHUYỆN ĐẾN BỮA ĂN CỦA BẠN</span>
            <h3>{product.name}</h3>
            <p>{description.length > 260 ? `${description.slice(0, 260)}…` : description}</p>
            <strong className="quick-buy-price">
              {variant ? money(variant.sellingPrice) : 'Chưa có phân loại đang bán'}
            </strong>
            <fieldset className="quick-buy-variants">
              <legend>Chọn phân loại</legend>
              <div className="variant-options">
                {product.variants
                  .filter((v) => v.isActive)
                  .map((v) => (
                    <button
                      key={v.id}
                      type="button"
                      aria-pressed={variantId === v.id}
                      className={variantId === v.id ? 'selected' : ''}
                      onClick={() => {
                        setVariantId(v.id);
                        setQuantity('1');
                        setAdded(false);
                        setError('');
                      }}
                    >
                      {v.label}
                      {v.netWeightGrams &&
                      !v.label.replace(/\s/g, '').toLowerCase().includes(`${v.netWeightGrams}g`)
                        ? ` · ${v.netWeightGrams}g`
                        : ''}
                      {v.available <= 0 ? ' · Hết hàng' : ''}
                    </button>
                  ))}
              </div>
            </fieldset>
            {added ? (
              <div className="quick-buy-added" role="status">
                <Check size={22} />
                <div>
                  <b>Đã thêm {amount} gói vào giỏ.</b>
                  <p>Bạn có thể tiếp tục khám phá hoặc xem lại giỏ hàng.</p>
                </div>
              </div>
            ) : (
              <>
                <p className="quick-buy-stock">
                  {!variant?.available
                    ? 'Phân loại này tạm hết hàng.'
                    : remaining > 0
                      ? `${remaining} gói có thể thêm${inCart ? ` · Đã có ${inCart} gói trong giỏ` : ''}`
                      : `Bạn đã có ${inCart} gói trong giỏ, đạt số lượng khả dụng.`}
                </p>
                {error && <ErrorBox text={error} />}
                <div className="quick-buy-actions">
                  <div className="quantity">
                    <button
                      type="button"
                      aria-label="Giảm số lượng mua nhanh"
                      disabled={amount <= 1 || !remaining}
                      onClick={() => setQuantity(String(Math.max(1, amount - 1)))}
                    >
                      <Minus size={15} />
                    </button>
                    <input
                      type="number"
                      aria-label="Số lượng mua nhanh"
                      min={1}
                      max={remaining || 1}
                      step={1}
                      required
                      disabled={!remaining}
                      value={quantity}
                      onChange={(e) => setQuantity(e.target.value)}
                    />
                    <button
                      type="button"
                      aria-label="Tăng số lượng mua nhanh"
                      disabled={amount >= remaining || !remaining}
                      onClick={() => setQuantity(String(Math.min(remaining, amount + 1)))}
                    >
                      <Plus size={15} />
                    </button>
                  </div>
                  <button className="button" disabled={!variant || !valid}>
                    <ShoppingBag size={17} />
                    Thêm vào giỏ
                  </button>
                </div>
              </>
            )}
            <div className="quick-buy-links">
              {added ? (
                <>
                  <button type="button" className="text-link" onClick={close}>
                    Tiếp tục khám phá
                  </button>
                  <Link className="button" to="/gio-hang" onClick={close}>
                    Đến giỏ hàng <ArrowUpRight size={17} />
                  </Link>
                </>
              ) : (
                <Link className="text-link" to={`/san-pham/${product.id}`} onClick={close}>
                  Xem đầy đủ thông tin <ArrowUpRight size={16} />
                </Link>
              )}
            </div>
            <div className="quick-buy-note">
              <Truck size={17} />
              <span>
                Giao tại Hà Nội · Thanh toán COD
                <br />
                Nhóm gọi xác nhận sau khi bạn gửi đơn.
              </span>
            </div>
          </form>
        </div>
      )}
    </Modal>
  );
}
