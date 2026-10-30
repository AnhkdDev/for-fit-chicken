import { useId } from 'react';

// Remove the near-white background at render time, without recoloring the brand.
export function BrandArtwork({
  part = 'full',
  className,
}: {
  part?: 'full' | 'symbol' | 'wordmark';
  className?: string;
}) {
  const backgroundFilter = `logo-background-${useId().replace(/:/g, '')}`;
  const viewBox =
    part === 'symbol'
      ? '512 309 596 751'
      : part === 'wordmark'
        ? '420 1105 782 364'
        : '420 309 782 1160';
  return (
    <svg className={className} viewBox={viewBox} aria-hidden="true" focusable="false">
      <defs>
        <filter id={backgroundFilter} colorInterpolationFilters="sRGB">
          <feColorMatrix type="matrix" values="1 0 0 0 0  0 1 0 0 0  0 0 1 0 0  -5 -5 -5 0 14.7" />
        </filter>
      </defs>
      <image
        href="/brand/4fit-orange-original.png"
        width="1620"
        height="1620"
        filter={`url(#${backgroundFilter})`}
      />
    </svg>
  );
}
