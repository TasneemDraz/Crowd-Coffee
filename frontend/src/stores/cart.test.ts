import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it } from 'vitest';
import { useCartStore } from './cart';

describe('useCartStore', () => {
  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
  });

  it('addLine creates a new line for a not-yet-seen id', () => {
    const cart = useCartStore();

    cart.addLine({ id: 'line-a', name: 'Latte (Oat Milk)', price: 140, qty: 1 });

    expect(cart.lines).toHaveLength(1);
    expect(cart.count).toBe(1);
  });

  it('addLine increments qty on a matching id instead of creating a second line', () => {
    const cart = useCartStore();

    cart.addLine({ id: 'line-a', name: 'Latte (Oat Milk)', price: 140, qty: 1 });
    cart.addLine({ id: 'line-a', name: 'Latte (Oat Milk)', price: 140, qty: 2 });

    expect(cart.lines).toHaveLength(1);
    expect(cart.lines[0].qty).toBe(3);
    expect(cart.count).toBe(3);
  });

  it('addLine with a differing id creates a separate line, even for the same product', () => {
    const cart = useCartStore();

    cart.addLine({ id: 'product-1::oat::shot::vanilla', name: 'Latte (Oat, +Shot, +Vanilla)', price: 180, qty: 1 });
    cart.addLine({ id: 'product-1::almond::noshot::', name: 'Latte (Almond)', price: 140, qty: 1 });

    expect(cart.lines).toHaveLength(2);
    expect(cart.count).toBe(2);
  });

  it('persists lines to localStorage under crowdcoffee_cart', () => {
    const cart = useCartStore();

    cart.addLine({ id: 'line-a', name: 'Latte', price: 140, qty: 1 });

    const stored = JSON.parse(localStorage.getItem('crowdcoffee_cart') ?? '[]');
    expect(stored).toEqual([{ id: 'line-a', name: 'Latte', price: 140, qty: 1 }]);
  });

  it('changeQty decrementing to zero removes the line', () => {
    const cart = useCartStore();
    cart.addLine({ id: 'line-a', name: 'Latte', price: 140, qty: 1 });

    cart.changeQty('line-a', -1);

    expect(cart.lines).toHaveLength(0);
  });

  it('subtotal sums price * qty across lines', () => {
    const cart = useCartStore();
    cart.addLine({ id: 'a', name: 'A', price: 100, qty: 2 });
    cart.addLine({ id: 'b', name: 'B', price: 50, qty: 3 });

    expect(cart.subtotal).toBe(350);
  });

  it('deliveryFee is 30 for delivery mode and 0 for pickup, persisted under crowdcoffee_order_mode', () => {
    const cart = useCartStore();
    expect(cart.orderMode).toBe('delivery');
    expect(cart.deliveryFee).toBe(30);

    cart.setOrderMode('pickup');

    expect(cart.deliveryFee).toBe(0);
    expect(localStorage.getItem('crowdcoffee_order_mode')).toBe('pickup');
  });

  it('total is subtotal plus deliveryFee', () => {
    const cart = useCartStore();
    cart.addLine({ id: 'a', name: 'A', price: 100, qty: 1 });

    expect(cart.total).toBe(130); // 100 + 30 delivery fee

    cart.setOrderMode('pickup');
    expect(cart.total).toBe(100);
  });
});
