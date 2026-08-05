import { computed, ref } from 'vue';
import type { ProductDto } from '../types/catalog';

const CART_STORAGE_KEY = 'crowdcoffee_cart';

export interface SimpleCartLine {
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
 * floating cart bar are functional and match the design now, ahead of Epic 8's Pinia store.
 * `addLine` is the general path — Epic 7's customization modal composes its own
 * `{productId}::{milkId}::{shot|noshot}::{syrups}` id (see `lib/cartLineId.ts`) and pushes the
 * resulting line straight through it, exactly mirroring how the design's own `handleAdd` and
 * `confirmAddModal` share one push-into-cart-array code path. `add` is the thin non-drink-path
 * convenience wrapper (product id/name/price as-is, qty 1). Epic 8 replaces this composable's
 * storage shape wholesale but keeps the same localStorage key.
 */
export function useSimpleCart() {
  function addLine(line: SimpleCartLine) {
    const existing = lines.value.find((l) => l.id === line.id);
    if (existing) {
      existing.qty += line.qty;
    } else {
      lines.value.push({ ...line });
    }
    persist();
  }

  function add(product: ProductDto) {
    addLine({ id: product.id, name: product.name, price: product.price, qty: 1 });
  }

  const count = computed(() => lines.value.reduce((sum, line) => sum + line.qty, 0));
  const subtotal = computed(() => lines.value.reduce((sum, line) => sum + line.price * line.qty, 0));

  return { lines, add, addLine, count, subtotal };
}
