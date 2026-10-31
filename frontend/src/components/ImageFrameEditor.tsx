import { useRef, useState, type PointerEvent } from 'react';
import type { HomeImageFrame, ImageFrameSettings } from '../lib/types';
import { clamp, defaultFrame, normalizeFrame } from '../lib/image-framing';
import { Modal } from './ui';
import { FramedProductImage } from './FramedProductImage';
export function ImageFrameEditor({
  assetId,
  src,
  name,
  initial,
  close,
  apply,
}: {
  assetId: number;
  src: string;
  name: string;
  initial?: HomeImageFrame;
  close: () => void;
  apply: (frame: HomeImageFrame) => void;
}) {
  const [draft, setDraft] = useState<HomeImageFrame>(() => ({
    assetId,
    desktop: normalizeFrame(initial?.desktop),
    mobile: initial?.mobile ? normalizeFrame(initial.mobile) : undefined,
  }));
  const [mode, setMode] = useState<'desktop' | 'mobile'>('desktop');
  const drag = useRef<{
    id: number;
    x: number;
    y: number;
    start: ImageFrameSettings;
    width: number;
    height: number;
  } | null>(null);
  const frame = mode === 'mobile' ? (draft.mobile ?? draft.desktop) : draft.desktop;
  const update = (next: ImageFrameSettings) =>
    setDraft((d) => ({
      ...d,
      [mode === 'mobile' && d.mobile ? 'mobile' : 'desktop']: normalizeFrame(next),
    }));
  function begin(e: PointerEvent<HTMLDivElement>) {
    if (frame.fit !== 'cover' || !e.isPrimary || e.button !== 0) return;
    const rect = e.currentTarget.getBoundingClientRect();
    drag.current = {
      id: e.pointerId,
      x: e.clientX,
      y: e.clientY,
      start: frame,
      width: rect.width,
      height: rect.height,
    };
    e.currentTarget.setPointerCapture(e.pointerId);
  }
  function move(e: PointerEvent<HTMLDivElement>) {
    const start = drag.current;
    if (!start || start.id !== e.pointerId) return;
    update({
      ...start.start,
      x: clamp(start.start.x - ((e.clientX - start.x) / start.width) * 100, 0, 100),
      y: clamp(start.start.y - ((e.clientY - start.y) / start.height) * 100, 0, 100),
    });
  }
  return (
    <Modal title={`Căn ảnh trang chủ — ${name}`} close={close} className="image-frame-editor">
      <p className="admin-help">
        Chỉ thay cách hiển thị ảnh chính trang chủ. Không sửa ảnh gốc, không tải thêm tệp lên R2. Áp
        dụng vào bản nháp, sau đó lưu và công bố ở Trưng bày trang chủ.
      </p>
      <div className="button-row">
        <button
          type="button"
          className="small-button"
          aria-pressed={mode === 'desktop'}
          onClick={() => setMode('desktop')}
        >
          Máy tính
        </button>
        <button
          type="button"
          className="small-button"
          aria-pressed={mode === 'mobile'}
          onClick={() => setMode('mobile')}
        >
          Điện thoại
        </button>
      </div>
      <label className="checkbox-label">
        <input
          type="checkbox"
          checked={!!draft.mobile}
          onChange={(e) => {
            drag.current = null;
            setDraft((d) => ({ ...d, mobile: e.target.checked ? { ...d.desktop } : undefined }));
          }}
        />
        Căn riêng cho điện thoại
      </label>
      {mode === 'mobile' && !draft.mobile && (
        <p className="admin-help">
          Điện thoại đang dùng chung cấu hình máy tính. Điều chỉnh tại đây cũng thay cấu hình máy
          tính; bật căn riêng để chỉnh độc lập.
        </p>
      )}
      <div
        className={`crop-stage ${mode === 'mobile' ? 'is-mobile' : ''} ${frame.fit === 'cover' ? 'is-draggable' : ''}`}
        onPointerDown={begin}
        onPointerMove={move}
        onPointerUp={() => {
          drag.current = null;
        }}
        onPointerCancel={() => {
          drag.current = null;
        }}
        onLostPointerCapture={() => {
          drag.current = null;
        }}
      >
        <FramedProductImage src={src} alt={name} frame={draft} mode={mode} />
      </div>
      <div className="crop-controls">
        <label>
          Cách hiển thị
          <select
            value={frame.fit}
            onChange={(e) =>
              update({ ...defaultFrame(), fit: e.target.value as ImageFrameSettings['fit'] })
            }
          >
            <option value="contain">Vừa khung — hiện toàn bộ ảnh</option>
            <option value="cover">Phủ kín khung — cắt phần ngoài khung</option>
          </select>
        </label>
        {frame.fit === 'cover' && (
          <>
            <p className="admin-help">
              Kéo ảnh bằng chuột hoặc chạm để căn. Có thể dùng các thanh điều chỉnh bên dưới bằng
              bàn phím.
            </p>
            <label>
              Phóng to: {frame.zoom.toFixed(2)}×
              <input
                type="range"
                min="1"
                max="3"
                step="0.01"
                value={frame.zoom}
                onChange={(e) => update({ ...frame, zoom: Number(e.target.value) })}
              />
            </label>
            <label>
              Vị trí ngang: {Math.round(frame.x)}%
              <input
                type="range"
                min="0"
                max="100"
                value={frame.x}
                onChange={(e) => update({ ...frame, x: Number(e.target.value) })}
              />
            </label>
            <label>
              Vị trí dọc: {Math.round(frame.y)}%
              <input
                type="range"
                min="0"
                max="100"
                value={frame.y}
                onChange={(e) => update({ ...frame, y: Number(e.target.value) })}
              />
            </label>
          </>
        )}
      </div>
      <div className="button-row">
        <button type="button" className="button" onClick={() => apply(draft)}>
          Áp dụng vào bản nháp
        </button>
        <button type="button" className="small-button" onClick={() => update(defaultFrame())}>
          Đặt lại khung đang xem
        </button>
        <button type="button" className="small-button" onClick={close}>
          Hủy
        </button>
      </div>
    </Modal>
  );
}
