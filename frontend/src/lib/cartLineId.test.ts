import { describe, expect, it } from 'vitest';
import { composeDrinkLineId, decomposeDrinkLineId } from './cartLineId';

describe('composeDrinkLineId', () => {
  it('collapses syrups selected in a different order into the same id', () => {
    const a = composeDrinkLineId('product-1', 'oat', 'espresso', ['vanilla', 'caramel']);
    const b = composeDrinkLineId('product-1', 'oat', 'espresso', ['caramel', 'vanilla']);

    expect(a).toBe(b);
    expect(a).toBe('product-1::oat::shot::caramel-vanilla');
  });

  it('produces the same id for identical selections (matching lines)', () => {
    const a = composeDrinkLineId('product-1', 'full', null, []);
    const b = composeDrinkLineId('product-1', 'full', null, []);

    expect(a).toBe(b);
  });

  it('produces a different id when the milk selection differs', () => {
    const a = composeDrinkLineId('product-1', 'oat', null, []);
    const b = composeDrinkLineId('product-1', 'almond', null, []);

    expect(a).not.toBe(b);
  });

  it('produces a different id when the shot selection differs', () => {
    const withShot = composeDrinkLineId('product-1', 'oat', 'espresso', []);
    const withoutShot = composeDrinkLineId('product-1', 'oat', null, []);

    expect(withShot).not.toBe(withoutShot);
    expect(withShot).toContain('::shot::');
    expect(withoutShot).toContain('::noshot::');
  });

  it('produces a different id when the syrup selection differs', () => {
    const oneSyrup = composeDrinkLineId('product-1', 'oat', null, ['vanilla']);
    const twoSyrups = composeDrinkLineId('product-1', 'oat', null, ['vanilla', 'caramel']);

    expect(oneSyrup).not.toBe(twoSyrups);
  });

  it('produces a different id for a different product entirely', () => {
    const a = composeDrinkLineId('product-1', 'oat', null, []);
    const b = composeDrinkLineId('product-2', 'oat', null, []);

    expect(a).not.toBe(b);
  });
});

describe('decomposeDrinkLineId', () => {
  it('recovers productId, milkOptionId, hasShot, and syrupOptionIds from a composed id', () => {
    const id = composeDrinkLineId('product-1', 'oat', 'espresso', ['vanilla', 'caramel']);

    const result = decomposeDrinkLineId(id);

    expect(result).toEqual({ productId: 'product-1', milkOptionId: 'oat', hasShot: true, syrupOptionIds: ['caramel', 'vanilla'] });
  });

  it('reports hasShot false and empty syrups for a bare milk-only line', () => {
    const id = composeDrinkLineId('product-1', 'full', null, []);

    const result = decomposeDrinkLineId(id);

    expect(result).toEqual({ productId: 'product-1', milkOptionId: 'full', hasShot: false, syrupOptionIds: [] });
  });

  it('treats a plain (non-composed) id as a non-drink line with no milk option', () => {
    const result = decomposeDrinkLineId('plain-product-id');

    expect(result).toEqual({ productId: 'plain-product-id', milkOptionId: null, hasShot: false, syrupOptionIds: [] });
  });
});
