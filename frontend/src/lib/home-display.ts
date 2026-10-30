import type { Product } from './types';
export function selectedProducts(products: Product[], ids: number[]) {
  return ids
    .map((id) => products.find((p) => p.id === id && p.isActive && p.isPublished))
    .filter((p): p is Product => !!p);
}

export function orderedProductImages(product: Product, mainAssetId?: number) {
  return [...product.images].sort(
    (a, b) => Number(b.assetId === mainAssetId) - Number(a.assetId === mainAssetId),
  );
}
