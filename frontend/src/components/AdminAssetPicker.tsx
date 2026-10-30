import { useState, type DragEvent } from 'react';
import { ImagePlus, Upload, X } from 'lucide-react';
import { api } from '../lib/api';
import { useOwner } from '../lib/admin-session';
import { useResource } from '../lib/useResource';
import type { Asset, Page } from '../lib/types';
import { ErrorBox, Loading, Modal } from './ui';
import { Pager, validImage } from './admin-ui';

export function AdminAssetPicker({
  selected,
  onChoose,
  disabled = false,
}: {
  selected: number[];
  onChoose: (asset: Asset) => Promise<void> | void;
  disabled?: boolean;
}) {
  const owner = useOwner();
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [pending, setPending] = useState<Asset>();
  async function choose(asset: Asset) {
    setBusy(true);
    setError('');
    try {
      await onChoose(asset);
      setPending(undefined);
      setOpen(false);
    } catch (e) {
      setPending(asset);
      setError(
        `Ảnh #${asset.id} đã có trong thư viện nhưng chưa gắn được. ${(e as Error).message}`,
      );
    } finally {
      setBusy(false);
    }
  }
  async function upload(file?: File) {
    if (!file || busy || disabled || !validImage(file, setError)) return;
    setBusy(true);
    setError('');
    try {
      const f = new FormData();
      f.append('file', file);
      const a = await api<Asset>('/media', { method: 'POST', body: f });
      setPending(a);
      await onChoose(a);
      setPending(undefined);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  function drop(e: DragEvent) {
    e.preventDefault();
    void upload(e.dataTransfer.files[0]);
  }
  return (
    <div className="admin-asset-picker">
      {error && <ErrorBox text={error} />}
      <div className="asset-actions">
        <label className="admin-dropzone" onDragOver={(e) => e.preventDefault()} onDrop={drop}>
          <Upload size={22} />
          <span>
            <b>{busy ? 'Đang xử lý ảnh…' : 'Chọn hoặc kéo thả một ảnh vào đây'}</b>
            <small>PNG / JPEG / WebP, tối đa 5 MiB. Tự tối ưu sau khi tải.</small>
          </span>
          <input
            type="file"
            accept="image/png,image/jpeg,image/webp"
            disabled={busy || disabled}
            onChange={(e) => {
              void upload(e.target.files?.[0]);
              e.target.value = '';
            }}
          />
        </label>
        {owner && (
          <button
            className="small-button"
            type="button"
            disabled={busy || disabled}
            onClick={() => setOpen(true)}
          >
            <ImagePlus size={17} />
            Chọn ảnh đã tải
          </button>
        )}
      </div>
      {pending && (
        <div className="info-box">
          <p>Ảnh #{pending.id} đã được tải lên. Có thể thử gắn lại mà không cần upload lần nữa.</p>
          <button
            type="button"
            className="small-button"
            disabled={busy || disabled}
            onClick={() => choose(pending)}
          >
            Gắn lại ảnh này
          </button>
        </div>
      )}
      {open && (
        <Library
          selected={selected}
          busy={busy}
          choose={choose}
          close={() => !busy && setOpen(false)}
        />
      )}
    </div>
  );
}
function Library({
  selected,
  busy,
  choose,
  close,
}: {
  selected: number[];
  busy: boolean;
  choose: (a: Asset) => void;
  close: () => void;
}) {
  const [page, setPage] = useState(1);
  const r = useResource<Page<Asset>>(`/admin/media/assets?page=${page}&size=20`);
  return (
    <Modal title="Chọn ảnh từ thư viện" close={close} className="admin-library-modal">
      {r.loading ? (
        <Loading />
      ) : r.error ? (
        <ErrorBox text={r.error} retry={r.reload} />
      ) : (
        <>
          <div className="library-grid">
            {r.data?.items.map((a) => (
              <button
                type="button"
                className="library-asset"
                disabled={busy || selected.includes(a.id)}
                key={a.id}
                onClick={() => choose(a)}
              >
                <img src={a.url} alt={`Ảnh thư viện ${a.id}`} loading="lazy" />
                <span>
                  {selected.includes(a.id) ? 'Đã chọn' : `Chọn ảnh #${a.id}`}
                  <small>{Math.ceil(a.byteSize / 1024)} KB</small>
                </span>
              </button>
            ))}
          </div>
          {!r.data?.items.length && <p>Thư viện chưa có ảnh. Hãy tải ảnh mới từ form bên ngoài.</p>}
          <Pager page={page} total={r.data?.total ?? 0} setPage={setPage} />
        </>
      )}
    </Modal>
  );
}

export function SelectedAssets({
  ids,
  onChange,
  disabled = false,
}: {
  ids: number[];
  onChange: (ids: number[]) => void;
  disabled?: boolean;
}) {
  return (
    <div className="selected-assets">
      {ids.map((id, i) => (
        <div className="selected-asset" key={id}>
          <img src={`/api/media/${id}`} alt={`Ảnh đã chọn ${i + 1}`} />
          <span>
            <b>{i === 0 ? 'Ảnh chính' : `Ảnh ${i + 1}`}</b>
            <small>Ảnh #{id}</small>
          </span>
          <div>
            <button
              type="button"
              className="small-button"
              disabled={disabled || i === 0}
              aria-label={`Đưa ảnh ${id} lên trước`}
              onClick={() => {
                const next = [...ids];
                [next[i - 1], next[i]] = [next[i], next[i - 1]];
                onChange(next);
              }}
            >
              ↑
            </button>
            <button
              type="button"
              className="small-button"
              disabled={disabled || i === ids.length - 1}
              aria-label={`Đưa ảnh ${id} xuống sau`}
              onClick={() => {
                const next = [...ids];
                [next[i], next[i + 1]] = [next[i + 1], next[i]];
                onChange(next);
              }}
            >
              ↓
            </button>
            <button
              type="button"
              className="icon-button"
              disabled={disabled}
              aria-label={`Bỏ chọn ảnh ${id}`}
              onClick={() => onChange(ids.filter((x) => x !== id))}
            >
              <X size={17} />
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}
