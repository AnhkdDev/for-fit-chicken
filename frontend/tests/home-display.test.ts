import { test } from 'node:test';
import assert from 'node:assert/strict';
import { orderedProductImages, selectedProducts } from '../src/lib/home-display.ts';
import type { Product } from '../src/lib/types.ts';
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
