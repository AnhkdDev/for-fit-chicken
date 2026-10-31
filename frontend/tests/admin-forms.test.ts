import { test } from 'node:test';
import assert from 'node:assert/strict';
import { productSlug } from '../src/lib/admin-forms.ts';
import { keepLastWordsTogether } from '../src/lib/display-text.ts';

test('heading keeps its final short phrase together without changing stored content', () => {
  const title = 'Ức gà cho người bận rộn';
  assert.equal(keepLastWordsTogether(title), 'Ức gà cho người bận\u00a0rộn');
  assert.equal(title, 'Ức gà cho người bận rộn');
  assert.equal(keepLastWordsTogether('Bận rộn'), 'Bận\u00a0rộn');
});

test('heading handles empty, single-word and long final phrases', () => {
  assert.equal(keepLastWordsTogether(''), '');
  assert.equal(keepLastWordsTogether('ForFit'), 'ForFit');
  const long = 'Tên sản phẩm ABCDEFGHIJKLMNOPQRSTUVWXYZ';
  assert.equal(keepLastWordsTogether(long), long);
});

test('Vietnamese names become valid predictable route slugs', () => {
  assert.equal(productSlug('  Ức Gà · Tiêu Đen 150g  '), 'uc-ga-tieu-den-150g');
  assert.equal(productSlug('ĐẶC BIỆT / Dễ ăn & tiện lợi'), 'dac-biet-de-an-tien-loi');
  assert.equal(productSlug('Ức Gà'.normalize('NFD')), productSlug('Ức Gà'));
});
test('slug punctuation and length stay within API rules', () => {
  assert.equal(productSlug('!!!'), '');
  const long = productSlug('Ức gà '.repeat(200));
  assert.ok(long.length <= 200);
  assert.match(long, /^[a-z0-9]+(-[a-z0-9]+)*$/);
});
