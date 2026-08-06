<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import {
  createCategory,
  createProduct,
  getAdminCategories,
  getAdminModifierGroups,
  getAdminProducts,
  updateCategory,
  updateModifierOption,
  updateProduct,
  uploadProductImage,
} from '../api/adminCatalog';
import { ApiError } from '../lib/adminApi';
import type { AdminCategory, AdminModifierGroup, AdminProduct } from '../types/adminCatalog';

type Tab = 'categories' | 'products' | 'modifiers';

const tab = ref<Tab>('categories');
const loading = ref(true);
const errorMessage = ref('');

const categories = ref<AdminCategory[]>([]);
const products = ref<AdminProduct[]>([]);
const modifierGroups = ref<AdminModifierGroup[]>([]);

async function loadAll() {
  loading.value = true;
  errorMessage.value = '';
  try {
    [categories.value, products.value, modifierGroups.value] = await Promise.all([
      getAdminCategories(),
      getAdminProducts(),
      getAdminModifierGroups(),
    ]);
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to load the menu.';
  } finally {
    loading.value = false;
  }
}

onMounted(loadAll);

// --- Categories ---

const newCategory = ref({ slug: '', label: '', description: '', displayOrder: 0 });
const editingCategoryId = ref<string | null>(null);
const categoryDraft = ref({ label: '', description: '', displayOrder: 0, isActive: true });
const savingCategory = ref(false);

function startEditCategory(category: AdminCategory) {
  editingCategoryId.value = category.id;
  categoryDraft.value = {
    label: category.label,
    description: category.description ?? '',
    displayOrder: category.displayOrder,
    isActive: category.isActive,
  };
}

async function saveCategory(categoryId: string) {
  savingCategory.value = true;
  errorMessage.value = '';
  try {
    const updated = await updateCategory(categoryId, {
      label: categoryDraft.value.label,
      description: categoryDraft.value.description || null,
      displayOrder: Number(categoryDraft.value.displayOrder),
      isActive: categoryDraft.value.isActive,
    });
    const index = categories.value.findIndex((c) => c.id === categoryId);
    if (index !== -1) categories.value[index] = updated;
    editingCategoryId.value = null;
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to save category.';
  } finally {
    savingCategory.value = false;
  }
}

async function submitNewCategory() {
  errorMessage.value = '';
  try {
    const created = await createCategory({
      slug: newCategory.value.slug.trim(),
      label: newCategory.value.label.trim(),
      description: newCategory.value.description.trim() || null,
      displayOrder: Number(newCategory.value.displayOrder),
    });
    categories.value.push(created);
    newCategory.value = { slug: '', label: '', description: '', displayOrder: 0 };
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to create category.';
  }
}

// --- Products ---

const productSearch = ref('');
const productCategoryFilter = ref('');
const filteredProducts = computed(() =>
  products.value.filter((p) => {
    const matchesSearch = p.name.toLowerCase().includes(productSearch.value.trim().toLowerCase());
    const matchesCategory = !productCategoryFilter.value || p.categorySlug === productCategoryFilter.value;
    return matchesSearch && matchesCategory;
  }),
);

const newProduct = ref({ categorySlug: '', slug: '', name: '', description: '', price: 0, displayOrder: 0 });
const newProductImageUrl = ref<string | null>(null);
const uploadingNewProductImage = ref(false);

const editingProductId = ref<string | null>(null);
const productDraft = ref({
  name: '', description: '', price: 0, imageUrl: null as string | null, displayOrder: 0, isAvailable: true, isActive: true,
});
const savingProduct = ref(false);
const uploadingEditImage = ref(false);

function startEditProduct(product: AdminProduct) {
  editingProductId.value = product.id;
  productDraft.value = {
    name: product.name,
    description: product.description ?? '',
    price: product.price,
    imageUrl: product.imageUrl,
    displayOrder: product.displayOrder,
    isAvailable: product.isAvailable,
    isActive: product.isActive,
  };
}

async function saveProduct(productId: string) {
  savingProduct.value = true;
  errorMessage.value = '';
  try {
    const updated = await updateProduct(productId, {
      name: productDraft.value.name,
      description: productDraft.value.description || null,
      price: Number(productDraft.value.price),
      imageUrl: productDraft.value.imageUrl,
      displayOrder: Number(productDraft.value.displayOrder),
      isAvailable: productDraft.value.isAvailable,
      isActive: productDraft.value.isActive,
    });
    const index = products.value.findIndex((p) => p.id === productId);
    if (index !== -1) products.value[index] = updated;
    editingProductId.value = null;
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to save product.';
  } finally {
    savingProduct.value = false;
  }
}

async function submitNewProduct() {
  errorMessage.value = '';
  try {
    const created = await createProduct({
      categorySlug: newProduct.value.categorySlug,
      slug: newProduct.value.slug.trim(),
      name: newProduct.value.name.trim(),
      description: newProduct.value.description.trim() || null,
      price: Number(newProduct.value.price),
      displayOrder: Number(newProduct.value.displayOrder),
      imageUrl: newProductImageUrl.value,
    });
    products.value.push(created);
    newProduct.value = { categorySlug: '', slug: '', name: '', description: '', price: 0, displayOrder: 0 };
    newProductImageUrl.value = null;
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to create product.';
  }
}

async function onUploadNewProductImage(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (!file) return;
  uploadingNewProductImage.value = true;
  errorMessage.value = '';
  try {
    newProductImageUrl.value = await uploadProductImage(file);
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Image upload failed.';
  } finally {
    uploadingNewProductImage.value = false;
  }
}

async function onUploadEditImage(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (!file) return;
  uploadingEditImage.value = true;
  errorMessage.value = '';
  try {
    productDraft.value.imageUrl = await uploadProductImage(file);
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Image upload failed.';
  } finally {
    uploadingEditImage.value = false;
  }
}

// --- Modifiers ---

const editingOptionId = ref<string | null>(null);
const optionDraft = ref({ priceDelta: 0, isActive: true });
const savingOption = ref(false);

function startEditOption(optionId: string, priceDelta: number, isActive: boolean) {
  editingOptionId.value = optionId;
  optionDraft.value = { priceDelta, isActive };
}

async function saveOption(groupId: string, optionId: string) {
  savingOption.value = true;
  errorMessage.value = '';
  try {
    await updateModifierOption(optionId, {
      priceDelta: Number(optionDraft.value.priceDelta),
      isActive: optionDraft.value.isActive,
    });
    const group = modifierGroups.value.find((g) => g.id === groupId);
    const option = group?.options.find((o) => o.id === optionId);
    if (option) {
      option.priceDelta = Number(optionDraft.value.priceDelta);
      option.isActive = optionDraft.value.isActive;
    }
    editingOptionId.value = null;
  } catch (err) {
    errorMessage.value = err instanceof ApiError ? err.message : 'Failed to save modifier option.';
  } finally {
    savingOption.value = false;
  }
}
</script>

<template>
  <div class="admin-menu">
    <h1>Menu management</h1>

    <div class="tabs">
      <button :class="['tab', { active: tab === 'categories' }]" @click="tab = 'categories'">Categories</button>
      <button :class="['tab', { active: tab === 'products' }]" @click="tab = 'products'">Products</button>
      <button :class="['tab', { active: tab === 'modifiers' }]" @click="tab = 'modifiers'">Modifiers</button>
    </div>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
    <p v-if="loading">Loading…</p>

    <section v-else-if="tab === 'categories'">
      <form class="inline-form" @submit.prevent="submitNewCategory">
        <input v-model="newCategory.slug" placeholder="slug" required />
        <input v-model="newCategory.label" placeholder="Label" required />
        <input v-model="newCategory.description" placeholder="Description (optional)" />
        <input v-model.number="newCategory.displayOrder" type="number" placeholder="Order" />
        <button class="btn btn--accent btn--sm" type="submit">Add category</button>
      </form>

      <table class="admin-table">
        <thead>
          <tr>
            <th>Slug</th><th>Label</th><th>Description</th><th>Order</th><th>Active</th><th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="category in categories" :key="category.id">
            <template v-if="editingCategoryId === category.id">
              <td>{{ category.slug }}</td>
              <td><input v-model="categoryDraft.label" /></td>
              <td><input v-model="categoryDraft.description" /></td>
              <td><input v-model.number="categoryDraft.displayOrder" type="number" style="width: 70px" /></td>
              <td><input v-model="categoryDraft.isActive" type="checkbox" /></td>
              <td>
                <button class="btn btn--accent btn--sm" :disabled="savingCategory" @click="saveCategory(category.id)">Save</button>
                <button class="btn btn--outline btn--sm" @click="editingCategoryId = null">Cancel</button>
              </td>
            </template>
            <template v-else>
              <td>{{ category.slug }}</td>
              <td>{{ category.label }}</td>
              <td>{{ category.description }}</td>
              <td>{{ category.displayOrder }}</td>
              <td>{{ category.isActive ? 'Yes' : 'No' }}</td>
              <td><button class="btn btn--outline btn--sm" @click="startEditCategory(category)">Edit</button></td>
            </template>
          </tr>
        </tbody>
      </table>
    </section>

    <section v-else-if="tab === 'products'">
      <details class="add-product">
        <summary>Add product</summary>
        <form class="inline-form" @submit.prevent="submitNewProduct">
          <select v-model="newProduct.categorySlug" required>
            <option value="" disabled>Category</option>
            <option v-for="c in categories" :key="c.id" :value="c.slug">{{ c.label }}</option>
          </select>
          <input v-model="newProduct.slug" placeholder="slug" required />
          <input v-model="newProduct.name" placeholder="Name" required />
          <input v-model="newProduct.description" placeholder="Description (optional)" />
          <input v-model.number="newProduct.price" type="number" step="0.01" placeholder="Price" required />
          <input v-model.number="newProduct.displayOrder" type="number" placeholder="Order" />
          <input type="file" accept="image/jpeg,image/png,image/webp" @change="onUploadNewProductImage" />
          <span v-if="uploadingNewProductImage">Uploading…</span>
          <img v-if="newProductImageUrl" :src="newProductImageUrl" alt="" class="thumb" />
          <button class="btn btn--accent btn--sm" type="submit">Add product</button>
        </form>
      </details>

      <div class="filters">
        <input v-model="productSearch" placeholder="Search products…" />
        <select v-model="productCategoryFilter">
          <option value="">All categories</option>
          <option v-for="c in categories" :key="c.id" :value="c.slug">{{ c.label }}</option>
        </select>
      </div>

      <table class="admin-table">
        <thead>
          <tr>
            <th>Image</th><th>Name</th><th>Category</th><th>Price</th><th>Order</th><th>Available</th><th>Active</th><th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="product in filteredProducts" :key="product.id">
            <template v-if="editingProductId === product.id">
              <td>
                <img v-if="productDraft.imageUrl" :src="productDraft.imageUrl" alt="" class="thumb" />
                <input type="file" accept="image/jpeg,image/png,image/webp" @change="onUploadEditImage" />
                <span v-if="uploadingEditImage">Uploading…</span>
              </td>
              <td><input v-model="productDraft.name" /></td>
              <td>{{ product.categorySlug }}</td>
              <td><input v-model.number="productDraft.price" type="number" step="0.01" style="width: 80px" /></td>
              <td><input v-model.number="productDraft.displayOrder" type="number" style="width: 70px" /></td>
              <td><input v-model="productDraft.isAvailable" type="checkbox" /></td>
              <td><input v-model="productDraft.isActive" type="checkbox" /></td>
              <td>
                <button class="btn btn--accent btn--sm" :disabled="savingProduct" @click="saveProduct(product.id)">Save</button>
                <button class="btn btn--outline btn--sm" @click="editingProductId = null">Cancel</button>
              </td>
            </template>
            <template v-else>
              <td><img v-if="product.imageUrl" :src="product.imageUrl" alt="" class="thumb" /></td>
              <td>{{ product.name }}</td>
              <td>{{ product.categorySlug }}</td>
              <td>{{ product.price }} EGP</td>
              <td>{{ product.displayOrder }}</td>
              <td>{{ product.isAvailable ? 'Yes' : 'No' }}</td>
              <td>{{ product.isActive ? 'Yes' : 'No' }}</td>
              <td><button class="btn btn--outline btn--sm" @click="startEditProduct(product)">Edit</button></td>
            </template>
          </tr>
        </tbody>
      </table>
    </section>

    <section v-else>
      <div v-for="group in modifierGroups" :key="group.id" class="modifier-group">
        <h2>{{ group.name }} <span class="tag-pill">{{ group.selectionType }}</span></h2>
        <table class="admin-table">
          <thead>
            <tr><th>Name</th><th>Price delta</th><th>Active</th><th></th></tr>
          </thead>
          <tbody>
            <tr v-for="option in group.options" :key="option.id">
              <template v-if="editingOptionId === option.id">
                <td>{{ option.name }}</td>
                <td><input v-model.number="optionDraft.priceDelta" type="number" step="0.01" style="width: 80px" /></td>
                <td><input v-model="optionDraft.isActive" type="checkbox" /></td>
                <td>
                  <button class="btn btn--accent btn--sm" :disabled="savingOption" @click="saveOption(group.id, option.id)">Save</button>
                  <button class="btn btn--outline btn--sm" @click="editingOptionId = null">Cancel</button>
                </td>
              </template>
              <template v-else>
                <td>{{ option.name }}</td>
                <td>{{ option.priceDelta }} EGP</td>
                <td>{{ option.isActive ? 'Yes' : 'No' }}</td>
                <td><button class="btn btn--outline btn--sm" @click="startEditOption(option.id, option.priceDelta, option.isActive)">Edit</button></td>
              </template>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<style scoped>
.admin-menu h1 {
  margin-top: 0;
}

.tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 20px;
  border-bottom: 1px solid var(--color-border-soft);
}

.tab {
  padding: 10px 16px;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  cursor: pointer;
  font-weight: 600;
  color: var(--color-ink-soft);
}

.tab.active {
  color: var(--color-accent);
  border-bottom-color: var(--color-accent);
}

.error {
  color: #b3261e;
}

.inline-form {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
  margin-bottom: 16px;
}

.inline-form input,
.inline-form select {
  padding: 8px 10px;
  border: 1px solid var(--color-border-idle);
  border-radius: var(--radius-sm);
  font-family: inherit;
}

.add-product {
  margin-bottom: 16px;
}

.filters {
  display: flex;
  gap: 8px;
  margin-bottom: 12px;
}

.filters input,
.filters select {
  padding: 8px 10px;
  border: 1px solid var(--color-border-idle);
  border-radius: var(--radius-sm);
}

.admin-table {
  width: 100%;
  border-collapse: collapse;
  background: var(--color-white);
}

.admin-table th,
.admin-table td {
  text-align: left;
  padding: 8px 10px;
  border-bottom: 1px solid var(--color-border-soft);
  font-size: 14px;
}

.admin-table input[type='text'],
.admin-table input:not([type='checkbox']):not([type='file']) {
  padding: 4px 6px;
  border: 1px solid var(--color-border-idle);
  border-radius: 6px;
  width: 100%;
}

.thumb {
  width: 40px;
  height: 40px;
  object-fit: cover;
  border-radius: 6px;
}

.modifier-group {
  margin-bottom: 24px;
}

.modifier-group h2 {
  font-size: 18px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.modifier-group .tag-pill {
  background: var(--color-accent-surface);
  color: var(--color-accent);
  font-size: 11px;
}
</style>
