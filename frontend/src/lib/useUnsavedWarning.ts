import { useEffect } from 'react';
// Covers refresh/tab close and normal internal links without storing drafts/PII on disk.
export function useUnsavedWarning(dirty: boolean) {
  useEffect(() => {
    if (!dirty) return;
    const unload = (e: BeforeUnloadEvent) => {
      e.preventDefault();
      e.returnValue = '';
    };
    const link = (e: MouseEvent) => {
      const a = (e.target as Element)?.closest('a');
      if (!a || a.target === '_blank' || e.ctrlKey || e.metaKey || e.shiftKey || e.button !== 0)
        return;
      const href = a.getAttribute('href');
      if (!href || href.startsWith('#') || href.startsWith('tel:')) return;
      if (!window.confirm('Bạn có thay đổi chưa lưu. Rời trang và bỏ các thay đổi này?')) {
        e.preventDefault();
        e.stopImmediatePropagation();
      }
    };
    window.addEventListener('beforeunload', unload);
    document.addEventListener('click', link, true);
    return () => {
      window.removeEventListener('beforeunload', unload);
      document.removeEventListener('click', link, true);
    };
  }, [dirty]);
}
