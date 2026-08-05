import { ref } from 'vue';
import { getCategories, getProducts } from '../api/catalog';
import type { CategoryDto, ProductDto } from '../types/catalog';

const categories = ref<CategoryDto[]>([]);
const products = ref<ProductDto[]>([]);
const isLoading = ref(false);
const error = ref<string | null>(null);
let loadPromise: Promise<void> | null = null;

/**
 * Fetch-once, module-scoped cache (Story 6.3, T6.3.1): categories/products are fetched a single
 * time per page load and shared by every component that calls this composable, no matter how
 * many times it's invoked. Deliberately not Pinia — there's no mutable shared state here, just a
 * memoized GET; Epic 8 introduces Pinia for the cart, which does need shared mutable state.
 */
export function useCatalog() {
  function ensureLoaded(): Promise<void> {
    if (loadPromise) {
      return loadPromise;
    }

    isLoading.value = true;
    error.value = null;

    loadPromise = Promise.all([getCategories(), getProducts()])
      .then(([categoryList, productList]) => {
        categories.value = categoryList;
        products.value = productList;
      })
      .catch((err: unknown) => {
        error.value = err instanceof Error ? err.message : 'Failed to load the menu.';
        loadPromise = null; // allow retry on next call
        throw err;
      })
      .finally(() => {
        isLoading.value = false;
      });

    return loadPromise;
  }

  return { categories, products, isLoading, error, ensureLoaded };
}
