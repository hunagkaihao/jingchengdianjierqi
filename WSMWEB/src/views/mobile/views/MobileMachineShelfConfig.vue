<template>
  <div class="shelf-page">
    <Header numb="机台货架修改" />

    <main class="content">
      <div class="toolbar">
        <a-input
          v-model:value="search"
          allow-clear
          aria-label="搜索货架库位"
          placeholder="搜索库区、货架名称或库位"
        />
        <div class="toolbar-actions">
          <a-button :loading="loading" :disabled="saving || deletingId !== null" @click="loadCells">
            刷新
          </a-button>
          <a-button
            v-if="hasPermission('Wms.Add')"
            type="primary"
            :disabled="rowActionsDisabled"
            @click="addCell"
          >
            添加新机台货架
          </a-button>
        </div>
      </div>

      <a-alert v-if="loadError" class="load-error" type="error" show-icon :message="loadError" />
      <a-table
        :columns="columns"
        :data-source="visibleCells"
        :loading="loading"
        :pagination="{ current: currentPage, pageSize: 15, hideOnSinglePage: true, showSizeChanger: false, onChange: changePage }"
        :scroll="{ x: 720 }"
        :locale="{ emptyText: search ? '没有匹配的库位' : '暂无机台货架库位' }"
        row-key="id"
        size="small"
      >
        <template #area="{ text }">{{ text ?? '—' }}</template>
        <template #shelf="{ text }">{{ text || '—' }}</template>
        <template #actions="{ record }">
          <a-button type="link" size="small" :disabled="rowActionsDisabled" @click="editCell(record)">
            编辑
          </a-button>
          <a-popconfirm
            v-if="hasPermission('Wms.Delete')"
            :title="'确认删除 ' + (record.shelfName || '未命名货架') + ' 的库位 ' + record.cellCode + '？'"
            :disabled="rowActionsDisabled"
            ok-text="删除"
            ok-type="danger"
            cancel-text="取消"
            @confirm="deleteCell(record)"
          >
            <a-button
              type="link"
              size="small"
              danger
              :disabled="rowActionsDisabled"
              :loading="deletingId === record.id"
            >
              删除
            </a-button>
          </a-popconfirm>
        </template>
      </a-table>

      <a-modal
        :visible="editing !== null"
        :confirm-loading="saving"
        :mask-closable="false"
        :title="isAdding ? '添加新机台货架' : '编辑货架库位'"
        ok-text="保存"
        cancel-text="取消"
        @ok="saveCell"
        @cancel="cancelEdit"
      >
        <a-form v-if="editing" layout="vertical">
          <a-form-item label="库区编号" html-for="shelf-area">
            <a-input-number id="shelf-area" v-model:value="editing.warehouseAreaId" :min="1" :precision="0" />
          </a-form-item>
          <a-form-item label="货架名称" html-for="shelf-name">
            <a-input id="shelf-name" v-model:value="editing.shelfName" :maxlength="30" />
          </a-form-item>
          <a-form-item label="库位编码" html-for="shelf-code" required>
            <a-input id="shelf-code" v-model:value="editing.cellCode" :maxlength="20" />
          </a-form-item>
          <a-form-item label="库位名称" html-for="cell-name" required>
            <a-input id="cell-name" v-model:value="editing.cellName" :maxlength="50" />
          </a-form-item>
        </a-form>
      </a-modal>
    </main>
  </div>
</template>

<script lang="ts" setup>
import { computed, onMounted, ref, watch } from 'vue';
import { message } from 'ant-design-vue';
import Header from '../header/Header.vue';
import { usePermission } from '/@/hooks/web/usePermission';
import { defHttp } from '/@/utils/http/axios';

/** PDA 表格中的真实库位记录，同编码记录以 ID 区分。 */
interface ShelfCell {
  /** 库位记录 ID。 */
  id: string;
  /** 库区编号，未分区时为空。 */
  warehouseAreaId: number | null;
  /** 数据库中的货架名称，可为空。 */
  shelfName: string | null;
  /** AGV 和传感器使用的库位编码。 */
  cellCode: string;
  /** 库位的显示名称。 */
  cellName: string;
  /** 保存或删除时用于校验并发修改的版本。 */
  revision: string;
}

/** 新增时使用未分配记录 ID 和版本的表单字段。 */
type ShelfCellForm = ShelfCell | Omit<ShelfCell, 'id' | 'revision'>;

const columns = [
  { title: '库区编号', dataIndex: 'warehouseAreaId', width: 96, slots: { customRender: 'area' } },
  { title: '货架名称', dataIndex: 'shelfName', width: 148, slots: { customRender: 'shelf' } },
  { title: '库位编码', dataIndex: 'cellCode', width: 170 },
  { title: '库位名称', dataIndex: 'cellName', width: 170 },
  { title: '操作', key: 'actions', width: 136, fixed: 'right' as const, slots: { customRender: 'actions' } },
];

const requestOptions = {
  isTransformResponse: false,
  errorMessageMode: 'none',
  authenticationScheme: 'Bearer',
} as const;
const url = '/wms/machine-shelves/cells';
const { hasPermission } = usePermission();
const cells = ref<ShelfCell[]>([]);
const editing = ref<ShelfCellForm | null>(null);
const isAdding = computed(() => editing.value !== null && !('id' in editing.value));
const search = ref('');
/** 当前表格页码，新增成功后回到第一页以显示新记录。 */
const currentPage = ref(1);
const loadError = ref('');
const loading = ref(false);
const saving = ref(false);
/** 正在删除的记录 ID，用于避免同一操作重复提交。 */
const deletingId = ref<string | null>(null);
const rowActionsDisabled = computed(() => loading.value || saving.value || deletingId.value !== null);
const visibleCells = computed(() => {
  const keyword = search.value.trim().toLowerCase();
  if (!keyword) return cells.value;
  return cells.value.filter((cell) =>
    [cell.warehouseAreaId, cell.shelfName, cell.cellCode, cell.cellName].some(
      (value) => String(value ?? '').toLowerCase().includes(keyword),
    ),
  );
});

/** 同步表格分页，搜索条件变化时从第一页查看结果。 */
function changePage(page: number) {
  currentPage.value = page;
}

watch(search, () => changePage(1));

/** 读取实际库位字段并显示查询失败原因。 */
async function loadCells() {
  loading.value = true;
  loadError.value = '';
  try {
    cells.value = await defHttp.get<ShelfCell[]>({ url }, requestOptions);
  } catch (error) {
    loadError.value = error?.response?.data?.error?.message || '库位列表读取失败，请刷新重试。';
  } finally {
    loading.value = false;
  }
}

/** 打开空白表单，录入新机台货架库位。 */
function addCell() {
  editing.value = { warehouseAreaId: null, shelfName: null, cellCode: '', cellName: '' };
}

/** 在表单中编辑一份副本，保存前保留原表格行。 */
function editCell(cell: ShelfCell) {
  editing.value = { ...cell };
}

/** 关闭编辑表单，放弃尚未提交的修改。 */
function cancelEdit() {
  if (!saving.value) editing.value = null;
}

/** 提交新增或编辑请求，使用服务器返回的记录更新表格。 */
async function saveCell() {
  if (!editing.value || saving.value) return;
  const form = editing.value;
  const { warehouseAreaId, shelfName, cellCode, cellName } = form;
  if (!cellCode.trim() || !cellName.trim()) {
    message.error('请输入库位编码和库位名称');
    return;
  }

  saving.value = true;
  try {
    const data = {
      warehouseAreaId: warehouseAreaId ?? null,
      shelfName: shelfName?.trim() || null,
      cellCode: cellCode.trim(),
      cellName: cellName.trim(),
    };
    if ('id' in form) {
      const saved = await defHttp.put<ShelfCell>(
        { url: url + '/' + form.id, data: { ...data, revision: form.revision } },
        requestOptions,
      );
      cells.value = cells.value.map((cell) => (cell.id === form.id ? saved : cell));
    } else {
      const saved = await defHttp.post<ShelfCell>({ url, data }, requestOptions);
      cells.value = [saved, ...cells.value];
      if (!visibleCells.value.some((cell) => cell.id === saved.id)) search.value = '';
      changePage(1);
    }
    editing.value = null;
    message.success('id' in form ? '库位已保存' : '机台货架已添加');
  } catch (error) {
    message.error(error?.response?.data?.error?.message || '保存失败，请重试。');
  } finally {
    saving.value = false;
  }
}

/** 确认后按记录 ID 删除，成功时只移除表格中的对应行。 */
async function deleteCell(cell: ShelfCell) {
  if (rowActionsDisabled.value) return;
  deletingId.value = cell.id;
  try {
    await defHttp.delete({
      url: url + '/' + cell.id + '?revision=' + encodeURIComponent(cell.revision),
    }, requestOptions);
    cells.value = cells.value.filter((row) => row.id !== cell.id);
    message.success('库位已删除');
  } catch (error) {
    message.error(error?.response?.data?.error?.message || '删除失败，请重试。');
  } finally {
    deletingId.value = null;
  }
}

onMounted(loadCells);
</script>

<style scoped lang="less">
.shelf-page {
  min-height: 100vh;
  background: #f5f7fa;
}

.content {
  max-width: 1120px;
  margin: 0 auto;
  padding: 12px 14px 24px;
}

.toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  margin-bottom: 12px;

  .ant-input-affix-wrapper {
    flex: 1 1 140px;
    max-width: 320px;
  }
}

.toolbar-actions {
  display: flex;
  gap: 12px;
}

.load-error {
  margin-bottom: 12px;
}
</style>
