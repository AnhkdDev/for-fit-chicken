export interface Page<T> {
  items: T[];
  total: number;
  pageNumber: number;
  pageSize: number;
}
export interface Variant {
  id: number;
  skuCode: string;
  label: string;
  netWeightGrams: number;
  sellingPrice: number;
  isActive: boolean;
  available: number;
  version?: string;
}
export interface Product {
  id: number;
  name: string;
  slug: string;
  shortDescription?: string;
  description?: string;
  ingredients?: string;
  nutritionText?: string;
  usageInstructions?: string;
  storageInstructions?: string;
  isActive: boolean;
  isPublished: boolean;
  isFeatured: boolean;
  sortOrder: number;
  version?: string;
  variants: Variant[];
  images: { id: number; assetId: number; altText?: string; url: string }[];
}
export interface ImageFrameSettings {
  fit: 'contain' | 'cover';
  x: number;
  y: number;
  zoom: number;
}
export interface HomeImageFrame {
  assetId: number;
  desktop: ImageFrameSettings;
  mobile?: ImageFrameSettings | null;
}
export interface Section {
  sectionKey: string;
  content: {
    title: string;
    body?: string;
    assetIds: number[];
    productIds?: number[];
    productMainImages?: Record<string, number>;
    productImageFrames?: Record<string, HomeImageFrame>;
    enabled?: boolean;
    linkLabel?: string;
    linkUrl?: string;
    version?: string;
  };
  version?: string;
  publishedAtUtc?: string;
}
export interface Gym {
  id: number;
  name: string;
  publicAddress: string;
  publicPhone?: string;
  publicDescription?: string;
  coverAssetId?: number;
  gymCode?: string;
  locationId?: number;
  version?: string;
}
export interface Profile {
  id: number;
  userName: string;
  displayName: string;
  roles: string[];
}
export interface CartItem {
  variantId: number;
  quantity: number;
}
export interface Order {
  id: number;
  orderNumber: string;
  status: string;
  recipientName: string;
  recipientPhone: string;
  recipientAddress: string;
  customerNote?: string;
  internalNote?: string;
  itemsSubtotal: number;
  createdAtUtc: string;
  version: string;
  items: { id: number; productName: string; label: string; quantity: number; lineTotal: number }[];
}
export interface Asset {
  id: number;
  url: string;
  contentType: string;
  byteSize: number;
  createdAtUtc?: string;
  isReferenced?: boolean;
}
