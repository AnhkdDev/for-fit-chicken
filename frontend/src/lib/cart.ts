import type { CartItem, Variant } from './types.ts';

export function planCartAdd(
  cart: CartItem[],
  variant: Variant | undefined,
  quantity: number,
): { ok: true; items: CartItem[] } | { ok: false; message: string } {
  if (!variant?.isActive || variant.available <= 0)
    return { ok: false, message: 'Phân loại này hiện không còn hàng.' };
  if (!Number.isInteger(quantity) || quantity < 1)
    return { ok: false, message: 'Vui lòng chọn số lượng nguyên, tối thiểu 1 gói.' };
  const existing = cart.find((x) => x.variantId === variant.id);
  const limit = Math.min(100, variant.available);
  if ((existing?.quantity ?? 0) + quantity > limit)
    return {
      ok: false,
      message: `Giỏ hàng chỉ có thể chứa tối đa ${limit} gói của phân loại này. Hãy kiểm tra số lượng đã có trong giỏ.`,
    };
  if (!existing && cart.length >= 20)
    return { ok: false, message: 'Mỗi đơn tối đa 20 phân loại khác nhau.' };
  return {
    ok: true,
    items: existing
      ? cart.map((x) =>
          x.variantId === variant.id ? { ...x, quantity: x.quantity + quantity } : x,
        )
      : [...cart, { variantId: variant.id, quantity }],
  };
}
