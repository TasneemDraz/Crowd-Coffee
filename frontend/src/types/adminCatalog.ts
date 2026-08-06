// Mirrors CrowdCoffee.Application.Catalog's admin-facing DTOs (server/src/CrowdCoffee.Application/Catalog/AdminCatalogDtos.cs).

export interface AdminCategory {
  id: string;
  slug: string;
  label: string;
  description: string | null;
  displayOrder: number;
  isActive: boolean;
}

export interface CreateCategoryRequest {
  slug: string;
  label: string;
  description: string | null;
  displayOrder: number;
}

export interface UpdateCategoryRequest {
  label: string;
  description: string | null;
  displayOrder: number;
  isActive: boolean;
}

export interface AdminProduct {
  id: string;
  categorySlug: string;
  slug: string;
  name: string;
  description: string | null;
  price: number;
  imageUrl: string | null;
  displayOrder: number;
  isAvailable: boolean;
  isActive: boolean;
}

export interface CreateProductRequest {
  categorySlug: string;
  slug: string;
  name: string;
  description: string | null;
  price: number;
  displayOrder: number;
  imageUrl: string | null;
}

export interface UpdateProductRequest {
  name: string;
  description: string | null;
  price: number;
  imageUrl: string | null;
  displayOrder: number;
  isAvailable: boolean;
  isActive: boolean;
}

export interface AdminModifierOption {
  id: string;
  slug: string;
  name: string;
  priceDelta: number;
  displayOrder: number;
  isActive: boolean;
}

export interface AdminModifierGroup {
  id: string;
  slug: string;
  name: string;
  selectionType: 'ExactlyOne' | 'UpToN';
  maxSelections: number;
  isRequired: boolean;
  displayOrder: number;
  options: AdminModifierOption[];
}

export interface UpdateModifierOptionRequest {
  priceDelta: number;
  isActive: boolean;
}
