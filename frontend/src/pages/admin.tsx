import { OrderDialog } from './admin-order-detail';
import '../admin.css';
import { canOpenAdminArea } from '../lib/admin-permissions';
import { Employees } from './admin-employees';
import { DeleteRecord } from '../components/DeleteRecord';
import { useEffect, useState, type FormEvent } from 'react';
import { Link, NavLink, Route, Routes, useLocation } from 'react-router-dom';
import {
  ArrowUpRight,
  BarChart3,
  Bell,
  FileText,
  Image,
  LayoutDashboard,
  LogOut,
  MapPin,
  Menu,
  Package,
  Plus,
  RefreshCw,
  Search,
  ShoppingBag,
  Upload,
  X,
} from 'lucide-react';
import { api, clearCsrf, date, money, refreshCsrf, send } from '../lib/api';
import type { Asset, Gym, Order, Page, Product, Profile } from '../lib/types';
import { Empty, ErrorBox, Loading, Modal, Status, Success } from '../components/ui';
import { Inventory, GymStock } from './operations';
import { BrandLogo } from '../components/BrandLogo';
import { useResource } from '../lib/useResource';
import { AdminUserContext as User, useOwner } from '../lib/admin-session';
import { Pager, Panel, Heading, Field, validImage } from '../components/admin-ui';
import { ProductWorkspace } from './admin-product';
import { HomeDisplayAdmin } from './admin-home-display';
import { ContentWorkspace } from './admin-content';
import { ManualOrder } from './admin-order-forms';

const links = [
  ['', 'Tổng quan', LayoutDashboard],
  ['don-hang', 'Đơn hàng', ShoppingBag],
  ['san-pham', 'Sản phẩm', Package],
  ['kho', 'Kho & lô hàng', Package],
  ['phong-gym', 'Phòng gym', MapPin],
  ['trung-bay', 'Trưng bày trang chủ', LayoutDashboard],
  ['noi-dung', 'Nội dung website', FileText],
  ['thu-vien', 'Thư viện ảnh', Image],
  ['nhan-vien', 'Nhân viên', LayoutDashboard],
  ['bao-cao', 'Báo cáo', BarChart3],
  ['thong-bao', 'Thông báo', Bell],
] as const;
export function Admin() {
  const [profile, setProfile] = useState<Profile | null>();
  const [open, setOpen] = useState(false);
  const [error, setError] = useState('');
  const location = useLocation();
  useEffect(() => {
    api<Profile>('/admin/auth/me')
      .then(setProfile)
      .catch((e) => {
        if (e.status !== 401) setError(e.message);
        setProfile(null);
      });
    const expired = () => {
      clearCsrf();
      setProfile(null);
    };
    window.addEventListener('session-expired', expired);
    return () => window.removeEventListener('session-expired', expired);
  }, []);
  useEffect(() => setOpen(false), [location.pathname]);
  async function logout() {
    try {
      await send('/admin/auth/logout', 'POST');
      clearCsrf();
      setProfile(null);
    } catch (e) {
      setError((e as Error).message);
    }
  }
  if (profile === undefined) return <Loading />;
  if (!profile) return <Login initialError={error} onLogin={setProfile} />;
  const owner = profile.roles.includes('Owner');
  return (
    <User.Provider value={profile}>
      <div className="admin-shell">
        <aside className={`sidebar ${open ? 'open' : ''}`}>
          <div className="sidebar-brand">
            <BrandLogo workspace />
            <button
              className="icon-button mobile-menu"
              onClick={() => setOpen(false)}
              aria-label="Đóng menu"
            >
              <X />
            </button>
          </div>
          <span className="sidebar-caption">ĐIỀU HÀNH HẰNG NGÀY</span>
          <nav aria-label="Quản trị">
            {links
              .filter(([key]) => canOpenAdminArea(owner, key))
              .map(([key, label, Icon]) => (
                <NavLink end={key === ''} to={`/quan-tri${key ? '/' + key : ''}`} key={key}>
                  <Icon size={19} />
                  {label}
                </NavLink>
              ))}
          </nav>
          <div className="sidebar-bottom">
            <Link to="/">
              Xem website <ArrowUpRight size={17} />
            </Link>
            <div className="profile">
              <span>{profile.displayName.charAt(0).toUpperCase()}</span>
              <div>
                <b>{profile.displayName}</b>
                <small>{owner ? 'Owner' : 'Operator'}</small>
              </div>
              <button className="icon-button" onClick={logout} aria-label="Đăng xuất tất cả phiên">
                <LogOut size={18} />
              </button>
            </div>
          </div>
        </aside>
        {open && (
          <button
            className="sidebar-backdrop"
            aria-label="Đóng menu"
            onClick={() => setOpen(false)}
          />
        )}
        <div className="admin-workspace">
          <header className="admin-topbar">
            <div>
              <button
                className="icon-button mobile-menu"
                onClick={() => setOpen(true)}
                aria-label="Mở menu quản trị"
              >
                <Menu />
              </button>
              <span>
                Không gian làm việc <span className="muted">/ ForFit Chicken</span>
              </span>
            </div>
            <span className="workspace-tag">
              <span /> {owner ? 'Owner' : 'Operator'}
            </span>
          </header>
          <main className="admin-main">
            {error && <ErrorBox text={error} />}
            <Routes>
              <Route index element={<Dashboard />} />
              <Route path="don-hang" element={<Orders />} />
              <Route path="san-pham" element={<Catalog />} />
              <Route path="san-pham/tao-moi" element={<ProductWorkspace key="new" />} />
              <Route path="san-pham/:id" element={<ProductWorkspace />} />
              <Route path="kho" element={<Inventory />} />
              <Route path="phong-gym" element={<Gyms />} />
              <Route path="trung-bay" element={owner ? <HomeDisplayAdmin /> : <Denied />} />
              <Route path="noi-dung" element={owner ? <ContentWorkspace /> : <Denied />} />
              <Route path="thu-vien" element={owner ? <Media /> : <Denied />} />
              <Route path="nhan-vien" element={owner ? <Employees /> : <Denied />} />
              <Route path="bao-cao" element={<Reports />} />
              <Route path="thong-bao" element={<Notifications />} />
              <Route
                path="*"
                element={
                  <Empty title="Không tìm thấy trang quản trị">
                    <Link to="/quan-tri">Về tổng quan</Link>
                  </Empty>
                }
              />
            </Routes>
          </main>
          <div className="admin-bottom">
            FORFIT CHICKEN <span>Chăm chút từng việc nhỏ.</span>
          </div>
        </div>
      </div>
    </User.Provider>
  );
}
function Denied() {
  return (
    <Empty title="Chức năng dành cho Owner">
      <Link className="button" to="/quan-tri/san-pham">
        Về quản lý sản phẩm
      </Link>
    </Empty>
  );
}
function Login({ initialError, onLogin }: { initialError: string; onLogin: (p: Profile) => void }) {
  const [error, setError] = useState(initialError);
  const [showPassword, setShowPassword] = useState(false);
  const [busy, setBusy] = useState(false);
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setBusy(true);
    setError('');
    const f = new FormData(e.currentTarget);
    try {
      clearCsrf();
      const p = await send<Profile>('/admin/auth/login', 'POST', {
        userName: f.get('userName'),
        password: f.get('password'),
      });
      await refreshCsrf();
      onLogin(p);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <div className="login-page">
      <div className="login-form-wrap">
        <form className="login-form" onSubmit={submit}>
          <fieldset disabled={busy}>
            <div className="login-brand">
              <BrandLogo workspace />
            </div>
            <span className="eyebrow">DÀNH CHO ĐỘI NGŨ FORFIT</span>
            <h1>Đăng nhập quản trị</h1>
            <p>Quản lý sản phẩm, đơn hàng và nội dung website.</p>
            {location.protocol !== 'https:' && (
              <ErrorBox text="Đăng nhập cần HTTPS. Hãy làm theo README để bật chứng chỉ phát triển; HTTP chỉ dùng xem giao diện." />
            )}
            {error && <ErrorBox text={error} />}
            <label>
              Tên đăng nhập
              <input
                name="userName"
                autoComplete="username"
                required
                maxLength={100}
                placeholder="Tên tài khoản nội bộ"
              />
            </label>
            <label>
              Mật khẩu
              <input
                name="password"
                type={showPassword ? 'text' : 'password'}
                autoComplete="current-password"
                required
                maxLength={128}
                placeholder="Nhập mật khẩu của bạn"
              />
            </label>
            <label className="login-show-password">
              <input
                type="checkbox"
                checked={showPassword}
                onChange={(e) => setShowPassword(e.target.checked)}
                disabled={busy}
              />
              Hiện mật khẩu
            </label>
            <button className="button full" disabled={busy || location.protocol !== 'https:'}>
              {busy ? 'Đang đăng nhập…' : 'Đăng nhập'}
              <ArrowUpRight size={18} />
            </button>
            <small>
              Tài khoản do nhóm cấp. Nếu quên mật khẩu, liên hệ Owner; website chưa có tự khôi phục
              mật khẩu.
            </small>
            <Link className="text-link" to="/">
              ← Về website
            </Link>
          </fieldset>
        </form>
      </div>
    </div>
  );
}
function Dashboard() {
  const r = useResource<Page<Order>>('/admin/orders?size=20');
  const pending = useResource<Page<Order>>('/admin/orders?status=PendingConfirmation&size=1');
  return (
    <>
      <Heading
        title="Tổng quan vận hành"
        description="Theo dõi việc cần làm và truy cập nhanh chức năng thường dùng."
      />
      <div className="admin-shortcuts">
        <Link className="button" to="/quan-tri/don-hang">
          Xử lý đơn hàng
        </Link>
        <Link className="small-button" to="/quan-tri/san-pham/tao-moi">
          + Thêm sản phẩm
        </Link>
        <Link className="small-button" to="/quan-tri/noi-dung">
          Sửa nội dung website
        </Link>
      </div>
      <div className="metric-grid">
        <Link className="metric" to="/quan-tri/don-hang?status=PendingConfirmation">
          <span>Đơn chờ gọi xác nhận</span>
          <strong>{pending.data?.total ?? '—'}</strong>
          <small>Chưa giữ hàng tại kho</small>
        </Link>
        <Link className="metric" to="/quan-tri/kho">
          <span>Kho & hạn dùng</span>
          <strong>
            <Package size={33} />
          </strong>
          <small>Kiểm tra lô và tồn khả dụng ↗</small>
        </Link>
        <Link className="metric" to="/quan-tri/phong-gym">
          <span>Điểm bán ký gửi</span>
          <strong>
            <MapPin size={33} />
          </strong>
          <small>Xem thời điểm kiểm đếm gần nhất ↗</small>
        </Link>
      </div>
      <Panel>
        <div className="panel-title">
          <h3>Đơn gần đây</h3>
          <Link to="/quan-tri/don-hang" className="text-link">
            Xem tất cả ↗
          </Link>
        </div>
        {r.loading ? (
          <Loading />
        ) : r.error ? (
          <ErrorBox text={r.error} retry={r.reload} />
        ) : r.data?.items.length ? (
          <div className="responsive-table">
            <table>
              <thead>
                <tr>
                  <th>Đơn hàng</th>
                  <th>Ngày đặt</th>
                  <th>Trạng thái</th>
                  <th>Tiền hàng</th>
                </tr>
              </thead>
              <tbody>
                {r.data.items.slice(0, 6).map((o) => (
                  <tr key={o.id}>
                    <td>
                      <b>{o.orderNumber}</b>
                    </td>
                    <td>{date(o.createdAtUtc)}</td>
                    <td>
                      <Status value={o.status} />
                    </td>
                    <td>{money(o.itemsSubtotal)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <Empty title="Chưa có đơn hàng" />
        )}
      </Panel>
    </>
  );
}
function Orders() {
  const [create, setCreate] = useState(false);

  const [page, setPage] = useState(1);
  const [status, setStatus] = useState(
    new URLSearchParams(window.location.search).get('status') ?? '',
  );
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const r = useResource<Page<Order>>(
    `/admin/orders?page=${page}&size=20&status=${status}&search=${encodeURIComponent(query)}`,
  );
  const [selected, setSelected] = useState<Order>();
  return (
    <>
      <Heading
        title="Đơn hàng"
        description="Đơn website cần gọi xác nhận. Đơn thủ công được chốt và giữ hàng ngay khi tạo."
      >
        <button className="button" onClick={() => setCreate(true)}>
          <Plus size={17} />
          Tạo đơn thủ công
        </button>
      </Heading>
      <nav className="tabs" aria-label="Việc cần xử lý">
        {[
          ['', 'Tất cả'],
          ['PendingConfirmation', 'Chờ gọi'],
          ['Confirmed', 'Chờ chuẩn bị'],
          ['Preparing', 'Đang chuẩn bị'],
          ['HandedOver', 'Đang giao'],
          ['Completed', 'Hoàn tất'],
          ['Cancelled', 'Đã hủy'],
        ].map(([key, label]) => (
          <button
            className={status === key ? 'active' : ''}
            key={key}
            onClick={() => {
              setStatus(key);
              setPage(1);
            }}
          >
            {label}
          </button>
        ))}
      </nav>
      <Panel>
        <div className="admin-toolbar">
          <form
            className="search-field"
            onSubmit={(e) => {
              e.preventDefault();
              setPage(1);
              setQuery(search);
            }}
          >
            <Search size={17} />
            <input
              placeholder="Mã đơn hoặc điện thoại…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              aria-label="Tìm đơn"
            />
            <button className="text-button">Tìm</button>
          </form>
          <select
            aria-label="Lọc trạng thái đơn"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
          >
            <option value="">Tất cả trạng thái</option>
            {[
              'PendingConfirmation',
              'Confirmed',
              'Preparing',
              'HandedOver',
              'Completed',
              'Cancelled',
            ].map((s) => (
              <option key={s} value={s}>
                {orderLabels[s]}
              </option>
            ))}
          </select>
          <button className="icon-button" aria-label="Tải lại" onClick={r.reload}>
            <RefreshCw size={17} />
          </button>
        </div>
        {r.loading ? (
          <Loading />
        ) : r.error ? (
          <ErrorBox text={r.error} retry={r.reload} />
        ) : r.data?.items.length ? (
          <>
            <div className="responsive-table orders-table">
              <table>
                <thead>
                  <tr>
                    <th>Mã đơn / thời gian</th>
                    <th>Khách hàng</th>
                    <th>Trạng thái</th>
                    <th>Tiền hàng</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {r.data.items.map((o) => (
                    <tr key={o.id}>
                      <td>
                        <b>{o.orderNumber}</b>
                        <small>{date(o.createdAtUtc)}</small>
                      </td>
                      <td>
                        {o.recipientName}
                        <small>{o.recipientPhone}</small>
                      </td>
                      <td>
                        <Status value={o.status} />
                      </td>
                      <td>{money(o.itemsSubtotal)}</td>
                      <td>
                        <button className="small-button" onClick={() => setSelected(o)}>
                          Chi tiết ↗
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pager page={page} total={r.data.total} setPage={setPage} />
          </>
        ) : (
          <Empty title="Không có đơn phù hợp" />
        )}
      </Panel>
      {selected && (
        <OrderDialog
          id={selected.id}
          canManageReceipts={true}
          close={() => {
            setSelected(undefined);
            r.reload();
          }}
        />
      )}
      {create && (
        <ManualOrder
          close={() => setCreate(false)}
          saved={(orderNumber) => {
            setCreate(false);
            setPage(1);
            setStatus('');
            setSearch(orderNumber);
            setQuery(orderNumber);
            r.reload();
          }}
        />
      )}
    </>
  );
}
const orderLabels: Record<string, string> = {
  PendingConfirmation: 'Chờ gọi xác nhận',
  Confirmed: 'Đã xác nhận',
  Preparing: 'Đang chuẩn bị',
  HandedOver: 'Đã bàn giao',
  Completed: 'Hoàn tất',
  Cancelled: 'Đã hủy',
};
function Catalog() {
  const owner = useOwner();
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState('active');
  const [page, setPage] = useState(1);
  const r = useResource<Page<Product>>(
    `/admin/products?page=${page}&size=20&search=${encodeURIComponent(query)}&status=${status}`,
  );

  return (
    <>
      <Heading title="Sản phẩm" description="Từ bản nháp đến sản phẩm được công bố trên website.">
        <Link className="button" to="/quan-tri/san-pham/tao-moi">
          <Plus size={17} />
          Tạo sản phẩm
        </Link>
      </Heading>
      <form
        className="admin-toolbar"
        onSubmit={(e) => {
          e.preventDefault();
          setPage(1);
          setQuery(search);
        }}
      >
        <input
          aria-label="Tìm sản phẩm"
          placeholder="Tên sản phẩm, đường dẫn, SKU…"
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
          <option value="active">Đang hoạt động</option>
          <option value="inactive">Ngừng hoạt động</option>
          <option value="">Tất cả</option>
        </select>
        <button className="button">Tìm kiếm</button>
      </form>
      <Panel>
        {r.loading ? (
          <Loading />
        ) : r.error ? (
          <ErrorBox text={r.error} retry={r.reload} />
        ) : r.data?.items.length ? (
          <>
            <div className="responsive-table catalog-table">
              <table>
                <thead>
                  <tr>
                    <th>Sản phẩm</th>
                    <th>SKU</th>
                    <th>Website</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {r.data.items.map((p) => (
                    <tr key={p.id}>
                      <td>
                        <div className="table-product">
                          {p.images[0] ? (
                            <img src={p.images[0].url} alt="" />
                          ) : (
                            <Package size={27} />
                          )}
                          <div>
                            <b>{p.name}</b>
                            <small>{p.slug}</small>
                          </div>
                        </div>
                      </td>
                      <td>{p.variants.length} phân loại</td>
                      <td>
                        <span className={`status ${p.isPublished ? 'status-Completed' : ''}`}>
                          {p.isPublished ? 'Đang công bố' : 'Bản nháp'}
                        </span>
                      </td>
                      <td>
                        <Link className="small-button" to={`/quan-tri/san-pham/${p.id}`}>
                          Chỉnh sửa
                        </Link>
                        {owner && (
                          <DeleteRecord
                            kind="sản phẩm"
                            label={p.name}
                            path={`/admin/products/${p.id}`}
                            version={p.version}
                            deleted={r.reload}
                          />
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pager page={page} total={r.data.total} setPage={setPage} />
          </>
        ) : (
          <Empty title="Sản phẩm đầu tiên của ForFit">
            <p>Tạo bản nháp, thêm SKU và ảnh trước khi công bố.</p>
          </Empty>
        )}
      </Panel>
    </>
  );
}
function Gyms() {
  const [page, setPage] = useState(1);
  const r = useResource<Page<Gym>>(`/admin/gyms?page=${page}&size=20`);
  const [edit, setEdit] = useState<Gym | null | undefined>();
  const [stock, setStock] = useState<Gym>();
  return (
    <>
      <Heading
        title="Phòng gym"
        description="Điểm bán ký gửi. Số đếm gần nhất có thời điểm, không phải tồn trực tiếp."
      >
        <button className="button" onClick={() => setEdit(null)}>
          <Plus size={17} />
          Thêm phòng gym
        </button>
      </Heading>
      {r.error ? (
        <ErrorBox text={r.error} retry={r.reload} />
      ) : r.loading ? (
        <Loading />
      ) : (
        <>
          <div className="admin-gym-grid">
            {r.data?.items.map((g) => (
              <Panel key={g.id}>
                <div className="panel-title">
                  <span className="gym-icon">
                    <MapPin size={23} />
                  </span>
                  <span className="status">{g.gymCode}</span>
                </div>
                <h3>{g.name}</h3>
                <p>{g.publicAddress}</p>
                <div className="button-row">
                  <button className="small-button" onClick={() => setStock(g)}>
                    Tồn & kiểm đếm
                  </button>
                  <button className="text-button" onClick={() => setEdit(g)}>
                    Thông tin ↗
                  </button>
                </div>
              </Panel>
            ))}
          </div>
          {!r.data?.items.length && <Empty title="Chưa có phòng gym" />}
          <Pager page={page} total={r.data?.total ?? 0} setPage={setPage} />
        </>
      )}
      {edit !== undefined && (
        <GymEditor
          g={edit}
          close={() => {
            setEdit(undefined);
            r.reload();
          }}
        />
      )}
      {stock && <GymStock gym={stock} close={() => setStock(undefined)} />}
    </>
  );
}
function GymEditor({ g, close }: { g: Gym | null; close: () => void }) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  async function save(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setBusy(true);
    try {
      const data = {
        ...Object.fromEntries(f),
        coverAssetId: f.get('coverAssetId') ? Number(f.get('coverAssetId')) : null,
        isActive: f.has('isActive'),
        isPublished: f.has('isPublished'),
        version: g?.version,
      };
      await send(`/admin/gyms${g ? '/' + g.id : ''}`, g ? 'PUT' : 'POST', data);
      close();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <Modal title={g ? 'Thông tin phòng gym' : 'Thêm phòng gym'} close={close}>
      <form onSubmit={save} className="form-stack">
        {error && <ErrorBox text={error} />}
        <Field
          label="Mã gym"
          name="gymCode"
          value={g?.gymCode}
          required
          readOnly={!!g}
          pattern="[A-Za-z0-9_-]+"
          maxLength={40}
        />
        <Field label="Tên phòng gym" name="name" value={g?.name} required maxLength={200} />
        <Field
          label="Địa chỉ công khai"
          name="publicAddress"
          value={g?.publicAddress}
          required
          maxLength={600}
        />
        <Field
          label="Điện thoại công khai"
          name="publicPhone"
          value={g?.publicPhone}
          maxLength={30}
        />
        <label>
          Mô tả công khai
          <textarea name="publicDescription" defaultValue={g?.publicDescription} maxLength={1000} />
        </label>
        <Field
          label="ID ảnh bìa (không bắt buộc)"
          name="coverAssetId"
          value={g?.coverAssetId ? String(g.coverAssetId) : ''}
          type="number"
          min={1}
        />
        {[
          ['internalContactName', 'Người liên hệ nội bộ'],
          ['internalPhone', 'Điện thoại nội bộ'],
          ['internalNotes', 'Ghi chú nội bộ'],
        ].map(([key, label]) => (
          <Field
            key={key}
            name={key}
            label={label}
            value={(g as unknown as Record<string, string>)?.[key] ?? ''}
            maxLength={key === 'internalNotes' ? 1000 : key === 'internalPhone' ? 30 : 150}
          />
        ))}
        <div className="checkboxes">
          <label>
            <input
              type="checkbox"
              name="isActive"
              defaultChecked={(g as unknown as { isActive: boolean })?.isActive ?? true}
            />
            Hoạt động
          </label>
          <label>
            <input
              type="checkbox"
              name="isPublished"
              defaultChecked={(g as unknown as { isPublished: boolean })?.isPublished}
            />
            Công khai điểm bán
          </label>
        </div>
        <button className="button" disabled={busy}>
          Lưu phòng gym
        </button>
      </form>
    </Modal>
  );
}
const contentLabels: Record<string, string> = {
  HomeHero: 'Banner trang chủ',
  Story: 'Câu chuyện',
  Usage: 'Hướng dẫn sử dụng',
  Faq: 'Câu hỏi thường gặp',
  Contact: 'Liên hệ',
  SiteSettings: 'Thông tin website',
};
function Media() {
  const owner = useOwner();
  const [revision, setRevision] = useState(0);
  const [last, setLast] = useState<Asset>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  async function upload(file: File) {
    if (!validImage(file, setError)) return;
    setBusy(true);
    setError('');
    try {
      const f = new FormData();
      f.append('file', file);
      setLast(await api<Asset>('/media', { method: 'POST', body: f }));
      setRevision((x) => x + 1);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <Heading
        title="Thư viện ảnh"
        description="Ảnh được tối ưu tự động. Dung lượng toàn hệ thống giới hạn 9 GB, không reset."
      />
      {error && <ErrorBox text={error} />}
      <label className="upload-zone">
        <Upload size={28} />
        <b>{busy ? 'Đang tối ưu và tải ảnh…' : 'Thêm một hình ảnh mới'}</b>
        <span>PNG, JPEG, WebP · Tối đa 5 MiB / ảnh · Tự chuyển WebP</span>
        <input
          type="file"
          accept="image/png,image/jpeg,image/webp"
          disabled={busy}
          onChange={(e) => {
            const f = e.target.files?.[0];
            if (f) void upload(f);
            e.target.value = '';
          }}
        />
      </label>
      {last && (
        <Success>
          Đã upload ảnh #{last.id} · {(last.byteSize / 1024).toFixed(1)} KB. Dùng ID này để gắn vào
          nội dung.
          <a href={last.url} target="_blank" rel="noreferrer">
            Xem ảnh ↗
          </a>
        </Success>
      )}
      {owner ? (
        <OwnerMedia revision={revision} />
      ) : (
        <Panel>
          <h3>Upload dành cho Operator</h3>
          <p>
            Bạn có thể upload và dùng ID ảnh trong nội dung hoặc sản phẩm. Danh sách toàn bộ ảnh và
            vận hành dung lượng do Owner quản lý.
          </p>
        </Panel>
      )}
    </>
  );
}
function OwnerMedia({ revision }: { revision: number }) {
  const [page, setPage] = useState(1);
  const [unused, setUnused] = useState(false);
  const r = useResource<Page<Asset>>(
    `/admin/media/assets?page=${page}&size=20&unusedOnly=${unused}&view=${revision}`,
  );
  const st = useResource<{
    chargedBytes: number;
    limitBytes: number;
    uncertainBytes: number;
    uploadsPaused: boolean;
    lastReconciledAtUtc: string;
    warnings: string[];
    nodeReady: boolean;
    classAThisMonth: number;
    classBThisMonth: number;
  }>(`/admin/media/storage?view=${revision}`);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [del, setDel] = useState<Asset>();
  async function action(kind: string) {
    setBusy(true);
    setError('');
    try {
      if (kind === 'delete' && del) {
        const result = await send<{ cleanupRequired: boolean }>(
          `/admin/media/assets/${del.id}`,
          'DELETE',
        );
        setMessage(
          result.cleanupRequired
            ? 'Đã gỡ metadata, nhưng chưa xác nhận xóa ảnh vật lý. Quota vẫn được giữ; cần xử lý orphan.'
            : 'Đã xóa ảnh không dùng.',
        );
        setDel(undefined);
      } else if (kind === 'reconcile') await send('/admin/media/reconcile', 'POST');
      else await send('/admin/media/uploads', 'PUT', { paused: !st.data?.uploadsPaused });
      r.reload();
      st.reload();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      {st.data && (
        <Panel>
          <div className="panel-title">
            <h3>Dung lượng lưu trữ</h3>
            <span className="status">
              {st.data.uploadsPaused
                ? 'Đã tạm dừng upload'
                : st.data.nodeReady
                  ? 'Sẵn sàng'
                  : 'Chờ đối soát'}
            </span>
          </div>
          <div className="quota-number">
            {(st.data.chargedBytes / 1e9).toFixed(3)}{' '}
            <span>/ {(st.data.limitBytes / 1e9).toFixed(0)} GB</span>
          </div>
          <progress max={st.data.limitBytes} value={st.data.chargedBytes} />
          <p className="muted">
            Đối soát: {date(st.data.lastReconciledAtUtc)} · Chưa rõ kết quả:{' '}
            {st.data.uncertainBytes} byte · A/B tháng: {st.data.classAThisMonth}/
            {st.data.classBThisMonth}
          </p>
          {st.data.warnings?.map((w) => (
            <p className="warning-text" key={w}>
              {w}
            </p>
          ))}
          <div className="button-row">
            <button className="small-button" disabled={busy} onClick={() => action('pause')}>
              {st.data.uploadsPaused ? 'Mở lại upload' : 'Tạm dừng upload mới'}
            </button>
            <button className="small-button" disabled={busy} onClick={() => action('reconcile')}>
              Đối soát dung lượng
            </button>
          </div>
        </Panel>
      )}
      {st.error && <ErrorBox text={st.error} retry={st.reload} />}{' '}
      {error && <ErrorBox text={error} />} {message && <Success>{message}</Success>}
      <Panel>
        <div className="panel-title">
          <h3>Ảnh đã tải lên</h3>
          <button className="small-button" disabled={busy} onClick={r.reload}>
            Tải lại thư viện
          </button>
          <label className="inline-check">
            <input
              type="checkbox"
              checked={unused}
              onChange={(e) => {
                setUnused(e.target.checked);
                setPage(1);
              }}
            />
            Chỉ ảnh không dùng
          </label>
        </div>
        {r.loading ? (
          <Loading />
        ) : r.error ? (
          <ErrorBox text={r.error} retry={r.reload} />
        ) : (
          <>
            <div className="asset-grid">
              {r.data?.items.map((a) => (
                <figure key={a.id}>
                  <a href={a.url} target="_blank" rel="noreferrer">
                    <img src={a.url} alt={`Ảnh thư viện ${a.id}`} loading="lazy" />
                  </a>
                  <figcaption>
                    <b>#{a.id}</b>
                    <span>{(a.byteSize / 1024).toFixed(1)} KB</span>
                  </figcaption>
                  {unused &&
                    a.createdAtUtc &&
                    Date.parse(a.createdAtUtc) + 86400000 > Date.now() && (
                      <p className="admin-help">
                        Chờ tối thiểu đến{' '}
                        {date(new Date(Date.parse(a.createdAtUtc) + 86400000).toISOString())} trước
                        khi xóa.
                      </p>
                    )}
                  {unused && (
                    <button
                      disabled={
                        busy ||
                        !!(a.createdAtUtc && Date.parse(a.createdAtUtc) + 86400000 > Date.now())
                      }
                      className="text-button danger"
                      onClick={() => {
                        setError('');
                        setDel(a);
                      }}
                    >
                      Xóa ảnh không dùng
                    </button>
                  )}
                </figure>
              ))}
            </div>
            {!r.data?.items.length && <Empty title="Chưa có ảnh phù hợp" />}
            <Pager page={page} total={r.data?.total ?? 0} setPage={setPage} />
          </>
        )}
      </Panel>
      {del && (
        <Modal title={`Xóa ảnh #${del.id}?`} close={() => !busy && setDel(undefined)}>
          {error && <ErrorBox text={error} />}
          <p>
            Chỉ xóa khi ảnh không còn được tham chiếu và đã qua thời gian chờ tối thiểu 24 giờ. Đây
            là thao tác xóa ảnh vật lý, không thể hoàn tác trong ứng dụng.
          </p>
          <button className="button danger-button" disabled={busy} onClick={() => action('delete')}>
            Xác nhận xóa ảnh
          </button>
        </Modal>
      )}
    </>
  );
}
function Reports() {
  const [range, setRange] = useState({
    from: new Date(Date.now() - 29 * 86400000).toISOString().slice(0, 10),
    to: new Date().toISOString().slice(0, 10),
  });
  const [query, setQuery] = useState(range);
  const from = new Date(`${query.from}T00:00:00+07:00`).toISOString();
  const to = new Date(new Date(`${query.to}T00:00:00+07:00`).getTime() + 86400000).toISOString();
  const r = useResource<{
    ordersRequested: number;
    ordersCompleted: number;
    completedItemsValue: number;
    codReceived: number;
    knownIssuedCost: number;
    issuedPackagesWithUnknownCost: number;
    note: string;
  }>(`/admin/reports/sales?fromUtc=${encodeURIComponent(from)}&toUtc=${encodeURIComponent(to)}`);
  return (
    <>
      <Heading
        title="Báo cáo bán hàng"
        description="Theo dõi doanh số, tiền đã nhận và giá vốn theo từng mốc thời gian."
      />
      <Panel>
        <form
          className="admin-toolbar"
          onSubmit={(e) => {
            e.preventDefault();
            setQuery({ ...range });
            r.reload();
          }}
        >
          <label>
            Từ ngày
            <input
              type="date"
              required
              value={range.from}
              max={range.to}
              onChange={(e) => setRange({ ...range, from: e.target.value })}
            />
          </label>
          <label>
            Đến hết ngày
            <input
              type="date"
              required
              value={range.to}
              min={range.from}
              onChange={(e) => setRange({ ...range, to: e.target.value })}
            />
          </label>
          <button className="button">Xem báo cáo</button>
        </form>
      </Panel>
      {r.loading ? (
        <Loading />
      ) : r.error ? (
        <ErrorBox text={r.error} retry={r.reload} />
      ) : (
        r.data && (
          <>
            <div className="metric-grid">
              {[
                ['Đơn được yêu cầu', r.data.ordersRequested],
                ['Đơn hoàn tất', r.data.ordersCompleted],
                ['Giá trị đơn hoàn tất', money(r.data.completedItemsValue)],
                ['COD thực nhận', money(r.data.codReceived)],
                ['Giá vốn xuất đã biết', money(r.data.knownIssuedCost)],
                ['Gói chưa biết giá vốn', r.data.issuedPackagesWithUnknownCost],
              ].map(([label, value]) => (
                <div className="metric" key={label}>
                  <span>{label}</span>
                  <strong className="money-metric">{value}</strong>
                </div>
              ))}
            </div>
            <div className="info-box">
              <h3>Đọc đúng các con số</h3>
              <p>{r.data.note}</p>
              <p>
                Ngày lọc theo múi giờ Việt Nam, bao gồm trọn ngày kết thúc. Các chỉ số này không
                phải lợi nhuận ròng.
              </p>
            </div>
          </>
        )
      )}
    </>
  );
}
function Notifications() {
  const [page, setPage] = useState(1);
  const r = useResource<
    Page<{
      id: number;
      orderId: number;
      status: string;
      attemptCount: number;
      createdAtUtc: string;
      lastError?: string;
    }>
  >(`/admin/notifications?page=${page}&size=20`);
  const [retry, setRetry] = useState<number>();
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  return (
    <>
      <Heading
        title="Thông báo Telegram"
        description="Theo dõi quá trình gửi. Trạng thái đã gửi không có nghĩa người trong nhóm đã đọc."
      />
      {error && <ErrorBox text={error} />}
      <Panel>
        {r.loading ? (
          <Loading />
        ) : r.error ? (
          <ErrorBox text={r.error} retry={r.reload} />
        ) : (
          <>
            <div className="responsive-table">
              <table>
                <thead>
                  <tr>
                    <th>Thông báo</th>
                    <th>Trạng thái</th>
                    <th>Số lần thử</th>
                    <th>Tạo lúc</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {r.data?.items.map((j) => (
                    <tr key={j.id}>
                      <td>
                        #{j.id} · Đơn #{j.orderId}
                        <small>{j.lastError}</small>
                      </td>
                      <td>
                        <Status value={j.status} />
                      </td>
                      <td>{j.attemptCount}</td>
                      <td>{date(j.createdAtUtc)}</td>
                      <td>
                        {j.status === 'Failed' && (
                          <button className="small-button" onClick={() => setRetry(j.id)}>
                            Gửi lại
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {!r.data?.items.length && <Empty title="Chưa có thông báo" />}
            <Pager page={page} total={r.data?.total ?? 0} setPage={setPage} />
          </>
        )}
      </Panel>
      {retry && (
        <Modal title="Gửi lại thông báo?" close={() => !busy && setRetry(undefined)}>
          <p>
            Hãy kiểm tra nhóm Telegram trước vì thông báo có thể bị gửi trùng khi lần gửi trước chưa
            rõ kết quả.
          </p>
          <button
            className="button"
            disabled={busy}
            onClick={async () => {
              setBusy(true);
              try {
                await send(`/admin/notifications/${retry}/retry`, 'POST');
                setRetry(undefined);
                r.reload();
              } catch (e) {
                setError((e as Error).message);
              } finally {
                setBusy(false);
              }
            }}
          >
            Xác nhận gửi lại
          </button>
        </Modal>
      )}
    </>
  );
}
