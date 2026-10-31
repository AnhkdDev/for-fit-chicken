import { useState, type CSSProperties } from 'react';
export function ProductImage({
  src,
  alt,
  priority = false,
  imageClassName,
  imageStyle,
}: {
  src: string;
  alt: string;
  priority?: boolean;
  imageClassName?: string;
  imageStyle?: CSSProperties;
}) {
  const [state, setState] = useState('loading');
  const [attempt, setAttempt] = useState(0);
  return (
    <span className="product-image-frame">
      <img
        key={attempt}
        src={src}
        className={imageClassName}
        alt={alt}
        loading={priority ? 'eager' : 'lazy'}
        decoding="async"
        fetchPriority={priority ? 'high' : 'auto'}
        onLoad={() => setState('ready')}
        onError={() => setState('error')}
        style={{ ...imageStyle, opacity: state === 'error' ? 0 : 1 }}
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
