import { useState } from 'react';
import { Link } from 'react-router-dom';
import { ApiError, send } from '../lib/api';
import { Modal, ErrorBox } from './ui';
export function DeleteRecord({
  path,
  version,
  label,
  kind,
  deleted,
}: {
  path: string;
  version?: string;
  label: string;
  kind: 'sản phẩm' | 'lô hàng';
  deleted: () => void;
}) {
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [code, setCode] = useState('');
  const [checked, setChecked] = useState(false);
  return (
    <>
      <button
        type="button"
        className="small-button danger-button"
        onClick={() => {
          setOpen(true);
          setError('');
          setCode('');
          setChecked(false);
        }}
      >
        Xóa {kind}
      </button>
      {open && (
        <Modal title={`Xóa ${kind}?`} close={() => !busy && setOpen(false)}>
          <p>
            <b>{label}</b>
          </p>
          <p>
            Chỉ xóa được bản ghi chưa phát sinh nghiệp vụ. Dữ liệu đã dùng cho đơn hàng, lô, tồn
            kho, chứng từ hoặc kiểm đếm được giữ lại. Tệp ảnh vẫn nằm trong thư viện.
          </p>
          {error && (
            <>
              <ErrorBox text={error} />
              {code === 'PRODUCT_ON_DISPLAY' && (
                <Link className="text-link" to="/quan-tri/trung-bay">
                  Mở cấu hình trưng bày →
                </Link>
              )}
              {code === 'PRODUCT_IN_USE' && (
                <Link className="text-link" to={`/quan-tri/san-pham/${path.split('/').at(-1)}`}>
                  Mở sản phẩm để ẩn/ngừng hoạt động →
                </Link>
              )}
              {code === 'VERSION_CONFLICT' && (
                <button
                  type="button"
                  className="small-button"
                  onClick={() => {
                    setOpen(false);
                    deleted();
                  }}
                >
                  Đóng và tải lại danh sách
                </button>
              )}
            </>
          )}
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={checked}
              disabled={busy}
              onChange={(e) => setChecked(e.target.checked)}
            />
            Tôi hiểu thao tác xóa này không thể hoàn tác.
          </label>
          <div className="button-row">
            <button
              type="button"
              className="button danger-button"
              disabled={!checked || busy}
              onClick={async () => {
                setBusy(true);
                setError('');
                try {
                  await send(path, 'DELETE', { version });
                  setOpen(false);
                  deleted();
                } catch (e) {
                  setError((e as Error).message);
                  setCode(e instanceof ApiError ? e.code : '');
                } finally {
                  setBusy(false);
                }
              }}
            >
              {busy ? 'Đang xóa…' : 'Xác nhận xóa'}
            </button>
            <button className="small-button" disabled={busy} onClick={() => setOpen(false)}>
              Giữ lại
            </button>
          </div>
        </Modal>
      )}
    </>
  );
}
