import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { useShop } from '../lib/state';
import { headMarkup } from '../lib/seo';
export function Seo() {
  const { pathname } = useLocation();
  const { products, sections, gyms, initialStatus, initialPath } = useShop();
  useEffect(() => {
    document.head
      .querySelectorAll(
        'title,meta[name="description"],link[rel="canonical"],meta[name="robots"],meta[property^="og:"],script[type="application/ld+json"]',
      )
      .forEach((n) => n.remove());
    document.head.insertAdjacentHTML(
      'beforeend',
      headMarkup(pathname, { products, sections, gyms }, pathname === initialPath ? initialStatus : 200),
    );
  }, [pathname, products, sections, gyms, initialStatus, initialPath]);
  return null;
}
