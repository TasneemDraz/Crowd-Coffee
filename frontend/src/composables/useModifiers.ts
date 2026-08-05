import { getProductModifiers } from '../api/catalog';
import type { ModifierGroupDto } from '../types/catalog';

// Modifier groups are category-wide (§4a, ProductModifierGroup) — every product in a category
// gets the identical group/option list, so the response is cached per category slug rather than
// per product. Keyed off whichever product slug happened to trigger the first fetch for that
// category; the response content is the same regardless of which product in the category asked.
const cache = new Map<string, Promise<ModifierGroupDto[]>>();

export function useModifiers() {
  function getForCategory(categorySlug: string, productSlugForFetch: string): Promise<ModifierGroupDto[]> {
    let entry = cache.get(categorySlug);
    if (!entry) {
      entry = getProductModifiers(productSlugForFetch);
      cache.set(categorySlug, entry);
    }
    return entry;
  }

  return { getForCategory };
}
