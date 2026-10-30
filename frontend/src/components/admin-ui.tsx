import type { ReactNode } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';

export function Pager({
  page,
  total,
  setPage,
}: {
  page: number;
  total: number;
  setPage: (p: number) => void;
}) {
  return (
    <div className="pager">
      <span>
        {total} bản ghi · Trang {page}/{Math.max(1, Math.ceil(total / 20))}
      </span>
      <div>
        <button
          className="icon-button"
          disabled={page === 1}
          onClick={() => setPage(page - 1)}
          aria-label="Trang trước"
        >
          <ChevronLeft size={18} />
        </button>
        <button
          className="icon-button"
          disabled={page * 20 >= total}
          onClick={() => setPage(page + 1)}
          aria-label="Trang sau"
        >
          <ChevronRight size={18} />
        </button>
      </div>
    </div>
  );
}
export function Panel({ children }: { children: ReactNode }) {
  return <section className="panel">{children}</section>;
}
export function Heading({
  title,
  description,
  children,
}: {
  title: string;
  description?: string;
  children?: ReactNode;
}) {
  return (
    <div className="admin-heading">
      <div>
        <span className="eyebrow">FORFIT WORKSPACE</span>
        <h1>{title}</h1>
        {description && <p>{description}</p>}
      </div>
      <div className="button-row">{children}</div>
    </div>
  );
}
export function Field({
  label,
  name,
  value,
  ...rest
}: { label: string; name: string; value?: string } & React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <label>
      {label}
      <input name={name} defaultValue={value ?? ''} {...rest} />
    </label>
  );
}
export function validImage(file: File, error: (s: string) => void) {
  if (!['image/png', 'image/jpeg', 'image/webp'].includes(file.type) || file.size > 5242880) {
    error('Chọn ảnh PNG/JPEG/WebP tối đa 5 MiB.');
    return false;
  }
  return true;
}
