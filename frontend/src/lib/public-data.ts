export interface PublicData {
  path?: string;
  status?: number;
  products: import('./types').Product[];
  sections: import('./types').Section[];
  gyms: import('./types').Gym[];
}
export const emptyPublicData = (): PublicData => ({ products: [], sections: [], gyms: [] });
