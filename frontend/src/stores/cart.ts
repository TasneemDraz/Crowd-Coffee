import { defineStore } from 'pinia';
import type { ProductDto } from '../types/catalog';

export interface CartLine {
  id: string;
  name: string;
  price: number;
  qty: number;
}

export type OrderMode = 'delivery' | 'pickup';

const CART_STORAGE_KEY = 'crowdcoffee_cart';
const ORDER_MODE_STORAGE_KEY = 'crowdcoffee_order_mode';

// Flat 30 EGP everywhere / free on pickup (§3) — §14.1 leaves open whether delivery should
// instead be distance-capped, but nothing in the design or seed data encodes zones today.
const DELIVERY_FEE = 30;

function readCart(): CartLine[] {
  try {
    const raw = localStorage.getItem(CART_STORAGE_KEY);
    return raw ? (JSON.parse(raw) as CartLine[]) : [];
  } catch {
    return [];
  }
}

function readOrderMode(): OrderMode {
  return localStorage.getItem(ORDER_MODE_STORAGE_KEY) === 'pickup' ? 'pickup' : 'delivery';
}

/**
 * The real cart (§3 localStorage keys `crowdcoffee_cart`/`crowdcoffee_order_mode`), replacing
 * Epic 6/7's `useSimpleCart` composable now that there's genuinely shared, mutable app state
 * (cart contents + order mode) spanning the menu, cart, and — eventually — checkout pages, which
 * is exactly the case Pinia is for (unlike Epic 5/6's plain fetch-once composables). `addLine`
 * is the general path (drink lines with a composed id from `lib/cartLineId.ts` collapse into the
 * same line on a matching id, exactly as §4a specifies); `add` is the non-drink convenience
 * wrapper.
 */
export const useCartStore = defineStore('cart', {
  state: () => ({
    lines: readCart(),
    orderMode: readOrderMode(),
  }),
  getters: {
    count: (state) => state.lines.reduce((sum, line) => sum + line.qty, 0),
    subtotal: (state) => state.lines.reduce((sum, line) => sum + line.price * line.qty, 0),
    deliveryFee: (state) => (state.orderMode === 'pickup' ? 0 : DELIVERY_FEE),
    total(): number {
      return this.subtotal + this.deliveryFee;
    },
  },
  actions: {
    persistLines() {
      localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(this.lines));
    },
    addLine(line: CartLine) {
      const existing = this.lines.find((l) => l.id === line.id);
      if (existing) {
        existing.qty += line.qty;
      } else {
        this.lines.push({ ...line });
      }
      this.persistLines();
    },
    add(product: ProductDto) {
      this.addLine({ id: product.id, name: product.name, price: product.price, qty: 1 });
    },
    changeQty(id: string, delta: number) {
      const line = this.lines.find((l) => l.id === id);
      if (!line) return;
      const nextQty = line.qty + delta;
      if (nextQty <= 0) {
        this.removeLine(id);
        return;
      }
      line.qty = nextQty;
      this.persistLines();
    },
    removeLine(id: string) {
      this.lines = this.lines.filter((l) => l.id !== id);
      this.persistLines();
    },
    setOrderMode(mode: OrderMode) {
      this.orderMode = mode;
      localStorage.setItem(ORDER_MODE_STORAGE_KEY, mode);
    },
  },
});
