import { test } from 'node:test';
import assert from 'node:assert/strict';
import { orderedProductImages, selectedProducts } from '../src/lib/home-display.ts';
import type { Product } from '../src/lib/types.ts';
import {
  defaultFrame,
  frameForAsset,
  framingStyle,
  normalizeFrame,
} from '../src/lib/image-framing.ts';

test('framing stays scoped to the selected asset and mobile inherits desktop', () => {
  const frame = { assetId: 11, desktop: { fit: 'cover' as const, x: 25, y: 65, zoom: 1.4 } };
  assert.equal(frameForAsset(frame, 22), undefined);
  assert.equal(frameForAsset(frame, 11), frame);
  const style = framingStyle(frame) as Record<string, unknown>;
  assert.equal(style['--desktop-fit'], 'cover');
  assert.equal(style['--mobile-x'], '25%');
  assert.equal(style['--mobile-zoom'], 1.4);
  const separate = framingStyle({ ...frame, mobile: defaultFrame() }) as Record<string, unknown>;
  assert.equal(separate['--mobile-fit'], 'contain');
  assert.equal(separate['--desktop-fit'], 'cover');
});

test('bad or legacy frame values are safe and do not alter the source', () => {
  const frame = { fit: 'cover' as const, x: -10, y: 150, zoom: Number.NaN };
  assert.deepEqual(normalizeFrame(frame), { fit: 'cover', x: 0, y: 100, zoom: 1 });
  assert.equal(frame.x, -10);
  assert.equal(normalizeFrame({ fit: 'contain', x: 50, y: 50, zoom: 3 }).zoom, 1);
  assert.deepEqual(normalizeFrame(), defaultFrame());
});
const products = [
  { id: 1, isActive: true, isPublished: true },
  { id: 2, isActive: true, isPublished: true },
  { id: 3, isActive: true, isPublished: false },
  { id: 4, isActive: false, isPublished: true },
] as Product[];
test('display preserves configured order, hides unpublished/inactive/deleted products', () => {
  assert.deepEqual(
    selectedProducts(products, [2, 4, 3, 999, 1]).map((p) => p.id),
    [2, 1],
  );
});
test('empty selection never automatically fills other products', () => {
  assert.deepEqual(selectedProducts(products, []), []);
});

test('main image preference is scoped to display, invalid choice falls back, source order unchanged', () => {
  const p = {
    images: [
      { id: 1, assetId: 11 },
      { id: 2, assetId: 22 },
    ],
  } as Product;
  assert.deepEqual(
    orderedProductImages(p, 22).map((x) => x.assetId),
    [22, 11],
  );
  assert.deepEqual(
    orderedProductImages(p, 999).map((x) => x.assetId),
    [11, 22],
  );
  assert.equal(p.images[0].assetId, 11);
});
