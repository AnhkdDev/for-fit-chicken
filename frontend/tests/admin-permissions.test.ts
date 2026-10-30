import { test } from 'node:test';
import assert from 'node:assert/strict';
import { canOpenAdminArea } from '../src/lib/admin-permissions.ts';
test('operator navigation only allows operational modules including child routes', () => {
  for (const path of ['', 'don-hang', 'don-hang/1', 'bao-cao', 'thong-bao', 'san-pham', 'san-pham/tao-moi', 'san-pham/1', 'kho', 'phong-gym'])
    assert.equal(canOpenAdminArea(false, path), true);
  for (const path of ['trung-bay', 'noi-dung', 'thu-vien', 'nhan-vien', 'san-pham-khac'])
    assert.equal(canOpenAdminArea(false, path), false);
});
test('owner retains all admin navigation', () => {
  for (const path of [
    '',
    'don-hang',
    'trung-bay',
    'noi-dung',
    'thu-vien',
    'nhan-vien',
    'bao-cao',
    'thong-bao',
  ])
    assert.equal(canOpenAdminArea(true, path), true);
});


