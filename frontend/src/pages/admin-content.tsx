import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { Check, Eye, Save } from 'lucide-react';
import { date, safeLink, send } from '../lib/api';
import { useResource } from '../lib/useResource';
import { useOwner } from '../lib/admin-session';
import { sectionNames } from '../lib/admin-forms';
import { useUnsavedWarning } from '../lib/useUnsavedWarning';
import type { Section } from '../lib/types';
import { Heading, Panel } from '../components/admin-ui';
import { AdminAssetPicker, SelectedAssets } from '../components/AdminAssetPicker';
import { ErrorBox, Loading, Modal, Success } from '../components/ui';

const blank = () => ({ title: '', body: '', assetIds: [] as number[], linkLabel: '', linkUrl: '' });
export function ContentWorkspace() {
  const owner = useOwner();
  const r = useResource<Section[]>('/admin/content');
  const [key, setKey] = useState('HomeHero');
  const [snapshot, setSnapshot] = useState<Section>();
  const [draft, setDraft] = useState(blank);
  const [baseline, setBaseline] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [publish, setPublish] = useState(false);
  const dirty = baseline !== '' && JSON.stringify(draft) !== baseline;
  useUnsavedWarning(dirty);
  function apply(s?: Section) {
    const d = s
      ? {
          title: s.content.title,
          body: s.content.body ?? '',
          assetIds: s.content.assetIds,
          linkLabel: s.content.linkLabel ?? '',
          linkUrl: s.content.linkUrl ?? '',
        }
      : blank();
    setDraft(d);
    setBaseline(JSON.stringify(d));
    setSnapshot(s);
  }
  useEffect(() => {
    if (r.data) apply(r.data.find((s) => s.sectionKey === key));
  }, [key, r.data]);
  function switchKey(k: string) {
    if (dirty && !confirm('Nội dung chưa lưu. Chuyển mục và bỏ thay đổi?')) return;
    setKey(k);
    setError('');
    setMessage('');
  }
  async function save(e: FormEvent) {
    e.preventDefault();
    if (draft.linkUrl && safeLink(draft.linkUrl, 'INVALID') === 'INVALID') {
      setError('Đường dẫn nút phải bắt đầu bằng / hoặc https://.');
      return;
    }
    setBusy(true);
    setError('');
    setMessage('');
    try {
      const s = await send<Section>(`/admin/content/${key}`, 'PUT', {
        ...draft,
        linkLabel: draft.linkLabel || null,
        linkUrl: draft.linkUrl || null,
        version: snapshot?.version,
      });
      apply(s);
      r.setData((items) => [...(items ?? []).filter((x) => x.sectionKey !== key), s]);
      setMessage('Đã lưu bản nháp. Website chưa thay đổi; Owner có thể công bố bản vừa lưu.');
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function doPublish() {
    if (!snapshot || dirty) return;
    setBusy(true);
    setError('');
    try {
      const s = await send<Section>(`/admin/content/${key}/publish`, 'POST', {
        version: snapshot.version,
      });
      apply(s);
      r.setData((items) => [...(items ?? []).filter((x) => x.sectionKey !== key), s]);
      setPublish(false);
      setMessage('Đã công bố bản đã lưu lên website.');
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <Heading
        title="Nội dung website"
        description="Chọn khu vực, sửa nội dung và ảnh. Lưu nháp trước khi công bố."
      >
        <button
          className="small-button"
          disabled={busy}
          onClick={() => {
            if (!dirty || confirm('Bỏ thay đổi chưa lưu và tải lại dữ liệu?')) r.reload();
          }}
        >
          Tải lại dữ liệu
        </button>
        <Link className="small-button" to="/" target="_blank">
          <Eye size={17} />
          Xem website
        </Link>
      </Heading>
      {r.loading ? (
        <Loading />
      ) : r.error ? (
        <ErrorBox text={r.error} retry={r.reload} />
      ) : (
        <div className="admin-content-grid">
          <nav className="admin-section-list" aria-label="Khu vực nội dung">
            {Object.entries(sectionNames).map(([k, label]) => (
              <button
                type="button"
                disabled={busy}
                aria-current={key === k ? 'page' : undefined}
                className={key === k ? 'active' : ''}
                onClick={() => switchKey(k)}
                key={k}
              >
                {label}
                <small>
                  {r.data?.find((s) => s.sectionKey === k)?.publishedAtUtc
                    ? 'Đã từng công bố'
                    : 'Chưa công bố'}
                </small>
              </button>
            ))}
          </nav>
          <form className="admin-content-form" onSubmit={save}>
            <fieldset disabled={busy} className="form-stack">
              <Panel>
                <div className="panel-title">
                  <h2>{sectionNames[key]}</h2>
                  <span className={`status ${dirty ? 'status-PendingConfirmation' : ''}`}>
                    {dirty
                      ? 'Có thay đổi chưa lưu'
                      : snapshot
                        ? 'Bản nháp đã lưu'
                        : 'Chưa có bản nháp'}
                  </span>
                </div>
                <p className="muted">
                  Công bố gần nhất: {date(snapshot?.publishedAtUtc)}. Nội dung là văn bản thường,
                  không cần nhập HTML.
                </p>
                {error && <ErrorBox text={error} />} {message && <Success>{message}</Success>}
                <label>
                  Tiêu đề <span className="required">*</span>
                  <input
                    required
                    maxLength={200}
                    value={draft.title}
                    onChange={(e) => setDraft({ ...draft, title: e.target.value })}
                  />
                </label>
                <label>
                  Nội dung
                  <textarea
                    rows={7}
                    maxLength={20000}
                    value={draft.body}
                    onChange={(e) => setDraft({ ...draft, body: e.target.value })}
                  />
                </label>
                <div className="form-grid">
                  <label>
                    Chữ trên nút
                    <input
                      maxLength={100}
                      placeholder="Ví dụ: Khám phá sản phẩm"
                      value={draft.linkLabel}
                      onChange={(e) => setDraft({ ...draft, linkLabel: e.target.value })}
                    />
                  </label>
                  <label>
                    Đường dẫn nút
                    <input
                      maxLength={500}
                      list="content-links"
                      placeholder="/san-pham"
                      value={draft.linkUrl}
                      onChange={(e) => setDraft({ ...draft, linkUrl: e.target.value })}
                    />
                    <datalist id="content-links">
                      <option value="/san-pham" />
                      <option value="/cau-chuyen" />
                      <option value="/diem-ban" />
                    </datalist>
                  </label>
                </div>
              </Panel>
              <Panel>
                <h2>Hình ảnh</h2>
                <p className="muted">
                  {key === 'HomeHero'
                    ? 'Mỗi ảnh là một slide banner. Dùng nút lên/xuống để đổi thứ tự; khách có thể chuyển ảnh. Chuẩn bị ảnh ngang, chừa vùng trống bên trái cho chữ.'
                    : key === 'Story'
                      ? 'Ảnh đầu là ảnh chính; ảnh thứ hai và ba là ảnh phụ trong cụm câu chuyện.'
                      : 'Ảnh được liên kết với khu vực nội dung này.'}{' '}
                  Tối đa 20 ảnh.
                </p>
                <SelectedAssets
                  ids={draft.assetIds}
                  disabled={busy}
                  onChange={(assetIds) => setDraft({ ...draft, assetIds })}
                />
                <AdminAssetPicker
                  disabled={busy || draft.assetIds.length >= 20}
                  selected={draft.assetIds}
                  onChoose={(a) => {
                    setDraft((d) => ({
                      ...d,
                      assetIds: d.assetIds.includes(a.id) ? d.assetIds : [...d.assetIds, a.id],
                    }));
                  }}
                />
              </Panel>
              <div className="admin-savebar">
                <span>
                  {dirty
                    ? 'Bạn có thay đổi chưa lưu.'
                    : snapshot
                      ? 'Đang xem bản đã lưu.'
                      : 'Nhập nội dung để tạo bản nháp.'}
                </span>
                <div className="button-row">
                  <button className="button" disabled={busy || !draft.title.trim()}>
                    <Save size={17} />
                    {busy ? 'Đang xử lý…' : 'Lưu bản nháp'}
                  </button>
                  {owner && (
                    <button
                      type="button"
                      className="small-button"
                      disabled={busy || dirty || !snapshot}
                      onClick={() => setPublish(true)}
                    >
                      Công bố bản đã lưu
                    </button>
                  )}
                </div>
              </div>
              {dirty && owner && (
                <p className="admin-help">
                  Lưu bản nháp để bật nút công bố. Công bố sẽ đưa nội dung/ảnh đã lưu ra website.
                </p>
              )}
            </fieldset>
          </form>
          <aside className="admin-content-preview">
            <Panel>
              <h2>
                <Eye size={16} />
                Xem trước nội dung nhập
              </h2>
              <p className="admin-help">
                Đây là bản xem nội dung, bố cục trên website có thể khác.
              </p>
              {draft.assetIds[0] && (
                <img src={`/api/media/${draft.assetIds[0]}`} alt="Ảnh xem trước nội dung" />
              )}
              <h3>{draft.title || 'Tiêu đề sẽ hiển thị ở đây'}</h3>
              <p className="plain-text">{draft.body || 'Nội dung đang để trống.'}</p>
              {draft.linkLabel && (
                <span className="preview-link">
                  {draft.linkLabel} → {draft.linkUrl || 'Chưa có đường dẫn'}
                </span>
              )}
            </Panel>
          </aside>
        </div>
      )}
      {publish && (
        <Modal title="Công bố nội dung lên website?" close={() => !busy && setPublish(false)}>
          <p>
            Bản đã lưu của mục <b>{sectionNames[key]}</b> sẽ được hiển thị công khai, bao gồm ảnh đã
            chọn.
          </p>
          {error && <ErrorBox text={error} />}
          <div className="button-row">
            <button type="button" className="button" disabled={busy} onClick={doPublish}>
              <Check size={17} />
              Xác nhận công bố
            </button>
            <button
              type="button"
              className="small-button"
              disabled={busy}
              onClick={() => setPublish(false)}
            >
              Để sau
            </button>
          </div>
        </Modal>
      )}
    </>
  );
}
