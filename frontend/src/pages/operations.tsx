import { DeleteRecord } from '../components/DeleteRecord';
import { useState, type FormEvent } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import { api, date, send } from '../lib/api';
import type { Gym, Page, Product } from '../lib/types';
import { Empty, ErrorBox, Loading, Modal, Status, Success } from '../components/ui';
import { Field, Heading, Pager, Panel } from '../components/admin-ui';
import { useOwner } from '../lib/admin-session';
import { useResource } from '../lib/useResource';

interface Batch {
  id: number;
  variantId: number;
  batchCode: string;
  manufacturedOn: string;
  expiresAtUtc: string;
  isSaleBlocked: boolean;
  version: string;
}
interface Location {
  id: number;
  name: string;
  locationCode: string;
  kind: string;
}
interface Balance {
  id: number;
  batchId: number;
  batchCode: string;
  variantId: number;
  condition: string;
  onHand: number;
  reserved: number;
  available: number;
  expiresAtUtc: string;
}
interface Stock {
  gymId: number;
  lastCountedAtUtc?: string;
  isRealtime: false;
  version: string;
  lastCountLines: {
    id: number;
    batchId: number;
    actualGood: number;
    actualQuarantine: number;
    actualDamaged: number;
    version: string;
  }[];
  bookEstimate: { batchId: number; good: number; quarantine: number; damaged: number }[];
}
export function Inventory() {
  const owner = useOwner();
  const [tab, setTab] = useState('batches');
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState('active');
  const [page, setPage] = useState(1);
  const [dialog, setDialog] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const path = tab === 'batches' ? 'batches' : tab === 'documents' ? 'documents' : 'balances';
  const r = useResource<
    Page<
      Balance &
        Batch & { documentNo: string; documentType: string; effectiveAtUtc: string; reason: string }
    >
  >(
    `/admin/inventory/${path}?page=${page}&size=20${tab === 'batches' ? `&search=${encodeURIComponent(query)}&status=${status}` : ''}`,
  );
  const loc = useResource<Location[]>('/admin/inventory/locations');
  return (
    <>
      <Heading
        title="Kho & lô hàng"
        description="Kho online tách biệt hàng ký gửi. Xuất theo hạn dùng, giữ đúng số lượng đã xác nhận."
      >
        <button className="small-button" onClick={() => setDialog('batch')}>
          <Plus size={16} />
          Tạo lô
        </button>
        <button className="button" onClick={() => setDialog('movement')}>
          Lập phiếu kho
        </button>
      </Heading>
      {message && <Success>{message}</Success>}
      {error && <ErrorBox text={error} />}{' '}
      {tab === 'batches' && (
        <form
          className="admin-toolbar"
          onSubmit={(e) => {
            e.preventDefault();
            setPage(1);
            setQuery(search);
          }}
        >
          <input
            aria-label="Tìm lô hàng"
            placeholder="Mã lô, SKU, tên sản phẩm…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <select
            aria-label="Lọc trạng thái"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
          >
            <option value="active">Chưa khóa và còn hạn</option>
            <option value="blocked">Đã khóa bán</option>
            <option value="expired">Đã hết hạn</option>
            <option value="">Tất cả</option>
          </select>
          <button className="button">Tìm kiếm</button>
        </form>
      )}
      {owner && loc.data && !loc.data.some((l) => l.kind === 'Warehouse') && (
        <Panel>
          <p>Chưa có kho chính. Khởi tạo MAIN để bắt đầu nhập thành phẩm.</p>
          <button
            className="button"
            onClick={async () => {
              try {
                await send('/admin/inventory/warehouse', 'POST');
                loc.reload();
                setMessage('Đã khởi tạo kho chính.');
              } catch (e) {
                setError((e as Error).message);
              }
            }}
          >
            Khởi tạo kho MAIN
          </button>
        </Panel>
      )}
      <div className="tabs">
        {[
          ['balances', 'Tồn kho online'],
          ['batches', 'Lô & hạn dùng'],
          ['documents', 'Chứng từ kho'],
        ].map(([key, label]) => (
          <button
            key={key}
            className={tab === key ? 'active' : ''}
            onClick={() => {
              setTab(key);
              setPage(1);
            }}
          >
            {label}
          </button>
        ))}
      </div>
      <Panel>
        {r.loading ? (
          <Loading />
        ) : r.error ? (
          <ErrorBox text={r.error} retry={r.reload} />
        ) : r.data?.items.length ? (
          <>
            <div className="responsive-table">
              <table>
                <thead>
                  <tr>
                    {(tab === 'balances'
                      ? ['Lô hàng', 'Tình trạng', 'Đang có', 'Đã giữ', 'Khả dụng', 'Hết hạn']
                      : tab === 'batches'
                        ? [
                            'Mã lô',
                            'SKU ID',
                            'Ngày sản xuất',
                            'Hết hạn',
                            'Trạng thái',
                            ...(owner ? ['Thao tác'] : []),
                          ]
                        : ['Chứng từ', 'Loại phiếu', 'Thời gian', 'Lý do']
                    ).map((x) => (
                      <th key={x}>{x}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {r.data.items.map((x) => (
                    <tr key={x.id}>
                      {tab === 'balances' ? (
                        <>
                          <td>
                            <b>{x.batchCode}</b>
                            <small>SKU #{x.variantId}</small>
                          </td>
                          <td>
                            <Status value={x.condition} />
                          </td>
                          <td>{x.onHand}</td>
                          <td>{x.reserved}</td>
                          <td>
                            <b>{x.available}</b>
                          </td>
                          <td>{date(x.expiresAtUtc)}</td>
                        </>
                      ) : tab === 'batches' ? (
                        <>
                          <td>
                            <b>{x.batchCode}</b>
                          </td>
                          <td>#{x.variantId}</td>
                          <td>{x.manufacturedOn}</td>
                          <td>{date(x.expiresAtUtc)}</td>
                          <td>{x.isSaleBlocked ? 'Đã khóa' : 'Không khóa'}</td>
                          {owner && (
                            <td>
                              <DeleteRecord
                                kind="lô hàng"
                                label={x.batchCode}
                                path={`/admin/inventory/batches/${x.id}`}
                                version={x.version}
                                deleted={r.reload}
                              />
                            </td>
                          )}
                        </>
                      ) : (
                        <>
                          <td>
                            <b>{x.documentNo}</b>
                          </td>
                          <td>{x.documentType}</td>
                          <td>{date(x.effectiveAtUtc)}</td>
                          <td>{x.reason}</td>
                        </>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pager page={page} total={r.data.total} setPage={setPage} />
          </>
        ) : (
          <Empty title="Chưa có dữ liệu kho">
            <p>Tạo lô rồi lập phiếu nhập thành phẩm để tăng tồn kho.</p>
          </Empty>
        )}
      </Panel>
      {dialog === 'batch' && (
        <BatchForm
          close={() => setDialog('')}
          saved={() => {
            setDialog('');
            setMessage('Đã tạo lô mới.');
            r.reload();
          }}
        />
      )}
      {dialog === 'movement' && (
        <MovementForm
          close={() => setDialog('')}
          saved={() => {
            setDialog('');
            setMessage('Đã chốt chứng từ kho.');
            r.reload();
          }}
        />
      )}
    </>
  );
}
function BatchForm({ close, saved }: { close: () => void; saved: () => void }) {
  const r = useResource<Page<Product>>('/admin/products?size=100');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setBusy(true);
    setError('');
    try {
      await send('/admin/inventory/batches', 'POST', {
        variantId: Number(f.get('variantId')),
        batchCode: f.get('batchCode'),
        manufacturedOn: f.get('manufacturedOn'),
        expiresAtUtc: new Date(String(f.get('expiresAt')) + '+07:00').toISOString(),
        notes: f.get('notes') || null,
      });
      saved();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <Modal title="Tạo lô thành phẩm" close={close}>
      {r.loading ? (
        <Loading />
      ) : r.error ? (
        <ErrorBox text={r.error} />
      ) : (
        <form onSubmit={submit} className="form-stack">
          {error && <ErrorBox text={error} />}
          <label>
            Phân loại sản phẩm
            <select name="variantId" required>
              <option value="">Chọn SKU</option>
              {r.data?.items.flatMap((p) =>
                p.variants.map((v) => (
                  <option key={v.id} value={v.id}>
                    {p.name} · {v.label} ({v.skuCode})
                  </option>
                )),
              )}
            </select>
          </label>
          {(r.data?.total ?? 0) > 100 && (
            <p className="warning-text">
              Đang hiển thị 100 sản phẩm đầu; bộ chọn đầy đủ sẽ nâng cấp cùng tìm SKU.
            </p>
          )}
          <Field label="Mã lô" name="batchCode" required maxLength={60} pattern="[A-Za-z0-9_-]+" />
          <Field label="Ngày sản xuất (Việt Nam)" name="manufacturedOn" type="date" required />
          <Field
            label="Thời điểm hết hạn (giờ Việt Nam)"
            name="expiresAt"
            type="datetime-local"
            required
          />
          <Field label="Ghi chú" name="notes" maxLength={1000} />
          <p className="muted">Tạo lô chưa tăng tồn. Lập phiếu nhập thành phẩm sau bước này.</p>
          <button className="button" disabled={busy}>
            Tạo lô
          </button>
        </form>
      )}
    </Modal>
  );
}
function MovementForm({ close, saved }: { close: () => void; saved: () => void }) {
  const owner = useOwner();
  const batches = useResource<Page<Batch>>('/admin/inventory/batches?size=100');
  const locations = useResource<Location[]>('/admin/inventory/locations');
  const [kind, setKind] = useState('ProductionReceipt');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [key, setKey] = useState({ payload: '', key: crypto.randomUUID() });
  const warehouse = locations.data?.find((l) => l.kind === 'Warehouse');
  const gyms = locations.data?.filter((l) => l.kind === 'Gym') ?? [];
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    const gym = Number(f.get('gymId'));
    const condition = String(f.get('condition') ?? 'Good');
    const data = {
      documentType: kind,
      reason: f.get('reason'),
      lines: [
        {
          batchId: Number(f.get('batchId')),
          quantity: Number(f.get('quantity')),
          fromLocationId:
            kind === 'ProductionReceipt' ? null : kind === 'GymRecall' ? gym : warehouse?.id,
          fromCondition: kind === 'ProductionReceipt' ? null : condition,
          toLocationId: kind === 'WriteOff' ? null : kind === 'GymDelivery' ? gym : warehouse?.id,
          toCondition: kind === 'WriteOff' ? null : kind === 'GymRecall' ? 'Quarantine' : 'Good',
          note: null,
        },
      ],
      correctsDocumentId: null,
    };
    const payload = JSON.stringify(data);
    const op = key.payload === payload ? key.key : crypto.randomUUID();
    setKey({ payload, key: op });
    setBusy(true);
    setError('');
    try {
      await send('/admin/inventory/documents', 'POST', { ...data, operationKey: op });
      saved();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <Modal title="Lập phiếu kho" close={close}>
      {batches.error || locations.error ? (
        <ErrorBox text={batches.error || locations.error} />
      ) : batches.loading || locations.loading ? (
        <Loading />
      ) : (
        <form onSubmit={submit} className="form-stack">
          {error && <ErrorBox text={error} />}
          <label>
            Loại chứng từ
            <select value={kind} onChange={(e) => setKind(e.target.value)}>
              <option value="ProductionReceipt">Nhập thành phẩm → kho MAIN</option>
              <option value="GymDelivery">Giao ký gửi → phòng gym</option>
              <option value="GymRecall">Thu hồi từ gym → kho cách ly</option>
              <option value="WriteOff">Hủy hàng tại kho MAIN</option>
            </select>
          </label>
          <label>
            Lô hàng
            <select name="batchId" required>
              <option value="">Chọn lô</option>
              {batches.data?.items.map((b) => (
                <option value={b.id} key={b.id}>
                  {b.batchCode} · SKU #{b.variantId} · HSD {date(b.expiresAtUtc)}
                </option>
              ))}
            </select>
          </label>
          {(batches.data?.total ?? 0) > 100 && (
            <p className="warning-text">
              Đang hiển thị 100 lô đầu; không tự chọn lô ngoài danh sách.
            </p>
          )}
          {['GymDelivery', 'GymRecall'].includes(kind) && (
            <label>
              Phòng gym
              <select name="gymId" required>
                <option value="">Chọn điểm ký gửi</option>
                {gyms.map((l) => (
                  <option key={l.id} value={l.id}>
                    {l.name}
                  </option>
                ))}
              </select>
            </label>
          )}
          {['GymRecall', 'WriteOff'].includes(kind) && (
            <label>
              Tình trạng nguồn
              <select name="condition">
                <option value="Good">Hàng tốt</option>
                <option value="Quarantine">Cách ly</option>
                <option value="Damaged">Hỏng</option>
              </select>
            </label>
          )}
          <Field
            label="Số lượng (gói)"
            name="quantity"
            type="number"
            min={1}
            max={100000}
            required
          />
          <Field label="Lý do / nội dung phiếu" name="reason" required maxLength={1000} />
          <p className="muted">
            Phiếu chốt ngay khi gửi, không có bản nháp. Không được tiêu hàng đã giữ cho đơn.
            {owner ? ' Điều chỉnh/sửa sai nâng cao sẽ có màn hình riêng.' : ''}
          </p>
          <button className="button" disabled={busy || !warehouse}>
            {busy ? 'Đang chốt…' : 'Chốt phiếu kho'}
          </button>
          {!warehouse && <ErrorBox text="Cần Owner khởi tạo kho MAIN trước." />}
        </form>
      )}
    </Modal>
  );
}
export function GymStock({ gym, close }: { gym: Gym; close: () => void }) {
  const owner = useOwner();
  const r = useResource<Stock>(`/admin/gyms/${gym.id}/stock`);
  const counts = useResource<
    Page<{ id: number; countNumber: string; countedAtUtc: string; kind: string }>
  >(`/admin/gyms/${gym.id}/counts?size=20`);
  const [mode, setMode] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);
  const stock = r.data;
  const ids = [
    ...new Set([
      ...(stock?.bookEstimate.map((x) => x.batchId) ?? []),
      ...(stock?.lastCountLines.map((x) => x.batchId) ?? []),
    ]),
  ];
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!stock) return;
    const f = new FormData(e.currentTarget);
    setBusy(true);
    setError('');
    try {
      await send(`/admin/gyms/${gym.id}/counts`, 'POST', {
        version: stock.version,
        note: f.get('note') || null,
        lines: ids.map((id) => ({
          batchId: id,
          actualGood: Number(f.get(`good-${id}`)),
          actualQuarantine: Number(f.get(`quarantine-${id}`)),
          actualDamaged: Number(f.get(`damaged-${id}`)),
          note: null,
        })),
      });
      setMode(false);
      setMessage('Đã chốt số đếm thực tế. Chênh lệch chưa xác minh không tự tính thành bán.');
      r.reload();
      counts.reload();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <Modal title={gym.name} close={close}>
      {r.loading ? (
        <Loading />
      ) : r.error ? (
        <ErrorBox text={r.error} retry={r.reload} />
      ) : (
        stock && (
          <>
            <div className="info-box">
              <b>Tồn tại lần kiểm đếm gần nhất</b>
              <p>{date(stock.lastCountedAtUtc)} · Không phải tồn thời gian thực</p>
              <small>
                Ước tính sổ = lần đếm + giao/thu hồi sau đó; chưa trừ các lần mua chưa ghi nhận.
                Không dùng cam kết hàng online.
              </small>
            </div>
            {error && <ErrorBox text={error} />} {message && <Success>{message}</Success>}
            <div className="responsive-table">
              <table>
                <thead>
                  <tr>
                    <th>Lô ID</th>
                    <th>Đã đếm tốt / cách ly / hỏng</th>
                    <th>Sổ ước tính tốt / cách ly / hỏng</th>
                  </tr>
                </thead>
                <tbody>
                  {ids.map((id) => {
                    const c = stock.lastCountLines.find((x) => x.batchId === id);
                    const b = stock.bookEstimate.find((x) => x.batchId === id);
                    return (
                      <tr key={id}>
                        <td>#{id}</td>
                        <td>
                          {c
                            ? `${c.actualGood} / ${c.actualQuarantine} / ${c.actualDamaged}`
                            : 'Chưa đếm'}
                        </td>
                        <td>{b ? `${b.good} / ${b.quarantine} / ${b.damaged}` : '0 / 0 / 0'}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            {!ids.length && <Empty title="Chưa có lô được giao tại gym này" />}
            <div className="button-row">
              <button
                className="button"
                disabled={!ids.length || (!stock.lastCountedAtUtc && !owner)}
                onClick={() => setMode(true)}
              >
                <Plus size={16} />
                Bắt đầu kiểm đếm
              </button>
              <button className="icon-button" onClick={r.reload} aria-label="Tải lại">
                <RefreshCw size={17} />
              </button>
            </div>
            {!stock.lastCountedAtUtc && !owner && (
              <p className="warning-text">Lần kiểm đếm mở đầu cần Owner.</p>
            )}
            {mode && (
              <form className="form-stack count-form" onSubmit={submit}>
                <h3>Nhập số thực đếm của mọi lô</h3>
                <p>
                  Không tự điền theo sổ. Số 0 cũng cần nhập rõ; chốt sẽ ghi nhận ngay thời điểm hiện
                  tại.
                </p>
                {ids.map((id) => (
                  <div key={id} className="count-line">
                    <b>Lô #{id}</b>
                    <div className="form-grid three">
                      <Field
                        label="Tốt"
                        name={`good-${id}`}
                        type="number"
                        min={0}
                        max={100000}
                        required
                      />
                      <Field
                        label="Cách ly"
                        name={`quarantine-${id}`}
                        type="number"
                        min={0}
                        max={100000}
                        required
                      />
                      <Field
                        label="Hỏng"
                        name={`damaged-${id}`}
                        type="number"
                        min={0}
                        max={100000}
                        required
                      />
                    </div>
                  </div>
                ))}
                <Field name="note" label="Ghi chú kiểm đếm" maxLength={1000} />
                <div className="button-row">
                  <button disabled={busy} className="button">
                    Chốt kiểm đếm
                  </button>
                  <button
                    type="button"
                    disabled={busy}
                    className="small-button"
                    onClick={() => setMode(false)}
                  >
                    Để sau
                  </button>
                </div>
              </form>
            )}
            <h4>Lịch sử kiểm đếm gần đây</h4>
            {counts.error ? (
              <ErrorBox text={counts.error} />
            ) : (
              counts.data?.items.map((c) => (
                <div className="history-row" key={c.id}>
                  <span>
                    {c.countNumber}
                    <small>
                      {c.kind === 'OpeningBaseline' ? 'Kiểm đếm mở đầu' : 'Kiểm đếm định kỳ'}
                    </small>
                  </span>
                  <b>{date(c.countedAtUtc)}</b>
                </div>
              ))
            )}
            <p className="muted">
              Màn hình xác minh chênh lệch riêng sẽ được bổ sung; số thiếu hiện chưa được ghi là
              doanh số.
            </p>
          </>
        )
      )}
    </Modal>
  );
}
