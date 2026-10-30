import { BrowserRouter, Routes, Route, useLocation } from 'react-router-dom';
import { useEffect } from 'react';
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
import { Admin } from './pages/admin';
function ScrollReset() {
  const { pathname } = useLocation();
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);
  return null;
}
export default function App() {
  return (
    <BrowserRouter>
      <ScrollReset />
      <Routes>
        <Route path="/quan-tri/*" element={<Admin />} />
        <Route
          path="*"
          element={
            <StoreProvider>
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
    </BrowserRouter>
  );
}
