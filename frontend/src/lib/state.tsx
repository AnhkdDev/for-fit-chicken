import type { PublicData } from './public-data';
import {
  createContext,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode,
  type Dispatch,
  type SetStateAction,
} from 'react';
import { api } from './api';
import type { CartItem, Product, Section, Gym } from './types';
import { planCartAdd } from './cart';

const Context = createContext<ReturnType<typeof useStore> | null>(null);
function useStore(initial?: PublicData) {
  const [products, setProducts] = useState<Product[]>(initial?.products ?? []);
  const [sections, setSections] = useState<Section[]>(initial?.sections ?? []);
  const [gyms, setGyms] = useState<Gym[]>(initial?.gyms ?? []);
  const [loading, setLoading] = useState(!initial);
  const [error, setError] = useState('');
  const [cart, updateCart] = useState<CartItem[]>(() => {
    try {
      if (initial || typeof window === 'undefined') return [];
      const v = JSON.parse(localStorage.getItem('forfit.cart.v1') ?? '[]');
      return Array.isArray(v)
        ? v
            .filter(
              (x: CartItem) =>
                Number.isSafeInteger(x.variantId) &&
                Number.isInteger(x.quantity) &&
                x.quantity > 0 &&
                x.quantity <= 100,
            )
            .slice(0, 20)
        : [];
    } catch {
      return [];
    }
  });
  const cartRef = useRef(cart);
  const setCart: Dispatch<SetStateAction<CartItem[]>> = (action) => {
    const next = typeof action === 'function' ? action(cartRef.current) : action;
    cartRef.current = next;
    updateCart(next);
  };
  const [toast, setToast] = useState('');
  const updatePublicProduct = (product: Product) =>
    setProducts((current) =>
      current.some((p) => p.id === product.id)
        ? current.map((p) => (p.id === product.id ? product : p))
        : [...current, product],
    );
  async function reload() {
    setLoading(true);
    setError('');
    try {
      const loadAll = async <T,>(path: string) => {
        const rows: T[] = [];
        let page = 1;
        while (true) {
          const r = await api<{ items: T[]; total: number }>(`${path}?size=100&page=${page}`);
          rows.push(...r.items);
          if (rows.length >= r.total || !r.items.length) return rows;
          page++;
        }
      };
      const [p, s, g] = await Promise.all([
        loadAll<Product>('/products'),
        api<Section[]>('/content'),
        loadAll<Gym>('/gyms'),
      ]);
      setProducts(p);
      setSections(s);
      setGyms(g);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }
  useEffect(() => {
    if (!initial) void reload();
    if (initial) {
      try {
        const saved = JSON.parse(localStorage.getItem('forfit.cart.v1') ?? '[]');
        if (Array.isArray(saved))
          setCart(
            saved
              .filter(
                (x: CartItem) =>
                  Number.isSafeInteger(x.variantId) &&
                  Number.isInteger(x.quantity) &&
                  x.quantity > 0 &&
                  x.quantity <= 100,
              )
              .slice(0, 20),
          );
      } catch {
        /* Keep empty cart. */
      }
    }
  }, []);
  useEffect(() => {
    try {
      localStorage.setItem('forfit.cart.v1', JSON.stringify(cartRef.current));
    } catch {
      /* Cart remains usable in memory if storage is unavailable. */
    }
  }, [cart]);
  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(() => setToast(''), 3500);
    return () => clearTimeout(t);
  }, [toast]);
  function add(variantId: number, quantity = 1, freshVariant?: import('./types').Variant) {
    const v =
      freshVariant?.id === variantId
        ? freshVariant
        : products.flatMap((p) => p.variants).find((v) => v.id === variantId);
    const result = planCartAdd(cartRef.current, v, quantity);
    if (!result.ok) {
      setToast(result.message);
      return false;
    }
    setCart(result.items);
    setToast(`Đã thêm ${quantity} gói vào giỏ hàng`);
    return true;
  }
  return {
    initialStatus: initial?.status,
    initialPath: initial?.path,
    products,
    sections,
    gyms,
    loading,
    error,
    reload,
    cart,
    setCart,
    add,
    toast,
    setToast,
    updatePublicProduct,
  };
}
export function StoreProvider({
  children,
  initial,
}: {
  children: ReactNode;
  initial?: PublicData;
}) {
  const value = useStore(initial);
  return <Context.Provider value={value}>{children}</Context.Provider>;
}
export function useShop() {
  const c = useContext(Context);
  if (!c) throw new Error('Missing StoreProvider');
  return c;
}
