<template>
  <div class="mobile-machine-status">
    <Header numb="机台状态"></Header>

    <!-- 汇总 -->
    <div class="summary-bar">
      <span>门站点: <b>{{ statusList.length }}</b></span>
      <span class="online">在线: <b>{{ summary.online }}</b></span>
      <span class="offline">断线: <b>{{ summary.offline }}</b></span>
      <span class="full">已触发: <b>{{ summary.full }}</b></span>
      <a-button size="small" type="primary" :loading="loading" @click="loadStatuses">刷新</a-button>
    </div>

    <div v-if="loading" class="loading-wrapper">
      <a-spin size="large" />
      <span class="loading-text">加载中...</span>
    </div>

    <div v-else-if="statusList.length === 0" class="empty-wrapper">
      <a-empty description="暂无机台状态数据" />
    </div>

    <!-- 机台分组卡片 -->
    <div v-else class="machine-list">
      <a-card v-for="machine in machineGroups" :key="machine.machineName"
        :title="machine.machineName" size="small" class="machine-card">
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
            <span>更新: {{ door.lastUpdateTime || '-' }}</span>
          </div>
        </div>
      </a-card>
    </div>
  </div>
</template>
<script lang="ts" setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { message } from 'ant-design-vue';
import Header from '../header/Header.vue';
import { defHttp } from '/@/utils/http/axios';

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
const statusList = ref<MachineStatusItem[]>([]);
let refreshTimer: ReturnType<typeof setInterval> | null = null;

const summary = computed(() => {
  const list = statusList.value;
  return {
    online: list.filter((i) => i.online).length,
    offline: list.filter((i) => !i.online).length,
    full: list.filter((i) => i.online && i.fullMaterial).length,
  };
});

// 按机台分组
const machineGroups = computed(() => {
  const map = new Map<string, MachineStatusItem[]>();
  for (const item of statusList.value) {
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
    statusList.value = res || [];
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
.mobile-machine-status {
  min-height: 100vh;
  background: #f5f6fa;
  padding-bottom: 20px;
}

.summary-bar {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  background: #ffffff;
  border-bottom: 1px solid #f0f0f0;
  font-size: 14px;

  b {
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

  .ant-btn {
    margin-left: auto;
  }
}

.loading-wrapper,
.empty-wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 80px 0;
  color: #999;
}

.loading-text {
  margin-top: 12px;
  font-size: 14px;
}

.machine-list {
  padding: 12px;
}

.machine-card {
  margin-bottom: 12px;
  border-radius: 10px;
  overflow: hidden;
}

.door-item {
  border: 1px solid #f0f0f0;
  border-radius: 8px;
  padding: 10px;
  margin-bottom: 8px;
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
  margin-bottom: 6px;

  .door-name {
    font-weight: 600;
    font-size: 15px;
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
  gap: 3px;
  font-size: 13px;
  color: #666;
}
</style>
