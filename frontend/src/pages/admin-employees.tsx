import { useState, type FormEvent } from 'react';
import { useResource } from '../lib/useResource';
import { api, ApiError, send } from '../lib/api';
import type { Page } from '../lib/types';
import { Heading, Panel, Pager, Field } from '../components/admin-ui';
import { Modal, Loading, ErrorBox, Success, Empty } from '../components/ui';
type Employee = {
  id: number;
  userName: string;
  displayName: string;
  isActive: boolean;
  isTemporarilyLocked: boolean;
  version: string;
};
export function Employees() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [active, setActive] = useState('true');
  const r = useResource<Page<Employee>>(
    `/admin/employees?page=${page}&size=20&search=${encodeURIComponent(query)}${active ? '&active=' + active : ''}`,
  );
  const [action, setAction] = useState<{ kind: string; employee?: Employee }>();
  const [message, setMessage] = useState('');
  return (
    <>
      <Heading
        title="Nhân viên"
        description="Quản lý tài khoản Operator. Chỉ Owner có quyền truy cập."
      >
        <button className="button" onClick={() => setAction({ kind: 'create' })}>
          + Tạo nhân viên
        </button>
      </Heading>
      {message && <Success>{message}</Success>}
      <form
        className="admin-toolbar"
        onSubmit={(e) => {
          e.preventDefault();
          setPage(1);
          setQuery(search);
        }}
      >
        <input
          aria-label="Tìm nhân viên"
          placeholder="Tên đăng nhập hoặc tên hiển thị…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          aria-label="Trạng thái nhân viên"
          value={active}
          onChange={(e) => {
            setActive(e.target.value);
            setPage(1);
          }}
        >
          <option value="true">Đang hoạt động</option>
          <option value="false">Đã khóa</option>
          <option value="">Tất cả</option>
        </select>
        <button className="button">Tìm kiếm</button>
        <button type="button" className="small-button" onClick={r.reload}>
          Tải lại
        </button>
      </form>
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
                    <th>Nhân viên</th>
                    <th>Trạng thái</th>
                    <th>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {r.data.items.map((e) => (
                    <tr key={e.id}>
                      <td>
                        <b>{e.displayName}</b>
                        <small>{e.userName} · Operator</small>
                      </td>
                      <td>
                        {!e.isActive
                          ? 'Đã khóa'
                          : e.isTemporarilyLocked
                            ? 'Tạm khóa do đăng nhập sai'
                            : 'Đang hoạt động'}
                      </td>
                      <td>
                        <div className="button-row">
                          <button
                            className="small-button"
                            onClick={() => setAction({ kind: 'edit', employee: e })}
                          >
                            Sửa tên
                          </button>
                          <button
                            className="small-button"
                            onClick={() => setAction({ kind: 'status', employee: e })}
                          >
                            {e.isActive && !e.isTemporarilyLocked ? 'Khóa tài khoản' : 'Mở khóa'}
                          </button>
                          <button
                            className="small-button"
                            onClick={() => setAction({ kind: 'password', employee: e })}
                          >
                            Đặt lại mật khẩu
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pager page={page} total={r.data.total} setPage={setPage} />
          </>
        ) : (
          <Empty title="Không có nhân viên phù hợp" />
        )}
      </Panel>
      {action && (
        <EmployeeForm
          kind={action.kind}
          initial={action.employee}
          close={() => setAction(undefined)}
          saved={() => {
            setAction(undefined);
            setMessage('Đã cập nhật nhân viên.');
            r.reload();
          }}
        />
      )}
    </>
  );
}
function EmployeeForm({
  kind,
  initial,
  close,
  saved,
}: {
  kind: string;
  initial?: Employee;
  close: () => void;
  saved: () => void;
}) {
  const [employee, setEmployee] = useState(initial);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [conflict, setConflict] = useState(false);
  const [revision, setRevision] = useState(0);
  const [show, setShow] = useState(false);
  const [checked, setChecked] = useState(false);
  const unlock = employee && (!employee.isActive || employee.isTemporarilyLocked);
  const title =
    kind === 'create'
      ? 'Tạo tài khoản Operator'
      : kind === 'edit'
        ? 'Sửa tên hiển thị'
        : kind === 'password'
          ? 'Đặt lại mật khẩu'
          : unlock
            ? 'Mở khóa tài khoản'
            : 'Khóa tài khoản';
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = e.currentTarget;
    const f = new FormData(form);
    if ((kind === 'password' || kind === 'create') && f.get('password') !== f.get('confirm')) {
      setError('Hai mật khẩu không khớp.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      if (kind === 'create')
        await send('/admin/employees', 'POST', {
          userName: f.get('userName'),
          displayName: f.get('displayName'),
          password: f.get('password'),
        });
      else if (employee)
        await send(
          `/admin/employees/${employee.id}${kind === 'status' ? '/status' : kind === 'password' ? '/password' : ''}`,
          kind === 'password' ? 'POST' : 'PUT',
          {
            version: employee.version,
            ...(kind === 'edit'
              ? { displayName: f.get('displayName') }
              : kind === 'status'
                ? { isActive: !!unlock }
                : { password: f.get('password') }),
          },
        );
      form.reset();
      saved();
    } catch (e) {
      setError((e as Error).message);
      setConflict(e instanceof ApiError && e.code === 'VERSION_CONFLICT');
    } finally {
      setBusy(false);
    }
  }
  return (
    <Modal title={title} close={() => !busy && close()}>
      {employee && (
        <p>
          <b>{employee.displayName}</b> · {employee.userName}
        </p>
      )}
      {error && <ErrorBox text={error} />}
      <form key={revision} onSubmit={submit}>
        <fieldset disabled={busy || conflict}>
          {kind === 'create' && (
            <Field
              label="Tên đăng nhập *"
              name="userName"
              required
              minLength={3}
              maxLength={100}
              pattern="[A-Za-z0-9][A-Za-z0-9._-]{2,99}"
              autoComplete="off"
            />
          )}
          {(kind === 'create' || kind === 'edit') && (
            <Field
              label="Tên hiển thị *"
              name="displayName"
              required
              maxLength={150}
              value={employee?.displayName}
            />
          )}{' '}
          {(kind === 'create' || kind === 'password') && (
            <>
              <p className="admin-help">
                Mật khẩu 12–128 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt. Không có mật
                khẩu mặc định.
              </p>
              <Field
                label="Mật khẩu mới *"
                name="password"
                type={show ? 'text' : 'password'}
                required
                minLength={12}
                maxLength={128}
                autoComplete="new-password"
              />
              <Field
                label="Nhập lại mật khẩu *"
                name="confirm"
                type={show ? 'text' : 'password'}
                required
                minLength={12}
                maxLength={128}
                autoComplete="new-password"
              />
              <label className="checkbox-label">
                <input type="checkbox" checked={show} onChange={(e) => setShow(e.target.checked)} />
                Hiện mật khẩu
              </label>
            </>
          )}
          {(kind === 'status' || kind === 'password') && (
            <>
              <p>
                Thao tác này thu hồi mọi phiên đăng nhập cũ. Mở khóa không khôi phục phiên cũ; đặt
                lại mật khẩu không tự mở tài khoản đã khóa.
              </p>
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  required
                  checked={checked}
                  onChange={(e) => setChecked(e.target.checked)}
                />
                Tôi xác nhận thay đổi tài khoản này.
              </label>
            </>
          )}
          <div className="button-row">
            <button className="button" disabled={busy}>
              {busy ? 'Đang lưu…' : title}
            </button>
            <button type="button" className="small-button" disabled={busy} onClick={close}>
              Hủy
            </button>
          </div>
        </fieldset>
      </form>
      {conflict && employee && (
        <button
          className="small-button"
          disabled={busy}
          onClick={async () => {
            setBusy(true);
            try {
              setEmployee(await api<Employee>(`/admin/employees/${employee.id}`));
              setRevision((x) => x + 1);
              setConflict(false);
              setError('');
              setChecked(false);
            } catch (e) {
              setError((e as Error).message);
            } finally {
              setBusy(false);
            }
          }}
        >
          Tải lại thông tin (bỏ nội dung đang nhập)
        </button>
      )}
    </Modal>
  );
}
