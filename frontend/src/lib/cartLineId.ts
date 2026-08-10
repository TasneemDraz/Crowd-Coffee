/**
 * Composes the synthetic cart line id for a customized drink (§4a): a drink customized
 * differently is a different cart line, not a qty bump on the same one. Syrup ids are sorted
 * before joining so vanilla+caramel and caramel+vanilla collapse into one line — matching the
 * design's own scheme exactly (`{productId}::{milkId}::{shot|noshot}::{sortedSyrupIds}`).
 *
 * Exported from `lib/` (not a composable) because it's a pure function with no Vue reactivity —
 * Epic 7's modal calls it directly, and Epic 8's server-side re-pricing mirrors this same
 * scheme, so it needs to stay a plain, easily-portable/testable function.
 */
export function composeDrinkLineId(
  productId: string,
  milkOptionId: string,
  shotOptionId: string | null,
  syrupOptionIds: string[],
): string {
  const shotPart = shotOptionId ? 'shot' : 'noshot';
  // Comma, not '-': syrup option ids are GUIDs, which themselves contain hyphens — joining with
  // '-' made decomposeDrinkLineId's split('-') shred a single GUID into garbage fragments.
  const syrupPart = [...syrupOptionIds].sort().join(',');
  return `${productId}::${milkOptionId}::${shotPart}::${syrupPart}`;
}

export interface DecomposedDrinkLine {
  productId: string;
  milkOptionId: string | null;
  /** shotOptionId can't be recovered from a plain "shot" marker — only whether a shot was
   * selected at all. Epic 9 checkout doesn't need the specific shot option id from the cart line
   * (there's only ever one shot option in the seed data), so callers needing the id look it up
   * from the product's modifier groups instead of trusting this decomposed value.
   */
  hasShot: boolean;
  syrupOptionIds: string[];
}

/**
 * Inverse of composeDrinkLineId, for Epic 9 checkout: the cart only stores the composed string
 * id (Epic 8.1), so submitting an order needs to pull the productId/milk/syrup ids back out of
 * it. Non-drink lines (Epic 6/7's plain `cart.add()`) have no "::" separators at all — those are
 * just a bare product id, so this returns null for milk/syrups rather than guessing.
 */
export function decomposeDrinkLineId(id: string): DecomposedDrinkLine {
  const parts = id.split('::');
  if (parts.length !== 4) {
    return { productId: id, milkOptionId: null, hasShot: false, syrupOptionIds: [] };
  }

  const [productId, milkOptionId, shotPart, syrupPart] = parts;
  return {
    productId,
    milkOptionId,
    hasShot: shotPart === 'shot',
    syrupOptionIds: syrupPart ? syrupPart.split(',') : [],
  };
}
