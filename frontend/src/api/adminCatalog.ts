import { authorizedFetch, authorizedJson, authorizedJsonBody, ApiError } from '../lib/adminApi';
import type {
  AdminCategory,
  AdminModifierGroup,
  AdminProduct,
  CreateCategoryRequest,
  CreateProductRequest,
  UpdateCategoryRequest,
  UpdateModifierOptionRequest,
  UpdateProductRequest,
} from '../types/adminCatalog';

export function getAdminCategories(): Promise<AdminCategory[]> {
  return authorizedJson<AdminCategory[]>('/admin/catalog/categories');
}

export function createCategory(request: CreateCategoryRequest): Promise<AdminCategory> {
  return authorizedJsonBody<AdminCategory>('/admin/catalog/categories', 'POST', request);
}

export function updateCategory(categoryId: string, request: UpdateCategoryRequest): Promise<AdminCategory> {
  return authorizedJsonBody<AdminCategory>(`/admin/catalog/categories/${encodeURIComponent(categoryId)}`, 'PUT', request);
}

export function getAdminProducts(): Promise<AdminProduct[]> {
  return authorizedJson<AdminProduct[]>('/admin/catalog/products');
}

export function createProduct(request: CreateProductRequest): Promise<AdminProduct> {
  return authorizedJsonBody<AdminProduct>('/admin/catalog/products', 'POST', request);
}

export function updateProduct(productId: string, request: UpdateProductRequest): Promise<AdminProduct> {
  return authorizedJsonBody<AdminProduct>(`/admin/catalog/products/${encodeURIComponent(productId)}`, 'PUT', request);
}

export function getAdminModifierGroups(): Promise<AdminModifierGroup[]> {
  return authorizedJson<AdminModifierGroup[]>('/admin/catalog/modifier-groups');
}

export function updateModifierOption(optionId: string, request: UpdateModifierOptionRequest) {
  return authorizedJsonBody(`/admin/catalog/modifier-options/${encodeURIComponent(optionId)}`, 'PUT', request);
}

/** Multipart upload — deliberately doesn't go through authorizedJsonBody (which always sends
 * JSON): the browser must set its own multipart boundary in the Content-Type header, which it
 * only does when FormData is handed to fetch directly with no Content-Type set manually. */
export async function uploadProductImage(file: File): Promise<string> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await authorizedFetch('/admin/catalog/images', { method: 'POST', body: formData });
  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new ApiError(problem?.detail ?? problem?.title ?? `Image upload failed with status ${response.status}.`, response.status);
  }

  const data = (await response.json()) as { url: string };
  return data.url;
}
