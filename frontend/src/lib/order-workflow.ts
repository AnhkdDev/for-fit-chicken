export interface Receipt {
  id: number;
  amount: number;
  receivedAtUtc: string;
  voidedAtUtc?: string;
}
export const actionLabels: Record<string, string> = {
  confirm: 'Đã gọi khách — xác nhận đơn',
  prepare: 'Bắt đầu chuẩn bị',
  handover: 'Bàn giao giao hàng',
  complete: 'Xác nhận giao thành công',
  'complete-paid': 'Hoàn thành & đã nhận tiền',
  receipt: 'Ghi nhận đã nhận đủ tiền',
  cancel: 'Hủy đơn',
  reallocate: 'Phân bổ lại lô',
};
export function orderWorkflow(
  status: string,
  receipts: Receipt[] | undefined,
  ready: boolean,
  canReceive = true,
) {
  const paid = ready && !!receipts?.some((r) => !r.voidedAtUtc);
  const transitions: Record<string, string[]> = {
    PendingConfirmation: ['confirm', 'cancel'],
    Confirmed: ['prepare', 'handover', 'reallocate', 'cancel'],
    Preparing: ['handover', 'reallocate', 'cancel'],
    HandedOver: ['complete'],
    Completed: [],
    Cancelled: [],
  };
  const actions = [...(transitions[status] ?? [])];
  if (ready && canReceive && !paid && ['HandedOver', 'Completed'].includes(status))
    actions.push('receipt');
  if (ready && canReceive && !paid && ['Confirmed', 'Preparing', 'HandedOver'].includes(status))
    actions.push('complete-paid');
  const primary = actions.find((a) => a !== 'cancel' && a !== 'reallocate');
  return {
    paid,
    actions,
    primary,
    secondary: actions.filter((a) => a !== primary),
    allDone: status === 'Completed' && paid,
  };
}
export function actionExplanation(action: string) {
  return (
    (
      {
        confirm: 'Xác nhận đã liên hệ khách và đồng ý chuẩn bị đơn. Hệ thống sẽ giữ hàng.',
        prepare: 'Đánh dấu nhóm bắt đầu chuẩn bị sản phẩm cho đơn.',
        handover:
          'Xác nhận đã bàn giao hàng cho người giao. Hệ thống sẽ xuất kho; không thể hủy đơn theo luồng thông thường sau bước này.',
        complete: 'Xác nhận khách đã nhận hàng. Tiền COD vẫn được theo dõi riêng.',
        'complete-paid':
          'Chỉ dùng khi khách đã nhận hàng và nhóm đã thực sự nhận đủ tiền. Hệ thống sẽ xuất kho nếu chưa bàn giao, hoàn thành giao hàng và ghi nhận đủ tiền trong cùng một lần lưu.',
        receipt:
          'Chỉ xác nhận khi nhóm đã thực sự nhận đủ tiền hàng. Không dùng thao tác này chỉ vì khách đã nhận hàng.',
        cancel: 'Hủy đơn và giải phóng hàng đã giữ nếu có. Cần ghi rõ lý do.',
        reallocate: 'Hệ thống sẽ phân bổ lại các lô hàng đang giữ theo điều kiện hiện tại.',
      } as Record<string, string>
    )[action] ?? ''
  );
}
