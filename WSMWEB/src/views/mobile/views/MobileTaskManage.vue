<template>
  <div class="mobile-task-manage">
    <Header numb="任务管理"></Header>

    <!-- 查询表单 -->
    <div class="query-panel">
      <div class="query-row">
        <span class="query-label">任务编号</span>
        <a-input
          v-model:value="queryForm.taskId"
          placeholder="输入任务编号"
          allowClear
          @keyup.enter="handleSearch"
        />
      </div>
      <div class="query-row">
        <span class="query-label">任务状态</span>
        <a-select
          v-model:value="queryForm.status"
          :options="statusOptions"
          placeholder="选择任务状态"
          allowClear
        />
      </div>
      <div class="query-actions">
        <a-button class="query-btn" @click="handleReset">重置</a-button>
        <a-button type="primary" class="query-btn" :loading="loading" @click="handleSearch">
          查询
        </a-button>
      </div>
    </div>

    <!-- 任务表格（默认显示最近 7 条，分页） -->
    <div class="table-wrapper">
      <a-table
        :columns="columns"
        :data-source="taskList"
        :row-key="(r: any) => r.id"
        :row-selection="{ type: 'radio', selectedRowKeys: selectedRowKeys, onChange: onSelectionChange }"
        :loading="loading"
        :pagination="paginationConfig"
        :scroll="{ x: 380 }"
        size="small"
        @change="handleTableChange"
      >
      </a-table>
    </div>

    <!-- 底部操作栏 -->
    <div class="tab-bar">
      <a-button
        type="primary"
        class="modern-btn complete-btn"
        :disabled="selectedCount === 0"
        :loading="submitting"
        @click="completeTask"
      >完成任务</a-button>
      <a-button
        danger
        class="modern-btn cancel-btn"
        :disabled="selectedCount === 0"
        :loading="submitting"
        @click="cancelTask"
      >取消任务</a-button>
    </div>
  </div>
</template>
<script lang="ts" setup>
import { ref, computed, onMounted, h } from 'vue';
import { message, Tag } from 'ant-design-vue';
import moment from 'moment';
import Header from '../header/Header.vue';
import { AgvTaskServiceProxy, PagingAgvTaskListInput } from '/@/services/ServiceProxies';

const agvTasksService = new AgvTaskServiceProxy();

interface TaskItem {
  id: number;
  startPosition?: string;
  targetPosition?: string;
  boxCode?: string;
  taskType?: string;
  taskStatus: number;
  createTime?: string;
  stockTyp?: number;
}

const taskList = ref<TaskItem[]>([]);
const selectedRowKeys = ref<(number | string)[]>([]);
const loading = ref(false);
const submitting = ref(false);
const total = ref(0);
const pageIndex = ref(1);
const pageSize = ref(10);

// 查询表单（时间固定为最近 7 天，不提供筛选）
const queryForm = ref<{
  taskId: string | undefined;
  status: string | undefined;
}>({
  taskId: undefined,
  status: undefined,
});

const statusOptions = [
  { label: '等待执行', value: '1' },
  { label: '执行中', value: '2' },
  { label: '任务开始', value: '3' },
  { label: '出储位', value: '4' },
  { label: '任务完成', value: '9' },
  { label: '调度删除任务', value: '10' },
];

const statusMap: Record<number, string> = {
  0: '被创建',
  1: '等待执行',
  2: '执行中',
  3: '任务开始',
  4: '出储位',
  5: '等待任务继续',
  6: '等待继续任务响应',
  7: '继续执行',
  8: '等待取消响应',
  9: '已完成',
  10: '已取消',
};

// 表格列定义（字段精简）
const columns = [
  { title: '任务编号', dataIndex: 'id', width: 80 },
  {
    title: '开始位置',
    dataIndex: 'startPosition',
    width: 100,
    customRender: ({ text }: any) => text || '-',
  },
  {
    title: '结束位置',
    dataIndex: 'targetPosition',
    width: 100,
    customRender: ({ text }: any) => text || '-',
  },
  {
    title: '任务状态',
    dataIndex: 'taskStatus',
    width: 96,
    customRender: ({ record }: any) => {
      const t = record as TaskItem;
      return h(Tag, { color: statusColor(t.taskStatus) }, { default: () => statusText(t.taskStatus) });
    },
  },
];

// 分页配置（由 a-table 内嵌渲染）
const paginationConfig = computed(() => ({
  current: pageIndex.value,
  pageSize: pageSize.value,
  total: total.value,
  showSizeChanger: true,
  pageSizeOptions: ['10', '20', '50'],
  showTotal: (t: number) => `共 ${t} 条`,
}));

const selectedCount = computed(() => selectedRowKeys.value.length);

function onSelectionChange(keys: (number | string)[]) {
  selectedRowKeys.value = keys;
}

// 解析后端返回的错误信息（与 web 端一致）
function getApiErrorMessage(error: any, fallback: string) {
  try {
    // 代理层 ApiException 的 response 可能是原始 JSON 字符串或已解析对象（axios 自动 parse）
    const payload = typeof error?.response === 'string' ? JSON.parse(error.response) : error?.response;
    const msg =
      payload?.error?.message
      || error?.error?.message
      || error?.response?.data?.error?.message;
    if (msg) return msg;
  } catch {
    // 解析失败走兜底
  }
  // 屏蔽代理层固定的英文文案，避免误导
  if (error?.message && error.message !== 'An unexpected server error occurred.') {
    return error.message;
  }
  return fallback;
}

async function loadTasks() {
  loading.value = true;
  try {
    // 按中国时区（UTC+8）计算查询时间；代理序列化会调用 toISOString()（输出 UTC），
    // 因此用 moment.utc 模式让中国本地时间原样输出，后端不会再做时区转换
    const chinaNow = moment.utc().add(8, 'hours');
    // 开始时间：7 天前的 00:00；结束时间：今天晚上 24:00（与 web 端日期选择"当天+1天"口径一致）
    const chinaStart = chinaNow.clone().subtract(7, 'days').startOf('day');
    const chinaEnd = chinaNow.clone().startOf('day').add(1, 'day');
    const input = new PagingAgvTaskListInput({
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
      skipCount: (pageIndex.value - 1) * pageSize.value,
      filter: queryForm.value.taskId && queryForm.value.taskId.length > 0 ? queryForm.value.taskId : undefined,
      // 固定只查最近 7 天的任务（截至今天 24:00，中国时区）
      startCreationTime: chinaStart as any,
      endCreationTime: chinaEnd as any,
      agvTaskStatus: queryForm.value.status,
    });
    const result = await agvTasksService.page(input);
    const res: any = result as any;
    const items: any[] = res?.items || [];
    total.value = res?.total || items.length;
    taskList.value = items.map((it: any) => ({
      id: it.id,
      startPosition: it.startPosition,
      targetPosition: it.targetPosition,
      boxCode: it.boxCode,
      taskType: it.taskType,
      taskStatus: it.taskStatus,
      createTime: it.createTime,
      stockTyp: it.stockTyp,
    }));
    // 翻页/查询后重置选中
    selectedRowKeys.value = [];
  } catch (error) {
    message.error('获取任务列表失败');
    taskList.value = [];
    total.value = 0;
  } finally {
    loading.value = false;
  }
}

function handleSearch() {
  pageIndex.value = 1;
  loadTasks();
}

function handleReset() {
  queryForm.value = { taskId: undefined, status: undefined };
  pageIndex.value = 1;
  loadTasks();
}

// 表格分页变化（含页码与每页条数）
function handleTableChange(pag: any) {
  pageIndex.value = pag.current;
  pageSize.value = pag.pageSize;
  loadTasks();
}

function statusText(status: number) {
  return statusMap[status] || String(status);
}

function statusColor(status: number) {
  if (status === 9) return 'success';
  if (status === 10) return 'default';
  return 'processing';
}

// 完成任务（与 web 端一致：单选任务，直接操作，错误由后端返回）
async function completeTask() {
  if (selectedRowKeys.value.length === 0) {
    message.warning('请选择要完成的任务');
    return;
  }
  submitting.value = true;
  try {
    await agvTasksService.setAsCompleted(Number(selectedRowKeys.value[0]));
    message.success('完成任务成功');
    await loadTasks();
  } catch (error: any) {
    message.error(getApiErrorMessage(error, '完成任务失败'));
  } finally {
    submitting.value = false;
  }
}

// 取消任务（与 web 端一致：isSync 设为 false）
async function cancelTask() {
  if (selectedRowKeys.value.length === 0) {
    message.warning('请选择要取消的任务');
    return;
  }
  submitting.value = true;
  try {
    await agvTasksService.setAsCancel(Number(selectedRowKeys.value[0]), false);
    message.success('取消任务成功');
    await loadTasks();
  } catch (error: any) {
    message.error(getApiErrorMessage(error, '取消任务失败'));
  } finally {
    submitting.value = false;
  }
}

onMounted(() => {
  loadTasks();
});
</script>

<style scoped lang="less">
.mobile-task-manage {
  min-height: 100vh;
  background: #f5f6fa;
  padding-bottom: 76px; // 为底部操作栏留空间
}

/* 查询面板 */
.query-panel {
  background: #ffffff;
  border-bottom: 1px solid #f0f0f0;
  padding: 12px;
}

.query-row {
  display: flex;
  align-items: center;
  margin-bottom: 10px;
}

.query-label {
  width: 64px;
  font-size: 14px;
  color: #333;
  flex-shrink: 0;
}

.query-row :deep(.ant-input),
.query-row :deep(.ant-select),
.query-row :deep(.ant-picker) {
  flex: 1;
}

.query-actions {
  display: flex;
  gap: 12px;
  margin-top: 4px;
}

.query-btn {
  flex: 1;
  height: 36px !important;
  border-radius: 6px !important;
  font-size: 15px !important;
}

/* 任务表格 */
.table-wrapper {
  padding: 12px;
}

.table-wrapper :deep(.ant-table) {
  border-radius: 8px;
  font-size: 13px;
}

/* 底部操作栏 */
.tab-bar {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  height: 60px;
  padding: 10px 16px;
  display: flex;
  gap: 12px;
  background: #ffffff;
  border-top: 1px solid #f0f0f0;
  box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.04);
  z-index: 1000;
}

.modern-btn {
  flex: 1;
  height: 40px !important;
  border-radius: 8px !important;
  font-size: 16px !important;
  font-weight: 600 !important;
}

.complete-btn {
  background: #1890ff !important;
  border: none !important;
}

.cancel-btn {
  background: #ffffff !important;
}
</style>
