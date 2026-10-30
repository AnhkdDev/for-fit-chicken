import { test } from 'node:test';
import assert from 'node:assert/strict';
import { problemMessage } from '../src/lib/problem.ts';
test('business conflicts from backend title are visible with actionable reason', () => {
  for (const code of [
    'PRODUCT_IN_USE',
    'BATCH_IN_USE',
    'PRODUCT_ON_DISPLAY',
    'MEDIA_ORPHAN_TOO_NEW',
    'MEDIA_STILL_REFERENCED',
  ])
    assert.equal(
      problemMessage({ code, title: 'Lý do nghiệp vụ cụ thể' }, 409),
      'Lý do nghiệp vụ cụ thể',
    );
});
test('detail, validation and safe fallbacks remain supported', () => {
  assert.equal(problemMessage({ detail: 'Chi tiết', title: 'Tiêu đề' }, 400), 'Chi tiết');
  assert.equal(problemMessage({ errors: { Name: ['Bắt buộc'] } }, 400), 'Bắt buộc');
  assert.match(problemMessage({}, 403), /quyền/);
  assert.match(problemMessage({ code: 'VERSION_CONFLICT' }, 409), /tải lại/);
});
