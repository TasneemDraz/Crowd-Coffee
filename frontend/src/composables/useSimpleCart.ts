import { computed, ref } from 'vue';
import type { ProductDto } from '../types/catalog';

const CART_STORAGE_KEY = 'crowdcoffee_cart';

interface SimpleCartLine {
  id: string;
  name: string;
  price: number;
  qty: number;
}

function readCart(): SimpleCartLine[] {
  try {
    const raw = localStorage.getItem(CART_STORAGE_KEY);
    return raw ? (JSON.parse(raw) as SimpleCartLine[]) : [];
  } catch {
    return [];
  }
}

const lines = ref<SimpleCartLine[]>(readCart());

function persist() {
  localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(lines.value));
}

/**
 * Minimal localStorage cart (§3: key `crowdcoffee_cart`) so the menu page's Add button and
 * floating cart bar are functional and match the design now, ahead of Epic 8. Deliberately NOT
 * the modifier-aware composed-line-identity cart Story 8.1 builds
 * (`{productId}::{milkId}::{shot|noshot}::{syrups}`) — that scheme only makes sense once Epic
 * 7's customization modal exists to produce milk/shot/syrup selections. Every product's Add
 * currently goes straight to cart at base price; Epic 7 will intercept DRINK_CATS products to
 * open the modal instead (Story 7.3), and Epic 8 replaces this composable's storage shape,
 * reusing the same localStorage key.
 */
export function useSimpleCart() {
  function add(product: ProductDto) {
    const existing = lines.value.find((line) => line.id === product.id);
    if (existing) {
      existing.qty += 1;
    } else {
      lines.value.push({ id: product.id, name: product.name, price: product.price, qty: 1 });
    }
    persist();
  }

  const count = computed(() => lines.value.reduce((sum, line) => sum + line.qty, 0));
  const subtotal = computed(() => lines.value.reduce((sum, line) => sum + line.price * line.qty, 0));

  return { lines, add, count, subtotal };
}
