<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import type { ModifierGroupDto, ProductDto } from '../../types/catalog';
import { useModifiers } from '../../composables/useModifiers';
import type { CartLine } from '../../stores/cart';
import { composeDrinkLineId } from '../../lib/cartLineId';

const props = defineProps<{ product: ProductDto; categoryLabel: string }>();
const emit = defineEmits<{ close: []; confirm: [line: CartLine] }>();

const { getForCategory } = useModifiers();

const groups = ref<ModifierGroupDto[]>([]);
const isLoading = ref(true);
const error = ref<string | null>(null);

const selectedMilkId = ref<string | null>(null);
const selectedShotId = ref<string | null>(null);
const selectedSyrupIds = ref<string[]>([]);
const qty = ref(1);

const closeButtonEl = ref<HTMLButtonElement | null>(null);

async function load() {
  isLoading.value = true;
  error.value = null;
  selectedMilkId.value = null;
  selectedShotId.value = null;
  selectedSyrupIds.value = [];
  qty.value = 1;
  try {
    groups.value = await getForCategory(props.product.categorySlug, props.product.slug);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load options.';
  } finally {
    isLoading.value = false;
  }

  // Story 16.3 (T16.3.1): move focus into the dialog on open — without this, focus stays on
  // whatever menu "Add" button triggered it, so a screen-reader/keyboard user gets no signal
  // a dialog just opened over the page.
  await nextTick();
  closeButtonEl.value?.focus();
}

onMounted(load);
// The modal component instance can be reused across products (parent keeps one <DrinkCustomizeModal>
// and swaps the prop) rather than remounted — reload whenever the target product changes.
watch(() => props.product.id, load);

// Story 16.3 (T16.3.1): a keyboard-only user has no way to dismiss this modal without Escape —
// the overlay's click-to-close only fires on a pointer event. The close button already covers
// the "give focus somewhere obvious" half of the standard modal pattern (it's the first
// focusable element in DOM order), so this just adds the other half.
function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    emit('close');
  }
}

onMounted(() => window.addEventListener('keydown', onKeydown));
onUnmounted(() => window.removeEventListener('keydown', onKeydown));

const milkGroup = computed(() => groups.value.find((g) => g.slug === 'milk') ?? null);
const shotGroup = computed(() => groups.value.find((g) => g.slug === 'shot') ?? null);
const syrupGroup = computed(() => groups.value.find((g) => g.slug === 'syrups') ?? null);

function selectMilk(optionId: string) {
  selectedMilkId.value = optionId;
}

function toggleShot(optionId: string) {
  selectedShotId.value = selectedShotId.value === optionId ? null : optionId;
}

function toggleSyrup(optionId: string) {
  const max = syrupGroup.value?.maxSelections ?? 2;
  const idx = selectedSyrupIds.value.indexOf(optionId);
  if (idx >= 0) {
    selectedSyrupIds.value.splice(idx, 1);
  } else if (selectedSyrupIds.value.length < max) {
    selectedSyrupIds.value.push(optionId);
  }
}

function decQty() {
  qty.value = Math.max(1, qty.value - 1);
}
function incQty() {
  qty.value += 1;
}

function priceDeltaFor(group: ModifierGroupDto | null, optionId: string | null): number {
  if (!group || !optionId) return 0;
  return group.options.find((o) => o.id === optionId)?.priceDelta ?? 0;
}

function formatDelta(delta: number): string {
  return `+EGP ${delta > 0 ? delta : 0}`;
}

function optionName(group: ModifierGroupDto | null, optionId: string | null): string | null {
  return group?.options.find((o) => o.id === optionId)?.name ?? null;
}

// Display-only estimate for the modal UI — the server recomputes unitPrice from base price +
// modifier deltas at checkout (§4a rule 3, Epic 9); this number is never sent as-is or trusted
// for the actual charge.
const unitPrice = computed(() => {
  const milkDelta = priceDeltaFor(milkGroup.value, selectedMilkId.value);
  const shotDelta = priceDeltaFor(shotGroup.value, selectedShotId.value);
  const syrupDelta = selectedSyrupIds.value.reduce((sum, id) => sum + priceDeltaFor(syrupGroup.value, id), 0);
  return props.product.price + milkDelta + shotDelta + syrupDelta;
});

const lineTotal = computed(() => unitPrice.value * qty.value);

const confirmDisabled = computed(() => {
  if (isLoading.value || !milkGroup.value) return true;
  // "No Milk" is a valid explicit option in the group itself — required-but-unchosen is the only
  // thing gating Add, never a hardcoded assumption that milk itself is mandatory (§4a: a null
  // milk value must never fall through as valid).
  return milkGroup.value.isRequired && !selectedMilkId.value;
});

function confirm() {
  if (confirmDisabled.value || !milkGroup.value || !selectedMilkId.value) {
    return;
  }

  const milkName = optionName(milkGroup.value, selectedMilkId.value)!;
  const shotName = optionName(shotGroup.value, selectedShotId.value);
  const syrupNames = selectedSyrupIds.value
    .map((id) => optionName(syrupGroup.value, id))
    .filter((name): name is string => name !== null);

  const parts = [milkName, ...(shotName ? [`+ ${shotName}`] : []), ...syrupNames.map((n) => `+ ${n}`)];
  const composedName = `${props.product.name} (${parts.join(', ')})`;

  const id = composeDrinkLineId(props.product.id, selectedMilkId.value, selectedShotId.value, selectedSyrupIds.value);

  emit('confirm', { id, name: composedName, price: unitPrice.value, qty: qty.value });
}
</script>

<template>
  <div class="modal-overlay" @click.self="emit('close')">
    <div class="modal-sheet" role="dialog" aria-modal="true" :aria-label="product.name">
      <div class="modal-header">
        <button ref="closeButtonEl" type="button" class="modal-close" aria-label="Close" @click="emit('close')">×</button>
        <span class="modal-header__title">{{ product.name }}</span>
        <span class="modal-header__spacer" />
      </div>

      <div class="modal-body">
        <p v-if="error" class="modal-status modal-status--error">{{ error }}</p>
        <p v-else-if="isLoading" class="modal-status">Loading options…</p>

        <template v-else>
          <h2 class="modal-title">{{ product.name }}</h2>
          <p class="modal-subtitle">{{ categoryLabel }}</p>

          <section v-if="milkGroup" class="option-group">
            <div class="option-group__header">
              <span class="option-group__name">{{ milkGroup.name }}</span>
              <span class="badge badge--required">Required</span>
            </div>
            <p class="option-group__hint">Select 1</p>
            <button
              v-for="option in milkGroup.options"
              :key="option.id"
              type="button"
              class="option-row"
              :disabled="!option.isAvailable"
              @click="selectMilk(option.id)"
            >
              <span class="option-row__label">
                <span class="option-row__name">
                  {{ option.name }}<template v-if="!option.isAvailable"> (unavailable)</template>
                </span>
                <span class="option-row__price">{{ formatDelta(option.priceDelta) }}</span>
              </span>
              <span
                class="option-indicator option-indicator--round"
                :class="{ 'option-indicator--selected': selectedMilkId === option.id }"
              />
            </button>
          </section>

          <section v-if="shotGroup" class="option-group">
            <div class="option-group__header">
              <span class="option-group__name">{{ shotGroup.name }} Add-On</span>
              <span class="badge badge--optional">Optional</span>
            </div>
            <p class="option-group__hint">Select up to 1</p>
            <button
              v-for="option in shotGroup.options"
              :key="option.id"
              type="button"
              class="option-row"
              :disabled="!option.isAvailable"
              @click="toggleShot(option.id)"
            >
              <span class="option-row__label">
                <span class="option-row__name">
                  {{ option.name }}<template v-if="!option.isAvailable"> (unavailable)</template>
                </span>
                <span class="option-row__price">{{ formatDelta(option.priceDelta) }}</span>
              </span>
              <span
                class="option-indicator option-indicator--square"
                :class="{ 'option-indicator--selected': selectedShotId === option.id }"
              />
            </button>
          </section>

          <section v-if="syrupGroup" class="option-group">
            <div class="option-group__header">
              <span class="option-group__name">{{ syrupGroup.name }} Add-Ons</span>
              <span class="badge badge--optional">Optional</span>
            </div>
            <p class="option-group__hint">Select up to {{ syrupGroup.maxSelections }}</p>
            <button
              v-for="option in syrupGroup.options"
              :key="option.id"
              type="button"
              class="option-row"
              :disabled="
                !option.isAvailable ||
                (!selectedSyrupIds.includes(option.id) && selectedSyrupIds.length >= syrupGroup.maxSelections)
              "
              @click="toggleSyrup(option.id)"
            >
              <span class="option-row__label">
                <span class="option-row__name">
                  {{ option.name }}<template v-if="!option.isAvailable"> (unavailable)</template>
                </span>
                <span class="option-row__price">{{ formatDelta(option.priceDelta) }}</span>
              </span>
              <span
                class="option-indicator option-indicator--square"
                :class="{ 'option-indicator--selected': selectedSyrupIds.includes(option.id) }"
              />
            </button>
          </section>
        </template>
      </div>

      <div class="modal-footer">
        <div class="qty-stepper">
          <button type="button" aria-label="Decrease quantity" @click="decQty">−</button>
          <span>{{ qty }}</span>
          <button type="button" aria-label="Increase quantity" @click="incQty">+</button>
        </div>
        <button type="button" class="modal-confirm" :disabled="confirmDisabled" @click="confirm">
          Add · {{ lineTotal }} EGP
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: 100;
  display: flex;
  align-items: flex-end;
  justify-content: center;
}

.modal-sheet {
  background: white;
  width: 100%;
  max-width: 520px;
  max-height: 88vh;
  border-radius: 20px 20px 0 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  border-bottom: 1px solid var(--color-border-soft);
  flex-shrink: 0;
}

.modal-close {
  background: none;
  border: none;
  font-size: 24px;
  line-height: 1;
  cursor: pointer;
  color: var(--color-ink-fainter);
}

.modal-header__title {
  font-weight: 700;
  font-size: 15px;
}

.modal-header__spacer {
  width: 24px;
}

.modal-body {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.modal-status {
  color: var(--color-ink-faint);
}

.modal-status--error {
  color: var(--color-accent);
}

.modal-title {
  margin: 0 0 4px;
  font-size: 22px;
}

.modal-subtitle {
  margin: 0 0 20px;
  color: var(--color-ink-fainter);
  font-size: 14px;
}

.option-group {
  border-radius: var(--radius-sm);
  background: var(--color-bg-tint);
  padding: 16px 18px;
  margin-bottom: 16px;
}

.option-group__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.option-group__name {
  font-weight: 700;
  font-size: 16px;
}

.option-group__hint {
  margin: 2px 0 8px;
  color: oklch(50% 0.01 50);
  font-size: 13px;
}

.badge {
  font-size: 12px;
  font-weight: 600;
  padding: 4px 10px;
  border-radius: var(--radius-pill);
}

.badge--required {
  background: oklch(90% 0.05 155);
  color: oklch(28% 0.05 155);
}

.badge--optional {
  background: oklch(92% 0.005 75);
  color: var(--color-ink-fainter);
}

.option-row {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 0;
  border-top: 1px solid oklch(92% 0.01 70);
  background: none;
  border-left: none;
  border-right: none;
  border-bottom: none;
  cursor: pointer;
  text-align: left;
  font: inherit;
  color: inherit;
}

.option-row:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.option-row__label {
  display: flex;
  flex-direction: column;
}

.option-row__name {
  font-weight: 600;
  font-size: 15px;
}

.option-row__price {
  color: oklch(50% 0.01 50);
  font-size: 13px;
}

.option-indicator {
  width: 20px;
  height: 20px;
  border: 2px solid oklch(80% 0.01 70);
  background: transparent;
  flex-shrink: 0;
}

.option-indicator--round {
  border-radius: 50%;
}

.option-indicator--square {
  border-radius: 6px;
}

.option-indicator--selected {
  border-color: var(--color-accent);
  background: var(--color-accent);
}

.modal-footer {
  flex-shrink: 0;
  border-top: 1px solid var(--color-border-soft);
  padding: 14px 20px;
  display: flex;
  gap: 12px;
  align-items: center;
}

.qty-stepper {
  display: flex;
  align-items: center;
  gap: 10px;
  border: 1px solid var(--color-border-idle);
  border-radius: var(--radius-pill);
  padding: 8px 14px;
}

.qty-stepper button {
  background: none;
  border: none;
  font-size: 18px;
  font-weight: 700;
  cursor: pointer;
}

.qty-stepper span {
  min-width: 16px;
  text-align: center;
  font-weight: 600;
}

.modal-confirm {
  flex: 1;
  background: var(--color-accent);
  color: white;
  border: none;
  border-radius: var(--radius-pill);
  padding: 14px;
  font-weight: 700;
  font-size: 15px;
  cursor: pointer;
}

.modal-confirm:disabled {
  background: var(--color-border-idle);
  cursor: not-allowed;
}
</style>
