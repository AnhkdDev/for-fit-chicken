import { test } from 'node:test';
import assert from 'node:assert/strict';
import { orderWorkflow } from '../src/lib/order-workflow.ts';
const receipt = { id: 1, amount: 42000, receivedAtUtc: '2026-10-30T00:00:00Z' };
test('delivery and COD are independent; complete delivery does not imply paid', () => {
  assert.equal(orderWorkflow('HandedOver', [], true).primary, 'complete');
  const completed = orderWorkflow('Completed', [], true);
  assert.equal(completed.primary, 'receipt');
  assert.equal(completed.allDone, false);
  const paid = orderWorkflow('Completed', [receipt], true);
  assert.equal(paid.allDone, true);
  assert.deepEqual(paid.actions, []);
});
test('never offer receipt while unknown, before handover, or already paid', () => {
  for (const state of ['PendingConfirmation', 'Confirmed', 'Preparing', 'Cancelled'])
    assert.equal(orderWorkflow(state, [], true).actions.includes('receipt'), false);
  for (const state of ['HandedOver', 'Completed']) {
    assert.equal(orderWorkflow(state, undefined, false).actions.includes('receipt'), false);
    assert.equal(orderWorkflow(state, [receipt], true).actions.includes('receipt'), false);
    assert.equal(orderWorkflow(state, [], true, false).actions.includes('receipt'), false);
  }
});
test('voided receipt does not count as paid; receipt before complete still needs delivery confirmation', () => {
  assert.equal(
    orderWorkflow('Completed', [{ ...receipt, voidedAtUtc: '2026-10-30T01:00:00Z' }], true).primary,
    'receipt',
  );
  assert.equal(orderWorkflow('HandedOver', [receipt], true).primary, 'complete');
  assert.equal(orderWorkflow('HandedOver', [receipt], true).allDone, false);
});
test('primary and secondary preserve allowed paths including direct handover', () => {
  const confirmed = orderWorkflow('Confirmed', [], true);
  assert.equal(confirmed.primary, 'prepare');
  assert.deepEqual(confirmed.secondary, ['handover', 'reallocate', 'cancel', 'complete-paid']);
  assert.equal(orderWorkflow('PendingConfirmation', [], true).primary, 'confirm');
  assert.deepEqual(orderWorkflow('Cancelled', [], true).actions, []);
  assert.deepEqual(orderWorkflow('Unexpected', [], true).actions, []);
});

test('combined completion is visible only after confirmation with known unpaid receipts and permission', () => {
  for (const state of ['Confirmed', 'Preparing', 'HandedOver']) {
    assert.ok(orderWorkflow(state, [], true).actions.includes('complete-paid'));
    assert.equal(orderWorkflow(state, [], false).actions.includes('complete-paid'), false);
    assert.equal(orderWorkflow(state, [], true, false).actions.includes('complete-paid'), false);
    assert.equal(orderWorkflow(state, [receipt], true).actions.includes('complete-paid'), false);
  }
  for (const state of ['PendingConfirmation', 'Cancelled', 'Completed'])
    assert.equal(orderWorkflow(state, [], true).actions.includes('complete-paid'), false);
});
