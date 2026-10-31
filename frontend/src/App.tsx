import { BrowserRouter, Routes, Route, useLocation } from 'react-router-dom';
import { useEffect, lazy, Suspense } from 'react';
import type { PublicData } from './lib/public-data';
import { Seo } from './components/Seo';
import { headMarkup } from './lib/seo';
import { StoreProvider } from './lib/state';
import {
  StoreLayout,
  Home,
  Products,
  ProductDetail,
  Story,
  GymPage,
  Cart,
  Checkout,
  NotFound,
} from './pages/storefront';
const Admin = lazy(() => import('./pages/admin').then((m) => ({ default: m.Admin })));
function ScrollReset() {
  const { pathname } = useLocation();
  useEffect(() => {
    window.scrollTo(0, 0);
    if (pathname === '/quan-tri' || pathname.startsWith('/quan-tri/')) {
      document.head
        .querySelectorAll(
          'title,meta[name="description"],link[rel="canonical"],meta[name="robots"],meta[property^="og:"],script[type="application/ld+json"]',
        )
        .forEach((n) => n.remove());
      document.head.insertAdjacentHTML(
        'beforeend',
        headMarkup(pathname, { products: [], sections: [], gyms: [] }),
      );
    }
  }, [pathname]);
  return null;
}
export function AppRoutes({ initial }: { initial?: PublicData }) {
  const { pathname } = useLocation();
  if (initial?.status === 404 && pathname === initial.path)
    return (
      <StoreProvider initial={initial}>
        <Seo />
        <Routes>
          <Route element={<StoreLayout />}>
            <Route path="*" element={<NotFound />} />
          </Route>
        </Routes>
      </StoreProvider>
    );
  return (
    <>
      <ScrollReset />
      <Routes>
        <Route
          path="/quan-tri/*"
          element={
            <Suspense fallback={<p>Đang tải quản trị…</p>}>
              <Admin />
            </Suspense>
          }
        />
        <Route
          path="*"
          element={
            <StoreProvider initial={initial?.status === 404 ? undefined : initial}>
              <Seo />
              <Routes>
                <Route element={<StoreLayout />}>
                  <Route index element={<Home />} />
                  <Route path="san-pham" element={<Products />} />
                  <Route path="san-pham/:id" element={<ProductDetail />} />
                  <Route path="cau-chuyen" element={<Story />} />
                  <Route path="diem-ban" element={<GymPage />} />
                  <Route path="gio-hang" element={<Cart />} />
                  <Route path="dat-hang" element={<Checkout />} />
                  <Route path="*" element={<NotFound />} />
                </Route>
              </Routes>
            </StoreProvider>
          }
        />
      </Routes>
    </>
  );
}

export default function App({ initial }: { initial?: PublicData }) {
  return (
    <BrowserRouter>
      <AppRoutes initial={initial} />
    </BrowserRouter>
  );
}
