import { useEffect, useRef, useState, type FormEvent } from 'react';
import { api, money, send, ApiError } from '../lib/api';
import type { Order, Page, Product } from '../lib/types';
import { Field } from '../components/admin-ui';
import { ErrorBox, Loading, Modal, Success } from '../components/ui';

export function EditRecipient({
  order,
  saved,
  cancel,
}: {
  order: Order;
  saved: () => void;
  cancel: () => void;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setBusy(true);
    setError('');
    try {
      await send(`/admin/orders/${order.id}`, 'PUT', {
        version: order.version,
        recipientName: f.get('name'),
        recipientPhone: f.get('phone'),
        recipientAddress: f.get('address'),
        internalNote: f.get('note') || null,
      });
      saved();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <form className="form-stack admin-inset" onSubmit={submit}>
      <h3>Sửa thông tin trước xác nhận</h3>
      {error && <ErrorBox text={error} />}
      <RecipientFields
        name={order.recipientName}
        phone={order.recipientPhone}
        address={order.recipientAddress}
      />
      <label>
        Ghi chú nội bộ
        <textarea name="note" maxLength={1000} defaultValue={order.internalNote ?? ''} />
      </label>
      <div className="button-row">
        <button className="button" disabled={busy}>
          Lưu thông tin khách
        </button>
        <button className="small-button" type="button" disabled={busy} onClick={cancel}>
          Đóng form
        </button>
      </div>
    </form>
  );
}
function RecipientFields({
  name = '',
  phone = '',
  address = '',
}: {
  name?: string;
  phone?: string;
  address?: string;
}) {
  return (
    <>
      <div className="form-grid">
        <Field
          name="name"
          label="Tên người nhận *"
          value={name}
          required
          maxLength={150}
          autoComplete="off"
        />
        <Field
          name="phone"
          label="Số điện thoại *"
          value={phone}
          type="tel"
          required
          pattern="(0[0-9]{9}|\+84[0-9]{9})"
          autoComplete="off"
        />
      </div>
      <label>
        Địa chỉ giao tại Hà Nội *
        <textarea name="address" defaultValue={address} required maxLength={600} rows={2} />
      </label>
    </>
  );
}
export function ManualOrder({ close, saved }: { close: () => void; saved: () => void }) {
  const [products, setProducts] = useState<Product[]>();
  const [error, setError] = useState('');
  const [query, setQuery] = useState('');
  const [lines, setLines] = useState<{ variantId: number; quantity: number }[]>([]);
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState('');
  const [uncertain, setUncertain] = useState(false);
  const attempt = useRef<{ payload: string; key: string } | undefined>(undefined);
  const form = useRef<HTMLFormElement>(null);
  useEffect(() => {
    let active = true;
    (async () => {
      const all: Product[] = [];
      for (let page = 1; ; page++) {
        const r = await api<Page<Product>>(`/admin/products?page=${page}&size=100`);
        all.push(...r.items);
        if (!r.items.length || all.length >= r.total) break;
      }
      if (active) setProducts(all);
    })().catch((e) => active && setError(e.message));
    return () => {
      active = false;
    };
  }, []);
  const variants =
    products
      ?.filter((p) => p.isActive)
      .flatMap((p) =>
        p.variants.filter((v) => v.isActive).map((v) => ({ ...v, productName: p.name })),
      ) ?? [];
  const matches = variants.filter((v) =>
    `${v.productName} ${v.label} ${v.skuCode}`
      .toLocaleLowerCase('vi')
      .includes(query.toLocaleLowerCase('vi')),
  );
  function requestClose() {
    if (busy) return;
    if (result) {
      saved();
      return;
    }
    if (
      (lines.length || form.current?.querySelector<HTMLInputElement>('[name=name]')?.value) &&
      !result &&
      !confirm('Đơn chưa được tạo. Đóng và bỏ nội dung đang nhập?')
    )
      return;
    close();
  }
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!lines.length) return;
    const f = new FormData(e.currentTarget);
    const data = {
      source: f.get('source'),
      order: {
        recipientName: f.get('name'),
        recipientPhone: f.get('phone'),
        recipientAddress: f.get('address'),
        customerNote: f.get('note') || null,
        items: lines,
      },
    };
    const payload = JSON.stringify(data);
    if (attempt.current && attempt.current.payload !== payload && uncertain) {
      setError(
        'Lần gửi trước chưa rõ kết quả. Hãy kiểm tra danh sách đơn theo số điện thoại trước khi đổi nội dung và tạo đơn mới.',
      );
      return;
    }
    if (attempt.current?.payload !== payload)
      attempt.current = { payload, key: crypto.randomUUID() };
    setBusy(true);
    setError('');
    try {
      const r = await send<{ orderNumber: string }>('/admin/orders', 'POST', data, {
        'Idempotency-Key': attempt.current!.key,
      });
      setResult(r.orderNumber);
      setUncertain(false);
    } catch (e) {
      setUncertain(!(e instanceof ApiError) || e.status >= 500);
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <Modal title="Tạo đơn thủ công · COD" close={requestClose}>
      {result ? (
        <>
          <Success>Đã tạo đơn {result}. Đơn đang chờ gọi xác nhận, chưa giữ hàng.</Success>
          <button className="button" onClick={saved}>
            Xong · về danh sách
          </button>
        </>
      ) : (
        <form ref={form} className="form-stack" onSubmit={submit}>
          {error && <ErrorBox text={error} />}
          <fieldset disabled={busy}>
            <label>
              Nguồn đơn
              <select name="source" defaultValue="Phone">
                <option value="Phone">Điện thoại</option>
                <option value="Facebook">Facebook</option>
                <option value="Zalo">Zalo</option>
                <option value="Other">Khác</option>
              </select>
            </label>
            <h3>1. Thông tin khách</h3>
            <RecipientFields />
            <label>
              Khách ghi chú
              <textarea name="note" maxLength={1000} rows={2} />
            </label>
            <h3>2. Chọn sản phẩm</h3>
            {!products && !error ? (
              <Loading />
            ) : (
              <>
                <input
                  aria-label="Tìm sản phẩm cho đơn"
                  placeholder="Tìm theo tên, quy cách hoặc mã SKU…"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                />
                <div className="admin-sku-options">
                  {matches.slice(0, 30).map((v) => (
                    <button
                      type="button"
                      className="admin-sku-option"
                      key={v.id}
                      disabled={lines.some((l) => l.variantId === v.id) || lines.length >= 20}
                      onClick={() => setLines([...lines, { variantId: v.id, quantity: 1 }])}
                    >
                      <span>
                        {v.productName}
                        <small>
                          {v.label} · {v.skuCode} · Khả dụng: {v.available}
                        </small>
                      </span>
                      <b>{money(v.sellingPrice)}</b>
                      <span>+ Thêm</span>
                    </button>
                  ))}
                </div>
                {matches.length > 30 && (
                  <p className="admin-help">Có nhiều kết quả; nhập tên cụ thể hơn để thu hẹp.</p>
                )}
                {!matches.length && <p>Không tìm thấy quy cách đang bán.</p>}
              </>
            )}
            <div className="line-list">
              {lines.map((l) => {
                const v = variants.find((x) => x.id === l.variantId);
                return (
                  <div className="admin-order-line" key={l.variantId}>
                    <span>
                      {v?.productName}
                      <small>{v?.label}</small>
                    </span>
                    <label>
                      Số lượng
                      <input
                        type="number"
                        min={1}
                        max={100}
                        required
                        value={l.quantity}
                        onChange={(e) =>
                          setLines(
                            lines.map((x) =>
                              x.variantId === l.variantId
                                ? { ...x, quantity: Number(e.target.value) }
                                : x,
                            ),
                          )
                        }
                      />
                    </label>
                    <b>{money((v?.sellingPrice ?? 0) * l.quantity)}</b>
                    <button
                      type="button"
                      className="small-button"
                      onClick={() => setLines(lines.filter((x) => x.variantId !== l.variantId))}
                    >
                      Bỏ
                    </button>
                  </div>
                );
              })}
            </div>
            <p>
              <b>
                Tiền hàng dự kiến:{' '}
                {money(
                  lines.reduce(
                    (sum, l) =>
                      sum +
                      (variants.find((v) => v.id === l.variantId)?.sellingPrice ?? 0) * l.quantity,
                    0,
                  ),
                )}
              </b>
            </p>
            <p className="admin-help">
              Backend tính lại giá khi tạo. Đơn chờ xác nhận chưa giữ tồn; chỉ xác nhận sau khi đã
              liên hệ khách.
            </p>
          </fieldset>
          <div className="button-row">
            <button className="button" disabled={busy || !lines.length || !products}>
              {busy ? 'Đang tạo…' : 'Tạo đơn chờ xác nhận'}
            </button>
            <button type="button" className="small-button" disabled={busy} onClick={requestClose}>
              Đóng
            </button>
          </div>
        </form>
      )}
    </Modal>
  );
}
