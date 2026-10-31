import { Link } from 'react-router-dom';
import { BrandArtwork } from './BrandArtwork';

// Display clipped windows into the approved artwork; keep its bitmap unchanged.
// Splitting its symbol/wordmark visually keeps the vertical logo legible in a short navbar.
export function BrandLogo({ workspace = false }: { workspace?: boolean }) {
  return (
    <Link
      className="logo brand-logo"
      to={workspace ? '/quan-tri' : '/'}
      aria-label={workspace ? '4fit — Không gian quản trị' : '4fit — For Fit Chicken, trang chủ'}
    >
      <BrandArtwork part="symbol" className="brand-symbol" />
      <BrandArtwork part="wordmark" className="brand-wordmark" />
    </Link>
  );
}
