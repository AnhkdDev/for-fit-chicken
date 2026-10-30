export const operatorAreas = ['', 'san-pham', 'kho', 'phong-gym', 'don-hang', 'bao-cao', 'thong-bao'] as const;
export function canOpenAdminArea(owner: boolean, path: string) {
  const key = path.replace(/^\/+|\/+$/g, '').split('/')[0];
  return owner || operatorAreas.some((area) => area === key);
}

