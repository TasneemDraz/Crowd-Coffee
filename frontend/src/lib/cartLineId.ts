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
  const syrupPart = [...syrupOptionIds].sort().join('-');
  return `${productId}::${milkOptionId}::${shotPart}::${syrupPart}`;
}
