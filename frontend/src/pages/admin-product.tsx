import { DeleteRecord } from '../components/DeleteRecord';
import { useOwner } from '../lib/admin-session';
import { useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api, money, send } from '../lib/api';
import type { Asset, Product, Variant } from '../lib/types';
import { productSlug } from '../lib/admin-forms';
import { useUnsavedWarning } from '../lib/useUnsavedWarning';
import { Field, Heading, Panel } from '../components/admin-ui';
import { AdminAssetPicker } from '../components/AdminAssetPicker';
import { ErrorBox, Loading, Success } from '../components/ui';

const defaults = {
  name: '',
  slug: '',
  shortDescription: '',
  description: '',
  ingredients: '',
  nutritionText: '',
  usageInstructions: '',
  storageInstructions: '',
  isActive: true,
  isPublished: false,
  isFeatured: false,
  sortOrder: 0,
};
function formOf(p: Product) {
  return Object.fromEntries(
    Object.entries(defaults).map(([k, v]) => [k, p[k as keyof Product] ?? v]),
  ) as typeof defaults;
}
export function ProductWorkspace() {
  const owner = useOwner();
  const { id } = useParams();
  const navigate = useNavigate();
  const [p, setP] = useState<Product>();
  const [draft, setDraft] = useState({ ...defaults });
  const [baseline, setBaseline] = useState(JSON.stringify(defaults));
  const [loading, setLoading] = useState(!!id);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [step, setStep] = useState(
    new URLSearchParams(window.location.search).get('step') === '1' ? 1 : 0,
  );
  const [sku, setSku] = useState<Variant | null | undefined>();
  const [slugEdited, setSlugEdited] = useState(!!id);
  const dirty = JSON.stringify(draft) !== baseline;
  useUnsavedWarning(dirty);
  function apply(value: Product) {
    setP(value);
    const d = formOf(value);
    setDraft(d);
    setBaseline(JSON.stringify(d));
  }
  useEffect(() => {
    let active = true;
    if (id) {
      setLoading(true);
      api<Product>(`/admin/products/${id}`)
        .then((value) => {
          if (active) apply(value);
        })
        .catch((e) => active && setError(e.message))
        .finally(() => active && setLoading(false));
    }
    return () => {
      active = false;
    };
  }, [id]);
  async function mutate(work: () => Promise<void>) {
    setBusy(true);
    setError('');
    setMessage('');
    try {
      await work();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function save(e: FormEvent) {
    e.preventDefault();
    await mutate(async () => {
      const value = await send<Product>(
        `/admin/products${p ? '/' + p.id : ''}`,
        p ? 'PUT' : 'POST',
        { ...draft, isPublished: p?.isPublished ?? false, version: p?.version },
      );
      apply(value);
      setMessage(
        p ? 'Đã lưu thông tin.' : 'Đã tạo bản nháp. Tiếp theo: thêm phân loại và giá bán.',
      );
      if (!p) {
        setStep(1);
        navigate(`/quan-tri/san-pham/${value.id}?step=1`, { replace: true });
      }
    });
  }
  async function attach(asset: Asset) {
    if (!p) return;
    await send(`/admin/products/${p.id}/images`, 'POST', {
      version: p.version,
      image: { assetId: asset.id, variantId: null, altText: p.name, sortOrder: p.images.length },
    });
    setP(await api<Product>(`/admin/products/${p.id}`));
    setMessage('Đã gắn ảnh sản phẩm.');
  }
  async function saveSku(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!p) return;
    const f = new FormData(e.currentTarget);
    await mutate(async () => {
      await send(
        `/admin/products/${p.id}/variants${sku ? '/' + sku.id : ''}`,
        sku ? 'PUT' : 'POST',
        {
          skuCode: String(f.get('skuCode')).toUpperCase(),
          label: f.get('label'),
          netWeightGrams: Number(f.get('weight')),
          sellingPrice: Number(f.get('price')),
          isActive: f.has('isActive'),
          sortOrder: 0,
          version: sku?.version,
        },
      );
      setP(await api<Product>(`/admin/products/${p.id}`));
      setSku(undefined);
      setMessage('Đã lưu phân loại và giá bán.');
    });
  }
  const ready = !!p && p.isActive && p.images.length > 0 && p.variants.some((v) => v.isActive);
  return (
    <>
      <Heading
        title={p ? p.name : 'Thêm sản phẩm'}
        description="Lưu thông tin cơ bản trước; sau đó thêm giá bán, ảnh và công bố."
      >
        {p && (
          <button
            type="button"
            className="small-button"
            disabled={busy}
            onClick={() => {
              if (!dirty || confirm('Bỏ thay đổi chưa lưu và tải lại dữ liệu?'))
                void mutate(async () => apply(await api<Product>(`/admin/products/${p.id}`)));
            }}
          >
            Tải lại dữ liệu
          </button>
        )}
        {owner && p && (
          <DeleteRecord
            kind="sản phẩm"
            label={p.name}
            path={`/admin/products/${p.id}`}
            version={p.version}
            deleted={() => navigate('/quan-tri/san-pham')}
          />
        )}
        <Link className="small-button" to="/quan-tri/san-pham">
          ← Danh sách sản phẩm
        </Link>
      </Heading>
      {loading ? (
        <Loading />
      ) : id && !p ? (
        <ErrorBox
          text={error || 'Không tải được sản phẩm.'}
          retry={() => {
            if (!busy) void mutate(async () => apply(await api<Product>(`/admin/products/${id}`)));
          }}
        />
      ) : (
        <>
          <nav className="admin-steps" aria-label="Các bước nhập sản phẩm">
            {[
              '1. Thông tin',
              '2. Phân loại & giá',
              '3. Hình ảnh',
              ...(owner ? ['4. Công bố'] : []),
            ].map((label, i) => (
              <button
                key={label}
                type="button"
                className={step === i ? 'active' : ''}
                disabled={busy || (!p && i > 0)}
                onClick={() => setStep(i)}
              >
                {label}
              </button>
            ))}
          </nav>
          {error && <ErrorBox text={error} />} {message && <Success>{message}</Success>}
          <div className="admin-editor-grid">
            <div>
              {step === 0 && (
                <form onSubmit={save} className="form-stack">
                  <fieldset disabled={busy} className="form-stack">
                    <Panel>
                      <h2>Thông tin cơ bản</h2>
                      <p className="admin-help">
                        Tên và mô tả ngắn được dùng chung ở trang chủ, danh sách và chi tiết sản
                        phẩm. Viết tên rõ hương vị, mô tả 1–2 câu, tránh lặp lại tên. Giá, khối
                        lượng và tên lựa chọn nhập tại bước Quy cách; không ghép giá vào tên sản
                        phẩm. Khi sản phẩm đã công bố, lưu thông tin có thể cập nhật ngay các vị trí
                        đang trưng bày. Trang chủ xem trước tối đa 3 dòng mô tả; khách mở chi tiết
                        để đọc đầy đủ.
                      </p>
                      <label>
                        Tên sản phẩm *
                        <input
                          required
                          maxLength={200}
                          value={draft.name}
                          onChange={(e) =>
                            setDraft({
                              ...draft,
                              name: e.target.value,
                              ...(!slugEdited ? { slug: productSlug(e.target.value) } : {}),
                            })
                          }
                        />
                      </label>
                      <label>
                        Đường dẫn sản phẩm *
                        <input
                          required
                          pattern="[a-z0-9]+(-[a-z0-9]+)*"
                          maxLength={200}
                          value={draft.slug}
                          onChange={(e) => {
                            setSlugEdited(true);
                            setDraft({ ...draft, slug: e.target.value });
                          }}
                        />
                        <small>
                          Mã đường dẫn được gợi ý từ tên. Đợt này website vẫn truy cập sản phẩm bằng
                          ID; sửa trường này chưa đổi URL khách đang dùng.
                        </small>
                      </label>
                      <label>
                        Mô tả ngắn
                        <textarea
                          rows={3}
                          maxLength={600}
                          value={draft.shortDescription}
                          onChange={(e) => setDraft({ ...draft, shortDescription: e.target.value })}
                        />
                      </label>
                      <div className="checkboxes">
                        <label>
                          <input
                            type="checkbox"
                            checked={draft.isActive}
                            disabled={!owner && !!p?.isPublished}
                            onChange={(e) => setDraft({ ...draft, isActive: e.target.checked })}
                          />
                          Đang hoạt động
                        </label>
                        {owner && (
                          <label>
                            <input
                              type="checkbox"
                              checked={draft.isFeatured}
                              onChange={(e) => setDraft({ ...draft, isFeatured: e.target.checked })}
                            />
                            Sản phẩm nổi bật (danh sách dự phòng)
                          </label>
                        )}
                      </div>
                      {owner && (
                        <p className="admin-help">
                          Để chọn sản phẩm và ảnh chính trên trang chủ, vào{' '}
                          <Link to="/quan-tri/trung-bay">Trưng bày trang chủ</Link>. Dấu nổi bật chỉ
                          quyết định danh sách trang chủ khi chưa có cấu hình trưng bày được công
                          bố.
                        </p>
                      )}
                    </Panel>
                    <Panel>
                      <details>
                        <summary>Thông tin chi tiết, dinh dưỡng & bảo quản</summary>
                        {[
                          ['description', 'Câu chuyện sản phẩm', 20000],
                          ['ingredients', 'Thành phần', 10000],
                          ['nutritionText', 'Dinh dưỡng', 10000],
                          ['usageInstructions', 'Hướng dẫn sử dụng', 10000],
                          ['storageInstructions', 'Bảo quản', 10000],
                        ].map(([key, label, max]) => (
                          <label key={key}>
                            {label}
                            <textarea
                              rows={4}
                              maxLength={Number(max)}
                              value={String(draft[key as keyof typeof draft])}
                              onChange={(e) => setDraft({ ...draft, [key]: e.target.value })}
                            />
                          </label>
                        ))}
                        <label>
                          Thứ tự hiển thị
                          <input
                            type="number"
                            min={0}
                            max={100000}
                            value={draft.sortOrder}
                            onChange={(e) =>
                              setDraft({ ...draft, sortOrder: Number(e.target.value) })
                            }
                          />
                        </label>
                      </details>
                    </Panel>
                    <div className="admin-savebar">
                      <span>
                        {dirty
                          ? 'Có thay đổi chưa lưu'
                          : p
                            ? 'Thông tin đã lưu'
                            : 'Chưa tạo bản nháp'}
                      </span>
                      <button className="button" disabled={busy}>
                        {busy ? 'Đang lưu…' : p ? 'Lưu thông tin' : 'Tạo bản nháp & tiếp tục'}
                      </button>
                    </div>
                  </fieldset>
                </form>
              )}
              {step === 1 && p && (
                <Panel>
                  <div className="panel-title">
                    <h2>Phân loại & giá bán</h2>
                    <button className="button" disabled={busy} onClick={() => setSku(null)}>
                      + Thêm phân loại
                    </button>
                  </div>
                  <p className="muted">
                    Mỗi quy cách là một SKU. Tồn hàng được cập nhật ở Kho & lô hàng.
                  </p>
                  <div className="line-list">
                    {p.variants.map((v) => (
                      <div key={v.id}>
                        <span>
                          <b>{v.label}</b>
                          <small>
                            {v.skuCode} · {v.netWeightGrams} g ·{' '}
                            {v.isActive ? 'Đang bán' : 'Ngừng bán'}
                          </small>
                        </span>
                        <b>{money(v.sellingPrice)}</b>
                        <button className="small-button" disabled={busy} onClick={() => setSku(v)}>
                          Sửa
                        </button>
                      </div>
                    ))}
                  </div>
                  {!p.variants.length && (
                    <p>Chưa có phân loại. Thêm quy cách đầu tiên để thiết lập giá.</p>
                  )}
                  {sku !== undefined && (
                    <form
                      className="form-stack admin-inset"
                      key={sku?.id ?? 'new'}
                      onSubmit={saveSku}
                    >
                      <fieldset disabled={busy} className="form-stack">
                        <h3>{sku ? 'Sửa phân loại' : 'Phân loại mới'}</h3>
                        <div className="form-grid">
                          <Field
                            label="Mã SKU *"
                            name="skuCode"
                            value={sku?.skuCode}
                            readOnly={!!sku}
                            required
                            maxLength={50}
                            pattern="[A-Za-z0-9_-]+"
                            placeholder="UC-GA-TIEU-150"
                          />
                          <Field
                            label="Tên quy cách *"
                            name="label"
                            value={sku?.label}
                            required
                            maxLength={150}
                            placeholder="Vị tiêu đen · gói 150 g"
                          />
                          <Field
                            label="Khối lượng (g) *"
                            name="weight"
                            type="number"
                            value={sku ? String(sku.netWeightGrams) : ''}
                            required
                            min={1}
                            max={100000}
                          />
                          <Field
                            label="Giá bán (đ) *"
                            name="price"
                            type="number"
                            value={sku ? String(sku.sellingPrice) : ''}
                            required
                            min={1}
                            max={999999999}
                          />
                        </div>
                        <label className="checkbox-label">
                          <input
                            type="checkbox"
                            name="isActive"
                            defaultChecked={sku?.isActive ?? true}
                          />
                          Cho phép bán phân loại này
                        </label>
                        <small>
                          Mã SKU không đổi sau khi tạo; khối lượng có thể bị khóa khi đã phát sinh
                          giao dịch.
                        </small>
                        <div className="button-row">
                          <button className="button" disabled={busy}>
                            Lưu phân loại
                          </button>
                          <button
                            type="button"
                            className="small-button"
                            disabled={busy}
                            onClick={() => setSku(undefined)}
                          >
                            Đóng form
                          </button>
                        </div>
                      </fieldset>
                    </form>
                  )}
                </Panel>
              )}
              {step === 2 && p && (
                <Panel>
                  <h2>Ảnh sản phẩm</h2>
                  <p className="muted">
                    Chọn ảnh đã có hoặc tải ảnh mới. Ảnh đầu dùng làm ảnh đại diện.
                  </p>
                  <div className="asset-grid">
                    {p.images.map((im) => (
                      <figure key={im.id}>
                        <img src={im.url} alt={im.altText ?? p.name} />
                        <figcaption>
                          <button
                            className="small-button"
                            disabled={busy}
                            onClick={() => {
                              if (confirm('Gỡ ảnh khỏi sản phẩm? Tệp trong thư viện vẫn được giữ.'))
                                void mutate(async () => {
                                  await send(`/admin/products/${p.id}/images/${im.id}`, 'DELETE', {
                                    version: p.version,
                                  });
                                  setP(await api<Product>(`/admin/products/${p.id}`));
                                });
                            }}
                          >
                            Gỡ khỏi sản phẩm
                          </button>
                        </figcaption>
                      </figure>
                    ))}
                  </div>
                  <AdminAssetPicker
                    selected={p.images.map((im) => im.assetId)}
                    disabled={busy}
                    onChoose={attach}
                  />
                </Panel>
              )}
              {owner && step === 3 && p && (
                <Panel>
                  <h2>{p.isPublished ? 'Sản phẩm đang công bố' : 'Chuẩn bị công bố'}</h2>
                  <ul className="admin-checklist">
                    <li>{p.isActive ? '✓' : '○'} Sản phẩm hoạt động</li>
                    <li>{p.variants.some((v) => v.isActive) ? '✓' : '○'} Có phân loại đang bán</li>
                    <li>{p.images.length ? '✓' : '○'} Có ảnh sản phẩm</li>
                    <li>{!dirty ? '✓' : '○'} Đã lưu thông tin đang nhập</li>
                  </ul>
                  <p>Công bố đưa sản phẩm lên website. Tạo sản phẩm không tự thêm hàng vào kho.</p>
                  <button
                    className="button"
                    disabled={busy || dirty || (!p.isPublished && !ready)}
                    onClick={() => {
                      if (
                        confirm(
                          p.isPublished
                            ? 'Ẩn sản phẩm khỏi website?'
                            : 'Công bố sản phẩm này lên website?',
                        )
                      )
                        void mutate(async () => {
                          const value = await send<Product>(`/admin/products/${p.id}`, 'PUT', {
                            ...formOf(p),
                            isPublished: !p.isPublished,
                            version: p.version,
                          });
                          apply(value);
                          setMessage(
                            value.isPublished ? 'Đã công bố sản phẩm.' : 'Đã ẩn sản phẩm.',
                          );
                        });
                    }}
                  >
                    {p.isPublished ? 'Ẩn khỏi website' : 'Công bố sản phẩm'}
                  </button>
                  {dirty && (
                    <p className="admin-help">
                      Quay về bước Thông tin và lưu trước khi đổi trạng thái công bố.
                    </p>
                  )}
                </Panel>
              )}
            </div>
            <aside className="admin-editor-aside">
              <Panel>
                <h2>Tiến độ nhập</h2>
                {!owner && (
                  <p className="admin-help">
                    Owner duyệt công bố/nổi bật. Sản phẩm đang công bố cần Owner thực hiện ẩn/ngừng
                    hoạt động.
                  </p>
                )}
                <p>{p ? 'Bản nháp đã có trong hệ thống.' : 'Chưa tạo sản phẩm.'}</p>
                <ul className="admin-checklist">
                  <li>{p ? '✓' : '○'} Thông tin cơ bản</li>
                  <li>{p?.variants.length ? '✓' : '○'} Phân loại & giá</li>
                  <li>{p?.images.length ? '✓' : '○'} Hình ảnh</li>
                  <li>{p?.isPublished ? '✓' : '○'} Công bố</li>
                </ul>
                {dirty && <p className="admin-help">Thông tin đang sửa chưa được lưu.</p>}
                <Link className="text-link" to="/quan-tri/kho">
                  Quản lý tồn kho →
                </Link>
              </Panel>
            </aside>
          </div>
        </>
      )}
    </>
  );
}
