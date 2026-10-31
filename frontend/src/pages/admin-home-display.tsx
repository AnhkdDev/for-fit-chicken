import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, safeLink, send } from '../lib/api';
import { useOwner } from '../lib/admin-session';
import { useUnsavedWarning } from '../lib/useUnsavedWarning';
import type { Product, Page, Section, HomeImageFrame } from '../lib/types';
import { ImageFrameEditor } from '../components/ImageFrameEditor';
import { frameForAsset } from '../lib/image-framing';
import { Heading, Panel } from '../components/admin-ui';
import { ErrorBox, Loading, Modal, Success } from '../components/ui';
import { Home } from './storefront';
import { StoreProvider } from '../lib/state';
const empty = () => ({
  title: 'Bữa ngon, gần bạn hơn.',
  body: '',
  assetIds: [] as number[],
  linkLabel: 'Tất cả sản phẩm',
  linkUrl: '/san-pham',
  productIds: [] as number[],
  productMainImages: {} as Record<string, number>,
  productImageFrames: {} as Record<string, HomeImageFrame>,
  enabled: true,
});
export function HomeDisplayAdmin() {
  const owner = useOwner();
  const [products, setProducts] = useState<Product[]>();
  const [section, setSection] = useState<Section>();
  const [draft, setDraft] = useState(empty);
  const [baseline, setBaseline] = useState('');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);
  const [query, setQuery] = useState('');
  const [preview, setPreview] = useState(false);
  const [mobile, setMobile] = useState(false);
  const [publish, setPublish] = useState(false);
  const [editingFrame, setEditingFrame] = useState<{
    productId: number;
    assetId: number;
    src: string;
    name: string;
  }>();
  const dirty = !!baseline && JSON.stringify(draft) !== baseline;
  useUnsavedWarning(dirty);
  function apply(s?: Section) {
    const d = s
      ? {
          ...empty(),
          ...s.content,
          productIds: s.content.productIds ?? [],
          productMainImages: s.content.productMainImages ?? {},
          productImageFrames: s.content.productImageFrames ?? {},
          enabled: s.content.enabled ?? true,
        }
      : empty();
    setSection(s);
    setDraft(d);
    setBaseline(JSON.stringify(d));
  }
  async function load() {
    setBusy(true);
    setError('');
    try {
      const all: Product[] = [];
      for (let page = 1; ; page++) {
        const r = await api<Page<Product>>(`/admin/products?page=${page}&size=100`);
        all.push(...r.items);
        if (!r.items.length || all.length >= r.total) break;
      }
      const content = await api<Section[]>('/admin/content');
      setProducts(all);
      apply(content.find((s) => s.sectionKey === 'HomeProducts'));
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  useEffect(() => {
    void load();
  }, []);
  async function save() {
    if (safeLink(draft.linkUrl, 'INVALID') === 'INVALID') {
      setError('Đường dẫn nút phải bắt đầu bằng / hoặc https://.');
      return;
    }
    setBusy(true);
    setError('');
    setMessage('');
    try {
      const s = await send<Section>('/admin/content/HomeProducts', 'PUT', {
        ...draft,
        version: section?.version,
      });
      if (
        Object.keys(draft.productImageFrames).some((key) => !s.content.productImageFrames?.[key])
      ) {
        setSection(s);
        throw new Error(
          'Backend chưa lưu được cấu hình căn ảnh. Cần cập nhật/khởi động lại backend mới trước khi lưu lại; cách căn đang nhập vẫn được giữ trên màn hình này.',
        );
      }
      apply(s);
      setMessage('Đã lưu nháp. Website chưa thay đổi.');
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function doPublish() {
    if (dirty || !section) return;
    setBusy(true);
    setError('');
    try {
      apply(
        await send<Section>('/admin/content/HomeProducts/publish', 'POST', {
          version: section.version,
        }),
      );
      setPublish(false);
      setMessage('Đã áp dụng trưng bày lên website.');
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  function move(i: number, d: number) {
    const ids = [...draft.productIds];
    [ids[i], ids[i + d]] = [ids[i + d], ids[i]];
    setDraft({ ...draft, productIds: ids });
  }
  const valid = (p: Product) => p.isActive && p.isPublished;
  return (
    <>
      <Heading
        title="Trưng bày trang chủ"
        description="Chọn sản phẩm, xếp thứ tự, xem trước rồi công bố. Số hiển thị bằng số sản phẩm hợp lệ đã chọn."
      >
        <button
          className="small-button"
          disabled={busy}
          onClick={() => {
            if (!dirty || confirm('Bỏ thay đổi chưa lưu và tải lại?')) void load();
          }}
        >
          Tải lại
        </button>
      </Heading>
      {error && <ErrorBox text={error} />} {message && <Success>{message}</Success>}
      {!products ? (
        <Loading />
      ) : (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            void save();
          }}
        >
          <fieldset disabled={busy}>
            <Panel>
              <h2>Nội dung khu trưng bày</h2>
              <div className="admin-help" role="note">
                <p>
                  “Sản phẩm nổi bật” là tiêu đề cố định. Tiêu đề bạn nhập bên dưới hiển thị thành
                  dòng phụ, ví dụ “Bữa ngon, gần bạn hơn.”; không nhập lặp “Sản phẩm nổi bật”. Lời
                  giới thiệu là đoạn bổ sung, có thể để trống để trang gọn hơn.
                </p>
                <p>
                  Tên, mô tả ngắn, ảnh, khối lượng và giá lấy từ{' '}
                  <Link to="/quan-tri/san-pham">Sản phẩm</Link>, không nhập lại ở đây. Thay đổi
                  thông tin của sản phẩm đã công bố có thể xuất hiện trên website sau khi lưu sản
                  phẩm, không đợi công bố lại cấu hình trưng bày.
                </p>
                <p>
                  Danh sách và thứ tự tại đây được ưu tiên sau khi công bố, thay cho dấu “Sản phẩm
                  nổi bật” trong form sản phẩm. Xem trước dùng cấu hình đang nhập và dữ liệu sản
                  phẩm hiện tại; không lưu bản sao giá hoặc nội dung sản phẩm.
                </p>
              </div>
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={draft.enabled}
                  onChange={(e) => setDraft({ ...draft, enabled: e.target.checked })}
                />
                Hiển thị khu sản phẩm trên trang chủ
              </label>
              <label>
                Dòng phụ dưới “Sản phẩm nổi bật” *
                <input
                  required
                  maxLength={200}
                  value={draft.title}
                  onChange={(e) => setDraft({ ...draft, title: e.target.value })}
                />
              </label>
              <label>
                Lời giới thiệu
                <textarea
                  maxLength={20000}
                  rows={2}
                  value={draft.body ?? ''}
                  onChange={(e) => setDraft({ ...draft, body: e.target.value })}
                />
              </label>
              <label>
                Chữ nút xem danh sách
                <input
                  required
                  maxLength={100}
                  value={draft.linkLabel ?? ''}
                  onChange={(e) => setDraft({ ...draft, linkLabel: e.target.value })}
                />
              </label>
              <label>
                Đường dẫn nút xem danh sách
                <input
                  required
                  maxLength={500}
                  placeholder="/san-pham"
                  value={draft.linkUrl ?? ''}
                  onChange={(e) => setDraft({ ...draft, linkUrl: e.target.value })}
                />
                <small>
                  Mặc định /san-pham. Dùng đường dẫn bắt đầu bằng / hoặc https://; không nhập mã
                  HTML.
                </small>
              </label>
            </Panel>
            <Panel>
              <h2>Đang chọn {draft.productIds.length} sản phẩm</h2>
              <p className="muted">
                Không có số lượng mặc định tự cắt danh sách. Tối đa 50 sản phẩm; chọn ít sản phẩm
                chủ lực giúp trang gọn hơn.
              </p>
              <div className="home-display-selected">
                {draft.productIds.map((id, i) => {
                  const p = products.find((p) => p.id === id);
                  return (
                    <div className="home-display-row" key={id}>
                      {p?.images[0] && <img src={p.images[0].url} alt="" />}
                      <div>
                        <b>
                          {i + 1}. {p?.name ?? `Sản phẩm #${id} không còn tồn tại`}
                        </b>
                        <small>
                          {p && valid(p)
                            ? `${p.images.length} ảnh · ${p.variants.filter((v) => v.isActive).length} quy cách`
                            : '⚠ Không còn hợp lệ; cần bỏ hoặc thay thế.'}
                        </small>
                        {p && (
                          <>
                            <Link to={`/quan-tri/san-pham/${id}`}>Chỉnh sản phẩm / ảnh →</Link>
                            <div
                              className="display-main-picker"
                              aria-label={`Chọn ảnh chính của ${p.name}`}
                            >
                              {p.images.map((im, index) => (
                                <button
                                  type="button"
                                  key={im.id}
                                  aria-label={`Dùng ảnh ${index + 1} làm ảnh chính cho ${p.name}`}
                                  aria-pressed={
                                    (draft.productMainImages[String(id)] ??
                                      p.images[0]?.assetId) === im.assetId
                                  }
                                  onClick={() =>
                                    setDraft({
                                      ...draft,
                                      productMainImages: {
                                        ...draft.productMainImages,
                                        [String(id)]: im.assetId,
                                      },
                                      productImageFrames: Object.fromEntries(
                                        Object.entries(draft.productImageFrames).filter(
                                          ([key, value]) =>
                                            key !== String(id) || value.assetId === im.assetId,
                                        ),
                                      ),
                                    })
                                  }
                                >
                                  <img src={im.url} alt={im.altText || `Ảnh ${index + 1}`} />
                                  <span>Ảnh {index + 1}</span>
                                </button>
                              ))}
                            </div>
                            {owner && p.images.length > 0 && (
                              <button
                                type="button"
                                className="small-button"
                                onClick={() => {
                                  const im =
                                    p.images.find(
                                      (im) => im.assetId === draft.productMainImages[String(id)],
                                    ) ?? p.images[0];
                                  setEditingFrame({
                                    productId: id,
                                    assetId: im.assetId,
                                    src: im.url,
                                    name: p.name,
                                  });
                                }}
                              >
                                Căn ảnh trang chủ
                              </button>
                            )}
                          </>
                        )}
                      </div>
                      <div className="button-row">
                        <button
                          type="button"
                          className="small-button"
                          disabled={i === 0 || busy}
                          aria-label={`Đưa sản phẩm ${id} lên`}
                          onClick={() => move(i, -1)}
                        >
                          ↑
                        </button>
                        <button
                          type="button"
                          className="small-button"
                          disabled={i === draft.productIds.length - 1 || busy}
                          aria-label={`Đưa sản phẩm ${id} xuống`}
                          onClick={() => move(i, 1)}
                        >
                          ↓
                        </button>
                        <button
                          type="button"
                          className="small-button"
                          onClick={() =>
                            setDraft({
                              ...draft,
                              productIds: draft.productIds.filter((x) => x !== id),
                              productMainImages: Object.fromEntries(
                                Object.entries(draft.productMainImages).filter(
                                  ([key]) => key !== String(id),
                                ),
                              ),
                              productImageFrames: Object.fromEntries(
                                Object.entries(draft.productImageFrames).filter(
                                  ([key]) => key !== String(id),
                                ),
                              ),
                            })
                          }
                        >
                          Bỏ khỏi trang chủ
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
              <label>
                Thêm sản phẩm trưng bày
                <input
                  placeholder="Tìm sản phẩm đã công bố…"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                />
              </label>
              <div className="admin-sku-options">
                {products
                  .filter(
                    (p) =>
                      valid(p) &&
                      !draft.productIds.includes(p.id) &&
                      p.name.toLocaleLowerCase('vi').includes(query.toLocaleLowerCase('vi')),
                  )
                  .map((p) => (
                    <button
                      type="button"
                      className="admin-sku-option"
                      disabled={draft.productIds.length >= 50}
                      key={p.id}
                      onClick={() =>
                        setDraft({ ...draft, productIds: [...draft.productIds, p.id] })
                      }
                    >
                      {p.images[0] && (
                        <img
                          src={p.images[0].url}
                          alt=""
                          style={{ width: 45, height: 45, objectFit: 'contain' }}
                        />
                      )}
                      <span>{p.name}</span>+ Thêm
                    </button>
                  ))}
              </div>
              <p className="admin-help">
                Bấm ảnh thu nhỏ ở từng sản phẩm để chọn ảnh chính trang chủ. Các ảnh khác vẫn làm
                ảnh phụ. Giá và nội dung dùng thông tin sản phẩm hiện hành. Lưu nháp không tạo bản
                sao độc lập của sản phẩm. Owner dùng “Căn ảnh trang chủ” để kéo/phóng ảnh trong
                khung, có tùy chọn riêng cho điện thoại. Chỉ ảnh chính trang chủ thay đổi; ảnh gốc,
                trang chi tiết và ảnh phóng to giữ nguyên. Đổi ảnh chính sẽ đặt lại cách căn của sản
                phẩm. Cần lưu nháp và công bố để áp dụng.
              </p>
            </Panel>
            <div className="admin-savebar">
              <span>
                {dirty
                  ? 'Có thay đổi chưa lưu'
                  : section
                    ? 'Đang xem bản đã lưu'
                    : 'Chưa có cấu hình lưu'}
              </span>
              <div className="button-row">
                <button className="button" disabled={busy}>
                  Lưu bản nháp
                </button>
                <button type="button" className="small-button" onClick={() => setPreview(true)}>
                  Xem trước trang chủ
                </button>
                {owner && (
                  <button
                    type="button"
                    className="small-button"
                    disabled={!section || dirty || busy}
                    onClick={() => setPublish(true)}
                  >
                    Áp dụng lên website
                  </button>
                )}
              </div>
            </div>
          </fieldset>
        </form>
      )}
      {editingFrame && (
        <ImageFrameEditor
          key={`${editingFrame.productId}:${editingFrame.assetId}`}
          assetId={editingFrame.assetId}
          src={editingFrame.src}
          name={editingFrame.name}
          initial={frameForAsset(
            draft.productImageFrames[String(editingFrame.productId)],
            editingFrame.assetId,
          )}
          close={() => setEditingFrame(undefined)}
          apply={(frame) => {
            setDraft((d) => ({
              ...d,
              productMainImages: {
                ...d.productMainImages,
                [String(editingFrame.productId)]: editingFrame.assetId,
              },
              productImageFrames: {
                ...d.productImageFrames,
                [String(editingFrame.productId)]: frame,
              },
            }));
            setEditingFrame(undefined);
          }}
        />
      )}
      {preview && (
        <Modal
          title="Bản xem trước — chưa áp dụng"
          className="home-display-preview-modal"
          close={() => setPreview(false)}
        >
          <div className="button-row">
            <button className="small-button" onClick={() => setMobile(false)}>
              Máy tính
            </button>
            <button className="small-button" onClick={() => setMobile(true)}>
              Điện thoại
            </button>
          </div>
          <p className="admin-help">
            Đang xem cấu hình hiện nhập và dữ liệu sản phẩm hiện tại; mọi đường dẫn được khóa, không
            tạo giỏ hoặc đơn thật.
          </p>
          <div
            className={`display-preview ${mobile ? 'mobile' : ''}`}
            onClickCapture={(e) => {
              if ((e.target as Element).closest('a')) e.preventDefault();
            }}
          >
            <StoreProvider>
              <Home displayConfiguration={draft} displayItems={products} displayPreview />
            </StoreProvider>
          </div>
        </Modal>
      )}
      {publish && (
        <Modal title="Áp dụng bản đã lưu lên website?" close={() => !busy && setPublish(false)}>
          <p>
            Cấu hình đã lưu sẽ thay thế khu sản phẩm trên trang chủ. Không chọn sản phẩm hoặc tắt
            hiển thị sẽ ẩn khu này.
          </p>
          {error && <ErrorBox text={error} />}
          <button className="button" disabled={busy} onClick={doPublish}>
            Xác nhận áp dụng
          </button>
        </Modal>
      )}
    </>
  );
}
