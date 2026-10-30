import { useState } from 'react';
export function ProductImage({
  src,
  alt,
  priority = false,
}: {
  src: string;
  alt: string;
  priority?: boolean;
}) {
  const [state, setState] = useState('loading');
  const [attempt, setAttempt] = useState(0);
  return (
    <span className="product-image-frame">
      <img
        key={attempt}
        src={src}
        alt={alt}
        loading={priority ? 'eager' : 'lazy'}
        decoding="async"
        fetchPriority={priority ? 'high' : 'auto'}
        onLoad={() => setState('ready')}
        onError={() => setState('error')}
        style={{ opacity: state === 'ready' ? 1 : 0 }}
      />
      {state === 'loading' && (
        <span className="product-image-status" role="status">
          Đang tải ảnh…
        </span>
      )}
      {state === 'error' && (
        <span className="product-image-status" role="status">
          Chưa tải được ảnh
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              setState('loading');
              setAttempt((x) => x + 1);
            }}
          >
            Thử tải lại
          </button>
        </span>
      )}
    </span>
  );
}
