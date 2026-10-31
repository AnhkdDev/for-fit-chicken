import { ArrowRight, Check, LoaderCircle, PackageOpen, X } from 'lucide-react';
import { useEffect, useRef, type ReactNode } from 'react';
export function Arrow() {
  return <ArrowRight size={18} aria-hidden="true" />;
}
export function Empty({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="empty">
      <PackageOpen size={38} />
      <h3>{title}</h3>
      {children}
    </div>
  );
}
export function Loading() {
  return (
    <div className="loading" role="status">
      <LoaderCircle className="spin" /> Đang tải dữ liệu…
    </div>
  );
}
export function ErrorBox({ text, retry }: { text: string; retry?: () => void }) {
  return (
    <div className="error" role="alert">
      {text}
      {retry && (
        <button className="text-button" onClick={retry}>
          Thử lại
        </button>
      )}
    </div>
  );
}
export function Modal({
  title,
  children,
  close,
  className = '',
  closeDisabled = false,
}: {
  title: string;
  children: ReactNode;
  close: () => void;
  className?: string;
  closeDisabled?: boolean;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const d = ref.current!;
    const previous = document.activeElement as HTMLElement;
    d.showModal();
    const overflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      d.close();
      document.body.style.overflow = overflow;
      previous?.focus();
    };
  }, []);
  return (
    <dialog
      ref={ref}
      className={`modal ${className}`}
      onCancel={(e) => {
        e.preventDefault();
        if (!closeDisabled) close();
      }}
      onClick={(e) => {
        if (!closeDisabled && e.target === e.currentTarget) close();
      }}
      aria-label={title}
    >
      <div className="modal-head">
        <h2>{title}</h2>
        <button className="icon-button" onClick={close} disabled={closeDisabled} aria-label="Đóng">
          <X />
        </button>
      </div>
      {children}
    </dialog>
  );
}
export function Status({ value }: { value: string }) {
  const labels: Record<string, string> = {
    PendingConfirmation: 'Chờ gọi xác nhận',
    Confirmed: 'Đã xác nhận',
    Preparing: 'Đang chuẩn bị',
    HandedOver: 'Đã bàn giao',
    Completed: 'Giao thành công',
    Cancelled: 'Đã hủy',
    Good: 'Hàng tốt',
    Quarantine: 'Cách ly',
    Damaged: 'Hỏng',
    Sent: 'Đã gửi',
    Failed: 'Gửi lỗi',
    Pending: 'Đang chờ',
  };
  return (
    <span className={`status status-${value}`}>
      <span />
      {labels[value] ?? value}
    </span>
  );
}
export function Success({ children }: { children: ReactNode }) {
  return (
    <div className="success">
      <Check size={18} />
      {children}
    </div>
  );
}
