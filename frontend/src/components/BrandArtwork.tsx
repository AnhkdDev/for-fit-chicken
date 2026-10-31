import { useId } from 'react';

// Clip explicitly: viewBox alone can expose fork tips in the wordmark's letterbox area.
// Multiply on light surfaces removes the white visually without an alpha-filter fringe.
export function BrandArtwork({
  part = 'full',
  className,
}: {
  part?: 'full' | 'symbol' | 'wordmark';
  className?: string;
}) {
  const clipId = `logo-window-${useId().replace(/:/g, '')}`;
  const viewBox =
    part === 'symbol'
      ? '490 300 630 770'
      : part === 'wordmark'
        ? '400 1085 810 385'
        : '400 300 810 1170';
  return (
    <svg
      className={className}
      viewBox={viewBox}
      aria-hidden="true"
      focusable="false"
      style={{ mixBlendMode: 'multiply' }}
    >
      <defs>
        <clipPath id={clipId}>
          {part === 'wordmark' ? (
            <rect x="400" y="1085" width="810" height="385" />
          ) : part === 'symbol' ? (
            <rect x="490" y="300" width="630" height="770" />
          ) : (
            <rect x="400" y="300" width="810" height="1170" />
          )}
        </clipPath>
      </defs>
      <image
        href="/brand/4fit-approved.jpg"
        width="1620"
        height="1620"
        clipPath={`url(#${clipId})`}
      />
    </svg>
  );
}
