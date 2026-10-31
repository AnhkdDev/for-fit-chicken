import type { CSSProperties } from 'react';
import type { HomeImageFrame, ImageFrameSettings } from './types';
export const defaultFrame = (): ImageFrameSettings => ({ fit: 'contain', x: 50, y: 50, zoom: 1 });
export const clamp = (v: number, min: number, max: number) => Math.min(max, Math.max(min, v));
export function normalizeFrame(v?: ImageFrameSettings | null): ImageFrameSettings {
  const fit = v?.fit === 'cover' ? 'cover' : 'contain';
  const number = (n: unknown, fallback: number, min: number, max: number) =>
    typeof n === 'number' && Number.isFinite(n) ? clamp(n, min, max) : fallback;
  return {
    fit,
    x: number(v?.x, 50, 0, 100),
    y: number(v?.y, 50, 0, 100),
    zoom: fit === 'cover' ? number(v?.zoom, 1, 1, 3) : 1,
  };
}
export function frameForAsset(
  frame: HomeImageFrame | undefined,
  assetId: number,
): HomeImageFrame | undefined {
  return frame?.assetId === assetId ? frame : undefined;
}
export function framingStyle(frame?: HomeImageFrame): CSSProperties {
  const desktop = normalizeFrame(frame?.desktop);
  const mobile = normalizeFrame(frame?.mobile ?? desktop);
  return Object.fromEntries(
    [
      ['desktop', desktop],
      ['mobile', mobile],
    ].flatMap(([key, v]) => {
      const f = v as ImageFrameSettings;
      return [
        [`--${key}-fit`, f.fit],
        [`--${key}-x`, `${f.x}%`],
        [`--${key}-y`, `${f.y}%`],
        [`--${key}-zoom`, f.zoom],
      ];
    }),
  ) as CSSProperties;
}
