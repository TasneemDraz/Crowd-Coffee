// Mirrors CrowdCoffee.Application.Catalog's API-facing DTOs (server/src/CrowdCoffee.Application/Catalog/CatalogDtos.cs).

export interface CategoryDto {
  id: string;
  slug: string;
  label: string;
  description: string | null;
  displayOrder: number;
}

export interface ProductDto {
  id: string;
  categorySlug: string;
  slug: string;
  name: string;
  description: string | null;
  price: number;
  imageUrl: string | null;
  displayOrder: number;
  isAvailable: boolean;
}

export interface ModifierOptionDto {
  id: string;
  slug: string;
  name: string;
  priceDelta: number;
  displayOrder: number;
  isAvailable: boolean;
}

export interface ModifierGroupDto {
  id: string;
  slug: string;
  name: string;
  selectionType: 'ExactlyOne' | 'UpToN';
  maxSelections: number;
  isRequired: boolean;
  displayOrder: number;
  options: ModifierOptionDto[];
}
