<template>
  <div class="machine-status-page">
    <a-alert v-if="dataSource.length === 0 && !loading" type="warning" show-icon
      message="暂无机台状态数据" description="请确认后端机台状态监控服务已启动，且机台配置已启用。" />

    <!-- 汇总信息 -->
    <div class="summary-bar">
      <span>共 <b>{{ dataSource.length }}</b> 个门站点</span>
      <span class="summary-item online">在线: <b>{{ summary.online }}</b></span>
      <span class="summary-item offline">断线: <b>{{ summary.offline }}</b></span>
      <span class="summary-item full">已触发上料: <b>{{ summary.full }}</b></span>
      <span class="refresh-info" v-if="lastRefreshTime">最近刷新: {{ lastRefreshTime }}</span>
      <a-button size="small" type="primary" :loading="loading" @click="loadStatuses">刷新</a-button>
    </div>

    <!-- 机台卡片列表 -->
    <a-row :gutter="16" class="machine-row">
      <a-col v-for="machine in machineGroups" :key="machine.machineName" :xs="24" :sm="12" :lg="8" :xl="6">
        <a-card :title="machine.machineName" size="small" class="machine-card">
          <div v-for="door in machine.doors" :key="door.doorNumber" class="door-item"
            :class="{ 'is-offline': !door.online, 'is-full': door.online && door.fullMaterial }">
            <div class="door-title">
              <span class="door-name">{{ door.doorName }}</span>
              <div class="door-tags">
                <a-tag v-if="!door.online" color="error">断线</a-tag>
                <a-tag v-else-if="door.fullMaterial" color="success">已触发上料</a-tag>
                <a-tag v-else color="warning">未触发上料</a-tag>
                <a-tag :color="taskStatusColor(door.taskStatus)">{{ door.taskStatusText || '无任务' }}</a-tag>
              </div>
            </div>
            <div class="door-info">
              <span>库位: {{ door.cellCode || '-' }}</span>
              <span>任务类型: {{ taskTypeText(door.taskType) }}</span>
              <span>任务状态: {{ door.taskStatusText || '无任务' }}</span>
              <span>信号地址: {{ door.fullMaterialAddress }}</span>
              <span>更新时间: {{ door.lastUpdateTime || '-' }}</span>
            </div>
          </div>
        </a-card>
      </a-col>
    </a-row>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { defHttp } from '/@/utils/http/axios';
import { message } from 'ant-design-vue';

interface MachineStatusItem {
  machineName: string;
  doorNumber: number;
  doorName: string;
  cellCode: string;
  taskType: string;
  fullMaterialAddress: number;
  online: boolean;
  fullMaterial: boolean;
  lastUpdateTime: string;
  taskId?: number | null;
  taskStatus?: number | null;
  taskStatusText?: string;
}

const loading = ref(false);
const dataSource = ref<MachineStatusItem[]>([]);
const lastRefreshTime = ref('');
let refreshTimer: ReturnType<typeof setInterval> | null = null;

const summary = computed(() => {
  const list = dataSource.value;
  return {
    online: list.filter((i) => i.online).length,
    offline: list.filter((i) => !i.online).length,
    full: list.filter((i) => i.online && i.fullMaterial).length,
  };
});

// 按机台分组
const machineGroups = computed(() => {
  const map = new Map<string, MachineStatusItem[]>();
  for (const item of dataSource.value) {
    if (!map.has(item.machineName)) {
      map.set(item.machineName, []);
    }
    map.get(item.machineName)!.push(item);
  }
  return [...map.entries()].map(([machineName, doors]) => ({
    machineName,
    doors: doors.sort((a, b) => a.doorNumber - b.doorNumber),
  }));
});

const taskTypeMap: Record<string, string> = {
  EmptyBoxToMachine: '空盒衬上机台',
  FinishedFromMachine: '成品下机台',
  EmptyBoxFromMachine: '空盒衬下机台',
  SemiFinishedFromMachine: '半成品上料',
};

function taskTypeText(type?: string) {
  if (!type) return '-';
  return taskTypeMap[type] || type;
}

// 任务状态标签配色：0/1/5/6/8 等待类橙色，2/3/4/7 执行类蓝色，11/12 异常红色，无任务灰色
function taskStatusColor(status?: number | null) {
  if (status == null) return 'default';
  if (status === 11 || status === 12) return 'error';
  if ([0, 1, 5, 6, 8].includes(status)) return 'warning';
  return 'processing';
}

async function loadStatuses() {
  loading.value = true;
  try {
    // 后端返回原生 JSON 数组（非 {code, result} 包装），跳过统一解包
    const res = await defHttp.get<MachineStatusItem[]>(
      { url: '/wms/machine-status/statuses' },
      { isTransformResponse: false },
    );
    dataSource.value = res || [];
    lastRefreshTime.value = new Date().toLocaleString('zh-CN');
  } catch (error) {
    console.error('获取机台状态失败:', error);
    message.error('获取机台状态失败');
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  loadStatuses();
  // 10 秒自动刷新（与后台监控循环 10 秒同步）
  refreshTimer = setInterval(loadStatuses, 10000);
});

onUnmounted(() => {
  if (refreshTimer) {
    clearInterval(refreshTimer);
    refreshTimer = null;
  }
});
</script>

<style scoped lang="less">
.machine-status-page {
  padding: 12px;
}

.summary-bar {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 12px;
  padding: 10px 14px;
  background: #fff;
  border-radius: 6px;
  font-size: 14px;

  .summary-item b {
    font-size: 16px;
  }

  .online b {
    color: #52c41a;
  }

  .offline b {
    color: #ff4d4f;
  }

  .full b {
    color: #1890ff;
  }

  .refresh-info {
    margin-left: auto;
    color: #999;
    font-size: 12px;
  }
}

.machine-card {
  margin-bottom: 16px;
}

.door-item {
  border: 1px solid #f0f0f0;
  border-radius: 6px;
  padding: 10px;
  margin-bottom: 10px;
  background: #fafafa;
  transition: all 0.3s ease;

  &.is-offline {
    border-color: #ffccc7;
    background: #fff2f0;
  }

  &.is-full {
    border-color: #b7eb8f;
    background: #f6ffed;
  }
}

.door-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;

  .door-name {
    font-weight: 600;
    font-size: 14px;
  }

  .door-tags {
    display: flex;
    align-items: center;
    gap: 4px;

    :deep(.ant-tag) {
      margin-right: 0;
    }
  }
}

.door-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 12px;
  color: #666;
}
</style>
