import type { CategoryDto, ModifierGroupDto, ProductDto } from '../types/catalog';

// In dev, Vite's server.proxy forwards /api to the ASP.NET Core API (vite.config.ts) so the
// browser sees a same-origin request and no CORS configuration is needed on the API yet
// (that's Epic 16's job). In production this is overridden to wherever the API is actually
// deployed (Epic 17).
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`);
  if (!response.ok) {
    throw new Error(`Request to ${path} failed with status ${response.status}`);
  }
  return response.json() as Promise<T>;
}

export function getCategories(): Promise<CategoryDto[]> {
  return getJson<CategoryDto[]>('/catalog/categories');
}

export function getProducts(): Promise<ProductDto[]> {
  return getJson<ProductDto[]>('/catalog/products');
}

export function getProductModifiers(productSlug: string): Promise<ModifierGroupDto[]> {
  return getJson<ModifierGroupDto[]>(`/catalog/products/${encodeURIComponent(productSlug)}/modifiers`);
}
