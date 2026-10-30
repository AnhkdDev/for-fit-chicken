import test from 'node:test';
import assert from 'node:assert/strict';
import { planCartAdd } from '../src/lib/cart.ts';
import type { Variant } from '../src/lib/types.ts';
const variant: Variant = {
  id: 1,
  skuCode: 'UI-1',
  label: 'Test',
  netWeightGrams: 150,
  sellingPrice: 35000,
  isActive: true,
  available: 8,
};
test('quick buy merges selected quantity without modifying the existing cart', () => {
  const cart = [{ variantId: 1, quantity: 2 }];
  const r = planCartAdd(cart, variant, 3);
  assert.deepEqual(r, { ok: true, items: [{ variantId: 1, quantity: 5 }] });
  assert.equal(cart[0].quantity, 2);
});
test('stock limit includes packages already in cart', () => {
  assert.equal(planCartAdd([{ variantId: 1, quantity: 7 }], variant, 2).ok, false);
  assert.equal(planCartAdd([{ variantId: 1, quantity: 7 }], variant, 1).ok, true);
});
test('100 package maximum applies even when warehouse has more', () => {
  assert.equal(
    planCartAdd([{ variantId: 1, quantity: 99 }], { ...variant, available: 500 }, 2).ok,
    false,
  );
});
test('twentieth distinct SKU is allowed, twenty-first is rejected; existing SKU still merges', () => {
  const cart = Array.from({ length: 19 }, (_, i) => ({ variantId: i + 10, quantity: 1 }));
  assert.equal(planCartAdd(cart, variant, 1).ok, true);
  const full = [...cart, { variantId: 1, quantity: 1 }];
  assert.equal(planCartAdd(full, { ...variant, id: 2 }, 1).ok, false);
  assert.equal(planCartAdd(full, variant, 1).ok, true);
});
test('out of stock, inactive, missing SKU and invalid quantities cannot be added', () => {
  for (const q of [0, -1, 1.5, NaN, Infinity]) assert.equal(planCartAdd([], variant, q).ok, false);
  assert.equal(planCartAdd([], { ...variant, available: 0 }, 1).ok, false);
  assert.equal(planCartAdd([], { ...variant, isActive: false }, 1).ok, false);
  assert.equal(planCartAdd([], undefined, 1).ok, false);
});
