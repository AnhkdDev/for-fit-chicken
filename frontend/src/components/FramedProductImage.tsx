import type { HomeImageFrame } from '../lib/types';
import { framingStyle } from '../lib/image-framing';
import { ProductImage } from './ProductImage';
import './image-framing.css';
export function FramedProductImage({
  src,
  alt,
  frame,
  mode,
}: {
  src: string;
  alt: string;
  frame?: HomeImageFrame;
  mode?: 'desktop' | 'mobile';
}) {
  return (
    <div
      className={`framed-product-image ${mode ? `frame-${mode}` : ''}`}
      style={framingStyle(frame)}
    >
      <ProductImage src={src} alt={alt} imageClassName="framed-photo" />
    </div>
  );
}
