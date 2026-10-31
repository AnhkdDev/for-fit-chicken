import { useEffect, useRef, useState, type FormEvent } from 'react';
import { api, ApiError, date, money, send } from '../lib/api';
import type { Order } from '../lib/types';
import {
  orderWorkflow,
  actionLabels,
  actionExplanation,
  type Receipt,
} from '../lib/order-workflow';
import { ErrorBox, Loading, Modal, Status, Success } from '../components/ui';
import { EditRecipient } from './admin-order-forms';
import './order-detail.css';
type Event = { id: number; eventType: string; createdAtUtc: string; note?: string };
type Snapshot = { order: Order; receipts: Receipt[]; events: Event[] };
export function OrderDialog({
  id,
  canManageReceipts,
  close,
}: {
  id: number;
  canManageReceipts: boolean;
  close: () => void;
}) {
  const [snapshot, setSnapshot] = useState<Snapshot>();
  const [loading, setLoading] = useState(true),
    [busy, setBusy] = useState(false),
    [editing, setEditing] = useState(false);
  const [error, setError] = useState(''),
    [loadError, setLoadError] = useState(''),
    [success, setSuccess] = useState('');
  const [action, setAction] = useState(''),
    [reason, setReason] = useState(''),
    [needsRefresh, setNeedsRefresh] = useState(false);
  const inFlight = useRef(false);
  const loadSequence = useRef(0);
  async function load(signal?: AbortSignal) {
    const sequence = ++loadSequence.current;
    setLoading(true);
    const timeout = AbortSignal.timeout(15000);
    const requestSignal = signal ? AbortSignal.any([signal, timeout]) : timeout;
    try {
      const [order, receipts, events] = await Promise.all([
        api<Order>(`/admin/orders/${id}`, { signal: requestSignal }),
        canManageReceipts
          ? api<Receipt[]>(`/admin/orders/${id}/receipts`, { signal: requestSignal })
          : Promise.resolve([]),
        api<Event[]>(`/admin/orders/${id}/events`, { signal: requestSignal }),
      ]);
      if (signal?.aborted || sequence !== loadSequence.current) return false;
      setSnapshot({ order, receipts, events });
      setLoadError('');
      setNeedsRefresh(false);
      return true;
    } catch {
      if (!signal?.aborted && sequence === loadSequence.current) {
        setLoadError(
          'Chưa tải đủ thông tin đơn và thanh toán. Vui lòng tải lại trước khi thao tác.',
        );
        setNeedsRefresh(true);
      }
      return false;
    } finally {
      if (!signal?.aborted && sequence === loadSequence.current) setLoading(false);
    }
  }
  useEffect(() => {
    const c = new AbortController();
    void load(c.signal);
    return () => c.abort();
  }, [id, canManageReceipts]);
  const o = snapshot?.order;
  const ready = !!snapshot && !loading && !loadError;
  const flow = orderWorkflow(o?.status ?? '', snapshot?.receipts, ready, canManageReceipts);
  const otherActions = flow.secondary.filter((cmd) => cmd !== 'complete-paid');
  const locked = busy || loading || needsRefresh || !!loadError || editing;
  const recorded =
    canManageReceipts && ready
      ? snapshot!.receipts.filter((r) => !r.voidedAtUtc).reduce((sum, r) => sum + r.amount, 0)
      : undefined;
  function choose(cmd: string) {
    if (locked || inFlight.current) return;
    setAction(cmd);
    setReason('');
    setError('');
    setSuccess('');
  }
  async function execute(e: FormEvent) {
    e.preventDefault();
    if (!o || locked || inFlight.current || !flow.actions.includes(action)) return;
    inFlight.current = true;
    setBusy(true);
    setError('');
    setSuccess('');
    let committed = false;
    try {
      if (action === 'receipt')
        await send(`/admin/orders/${id}/receipts`, 'POST', {
          version: o.version,
          receivedAtUtc: new Date().toISOString(),
          reference: reason.trim() || null,
        });
      else
        await send(`/admin/orders/${id}/actions/${action}`, 'POST', {
          version: o.version,
          reason: reason.trim() || null,
        });
      committed = true;
      setSuccess(
        action === 'receipt'
          ? `Đã ghi nhận nhận đủ ${money(o.itemsSubtotal)}.`
          : (
              {
                confirm: 'Đã xác nhận đơn và giữ hàng.',
                prepare: 'Đã bắt đầu chuẩn bị đơn.',
                handover: 'Đã bàn giao hàng và xuất kho.',
                complete: 'Đã xác nhận giao thành công. Trạng thái COD được theo dõi riêng.',
                'complete-paid': 'Đã hoàn thành giao hàng và ghi nhận đủ tiền.',
                cancel: 'Đã hủy đơn.',
                reallocate: 'Đã phân bổ lại lô.',
              } as Record<string, string>
            )[action],
      );
      setAction('');
      setReason('');
      await load();
    } catch (e) {
      const conflict = e instanceof ApiError && e.status === 409;
      setError(
        conflict
          ? `${(e as Error).message} Tải lại dữ liệu đơn trước khi tiếp tục.`
          : (e as Error).message,
      );
      if (committed || conflict || !(e instanceof ApiError) || e.status >= 500) {
        setNeedsRefresh(true);
        setAction('');
      }
    } finally {
      inFlight.current = false;
      setBusy(false);
    }
  }
  return (
    <Modal
      title="Chi tiết đơn hàng"
      className="order-workspace"
      closeDisabled={busy}
      close={() => {
        if (!busy) close();
      }}
    >
      {!o && loading ? (
        <Loading />
      ) : !o ? (
        <ErrorBox text={loadError} retry={() => void load()} />
      ) : (
        <>
          <header className="order-overview">
            <div>
              <span className="eyebrow">{o.orderNumber}</span>
              <h3>{o.recipientName}</h3>
            </div>
            <div className="order-state-pair">
              <span>
                Giao hàng <Status value={o.status} />
              </span>
              <span>
                Thanh toán{' '}
                <b>
                  {!canManageReceipts
                    ? 'Không có quyền xem'
                    : !ready
                      ? 'Đang kiểm tra'
                      : o.status === 'Cancelled'
                        ? 'Đơn đã hủy'
                        : flow.paid
                          ? 'Đã nhận đủ tiền'
                          : 'Chưa ghi nhận COD'}
                </b>
              </span>
            </div>
          </header>
          <div className="order-money-summary">
            <div>
              <span>Tiền hàng</span>
              <strong>{money(o.itemsSubtotal)}</strong>
            </div>
            <div>
              <span>Đã ghi nhận</span>
              <strong>{recorded === undefined ? '—' : money(recorded)}</strong>
            </div>
            <div>
              <span>Chưa ghi nhận</span>
              <strong>
                {recorded === undefined
                  ? '—'
                  : o.status === 'Cancelled'
                    ? 'Không cần thu'
                    : money(Math.max(0, o.itemsSubtotal - recorded))}
              </strong>
            </div>
          </div>
          {success && <Success>{success}</Success>}
          {error && <ErrorBox text={error} />}
          {loadError && <ErrorBox text={loadError} />}
          {needsRefresh && (
            <button
              className="small-button"
              disabled={busy || loading}
              onClick={() => {
                setAction('');
                setError('');
                void load();
              }}
            >
              Tải lại dữ liệu đơn
            </button>
          )}
          <div className="order-workspace-grid">
            <div>
              <section className="order-section">
                <h3>Thông tin nhận hàng</h3>
                <a href={`tel:${o.recipientPhone}`}>{o.recipientPhone}</a>
                <p>{o.recipientAddress}</p>
                {o.customerNote && <p>Khách nhắn: {o.customerNote}</p>}
                {o.internalNote && <p>Nội bộ: {o.internalNote}</p>}
                {o.status === 'PendingConfirmation' && (
                  <button
                    className="small-button"
                    disabled={busy || loading || needsRefresh || !!action}
                    onClick={() => setEditing(!editing)}
                  >
                    Sửa thông tin nhận hàng
                  </button>
                )}
                {editing && o.status === 'PendingConfirmation' && (
                  <EditRecipient
                    order={o}
                    saved={() => {
                      setEditing(false);
                      void load();
                    }}
                    cancel={() => setEditing(false)}
                  />
                )}
              </section>
              <section className="order-section">
                <h3>Sản phẩm</h3>
                <div className="line-list">
                  {o.items.map((i) => (
                    <div key={i.id}>
                      <span>
                        {i.productName}
                        <small>
                          {i.label} × {i.quantity}
                        </small>
                      </span>
                      <b>{money(i.lineTotal)}</b>
                    </div>
                  ))}
                </div>
              </section>
            </div>
            <section className="order-next-step" aria-label="Việc cần làm">
              <h3>Việc cần làm</h3>
              {loading ? (
                <p role="status">Đang cập nhật thông tin…</p>
              ) : flow.allDone ? (
                <p className="success">Đã giao thành công và ghi nhận đủ tiền.</p>
              ) : o.status === 'Cancelled' ? (
                <p>Đơn đã hủy. Không có bước giao hàng tiếp theo.</p>
              ) : (
                <p>
                  {o.status === 'HandedOver'
                    ? 'Theo dõi kết quả giao hàng. Tiền về nhóm được ghi nhận riêng.'
                    : o.status === 'Completed'
                      ? 'Đã giao thành công. Kiểm tra tiền COD đã về nhóm chưa.'
                      : 'Chọn bước tiếp theo sau khi đã xác nhận công việc thực tế.'}
                </p>
              )}
              {!action && flow.primary && (
                <button
                  className="button full"
                  disabled={locked}
                  onClick={() => choose(flow.primary!)}
                >
                  {actionLabels[flow.primary]}
                </button>
              )}
              {!action && flow.actions.includes('complete-paid') && (
                <button
                  className="small-button full"
                  disabled={locked}
                  onClick={() => choose('complete-paid')}
                >
                  {actionLabels['complete-paid']}
                </button>
              )}
              {!action && otherActions.length > 0 && (
                <details className="order-other-actions">
                  <summary>Thao tác khác</summary>
                  {otherActions.map((cmd) => (
                    <button
                      key={cmd}
                      className={cmd === 'cancel' ? 'small-button danger' : 'small-button'}
                      disabled={locked}
                      onClick={() => choose(cmd)}
                    >
                      {actionLabels[cmd]}
                    </button>
                  ))}
                </details>
              )}
              {action && (
                <form className="order-action-form" onSubmit={execute}>
                  <h4>
                    {action === 'receipt'
                      ? `Xác nhận đã nhận đủ ${money(o.itemsSubtotal)}`
                      : actionLabels[action]}
                  </h4>
                  <p>{actionExplanation(action)}</p>
                  {action === 'complete-paid' && (
                    <label className="checkbox-label">
                      <input type="checkbox" required disabled={busy} />
                      Tôi xác nhận khách đã nhận hàng và nhóm đã nhận đủ {money(o.itemsSubtotal)}.
                    </label>
                  )}
                  <label>
                    {action === 'cancel'
                      ? 'Lý do hủy — bắt buộc'
                      : action === 'receipt'
                        ? 'Mã đối soát / ghi chú — không bắt buộc'
                        : 'Ghi chú — không bắt buộc'}
                    <textarea
                      rows={2}
                      value={reason}
                      maxLength={action === 'complete-paid' ? 200 : 500}
                      required={action === 'cancel'}
                      disabled={busy}
                      onChange={(e) => setReason(e.target.value)}
                    />
                  </label>
                  <div className="button-row">
                    <button className="button" disabled={locked}>
                      {busy
                        ? 'Đang lưu…'
                        : action === 'receipt'
                          ? `Đã nhận đủ ${money(o.itemsSubtotal)}`
                          : actionLabels[action]}
                    </button>
                    <button
                      type="button"
                      className="small-button"
                      disabled={busy}
                      onClick={() => {
                        setAction('');
                        setReason('');
                      }}
                    >
                      Quay lại
                    </button>
                  </div>
                </form>
              )}
            </section>
          </div>
          {canManageReceipts && (
            <details className="order-history">
              <summary>Lịch sử thu COD{ready ? ` (${snapshot!.receipts.length})` : ''}</summary>
              {!ready ? (
                <p>Chưa có dữ liệu đầy đủ.</p>
              ) : snapshot!.receipts.length ? (
                snapshot!.receipts.map((x) => (
                  <p key={x.id}>
                    {money(x.amount)} · {date(x.receivedAtUtc)} ·{' '}
                    {x.voidedAtUtc ? 'Đã vô hiệu' : 'Hiệu lực'}
                  </p>
                ))
              ) : (
                <p>
                  Chưa ghi nhận khoản thu. Điều này không có nghĩa khách chưa thanh toán cho người
                  giao.
                </p>
              )}
            </details>
          )}
          <details className="order-history">
            <summary>Lịch sử đơn hàng ({snapshot!.events.length})</summary>
            <ol className="timeline">
              {snapshot!.events.map((e) => (
                <li key={e.id}>
                  <span>
                    {(
                      {
                        OrderRequested: 'Đã tiếp nhận đơn',
                        ManualOrderConfirmed: 'Đã chốt đơn thủ công và giữ hàng',
                        RecipientUpdated: 'Đã sửa người nhận',
                        confirm: 'Đã xác nhận đơn',
                        prepare: 'Bắt đầu chuẩn bị',
                        handover: 'Đã bàn giao giao hàng',
                        complete: 'Giao thành công',
                        cancel: 'Đã hủy đơn',
                        reallocate: 'Đã phân bổ lại lô',
                      } as Record<string, string>
                    )[e.eventType] ?? e.eventType}
                  </span>
                  <small>
                    {date(e.createdAtUtc)}
                    {e.note && ` · ${e.note}`}
                  </small>
                </li>
              ))}
            </ol>
          </details>
        </>
      )}
    </Modal>
  );
}
