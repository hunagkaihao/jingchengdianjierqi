<template>
  <div class="mobile-machine-config">
    <Header numb="机台配置"></Header>

    <div class="summary-bar">
      <span>机台: <b>{{ machineList.length }}</b></span>
      <span class="enabled">启用: <b>{{ enabledCount }}</b></span>
      <span class="disabled">停用: <b>{{ machineList.length - enabledCount }}</b></span>
      <a-button size="small" type="primary" :loading="loading" @click="loadMachines">刷新</a-button>
    </div>

    <a-alert class="tip-bar" message="停用后机台将不再监控信号、不再自动创建搬运任务（10秒内生效）" type="info" banner />

    <div v-if="loading" class="loading-wrapper">
      <a-spin size="large" />
      <span class="loading-text">加载中...</span>
    </div>

    <div v-else-if="machineList.length === 0" class="empty-wrapper">
      <a-empty description="暂无机台配置数据" />
    </div>

    <div v-else class="machine-list">
      <div v-for="machine in machineList" :key="machine.id" class="machine-item">
        <div class="machine-info">
          <div class="machine-name">
            {{ machine.machineName }}
            <a-tag :color="machine.isEnabled ? 'success' : 'default'" class="status-tag">
              {{ machine.isEnabled ? '已启用' : '已停用' }}
            </a-tag>
          </div>
          <div class="machine-desc">
            <span v-if="machine.description">{{ machine.description }}</span>
            <span v-if="machine.ipAddress">{{ machine.ipAddress }}:{{ machine.port }}</span>
          </div>
        </div>
        <div class="machine-switch">
          <a-switch
            :checked="machine.isEnabled"
            :loading="switchingId === machine.id"
            size="large"
            @change="(checked: any) => onSwitchChange(machine, checked)"
          />
        </div>
      </div>
    </div>
  </div>
</template>
<script lang="ts" setup>
import { ref, computed, onMounted } from 'vue';
import { message } from 'ant-design-vue';
import Header from '../header/Header.vue';
import { defHttp } from '/@/utils/http/axios';

interface MachineConfigItem {
  id: number;
  machineName: string;
  description: string;
  ipAddress: string;
  port: number;
  machineNumber: number;
  isEnabled: boolean;
}

const loading = ref(false);
const machineList = ref<MachineConfigItem[]>([]);
const switchingId = ref<number | null>(null);

const enabledCount = computed(() => machineList.value.filter((m) => m.isEnabled).length);

async function loadMachines() {
  loading.value = true;
  try {
    // 后端返回原生 JSON 数组（非 {code, result} 包装），跳过统一解包
    const res = await defHttp.get<MachineConfigItem[]>(
      { url: '/wms/machine-status/machines' },
      { isTransformResponse: false },
    );
    machineList.value = res || [];
  } catch (error) {
    console.error('获取机台配置失败:', error);
    message.error('获取机台配置失败');
  } finally {
    loading.value = false;
  }
}

async function onSwitchChange(machine: MachineConfigItem, checked: boolean) {
  switchingId.value = machine.id;
  try {
    await defHttp.post(
      {
        url: '/wms/machine-status/setMachineEnabled',
        // 注意：vben defHttp 对 POST 只传 params 会把参数塞进 body 但 Controller 简单参数绑定不到，
        // 这里用 data(JSON body) + 后端 DTO 接收
        data: { machineId: machine.id, isEnabled: checked },
      },
      { isTransformResponse: false },
    );
    machine.isEnabled = checked;
    message.success(`机台 ${machine.machineName} 已${checked ? '启用' : '停用'}`);
  } catch (error) {
    console.error('设置机台启用状态失败:', error);
    message.error('设置机台启用状态失败');
  } finally {
    switchingId.value = null;
  }
}

onMounted(() => {
  loadMachines();
});
</script>

<style scoped lang="less">
.mobile-machine-config {
  min-height: 100vh;
  background: #f5f6fa;
  padding-bottom: 30px;
}

.summary-bar {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 10px 14px;
  font-size: 16px;
  color: #333;

  b {
    font-size: 18px;
  }

  .enabled {
    color: #52c41a;
  }

  .disabled {
    color: #999;
  }
}

.tip-bar {
  margin: 0 14px;
  font-size: 14px;
}

.machine-list {
  margin: 14px;
  background: #fff;
  border-radius: 10px;
  overflow: hidden;
}

.machine-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 16px;
  border-bottom: 1px solid #f0f0f0;

  &:last-child {
    border-bottom: none;
  }

  .machine-info {
    flex: 1;
    min-width: 0;

    .machine-name {
      font-size: 18px;
      font-weight: 600;
      color: #222;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .status-tag {
      font-size: 13px;
    }

    .machine-desc {
      margin-top: 6px;
      font-size: 14px;
      color: #888;
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
    }
  }

  .machine-switch {
    margin-left: 12px;
  }
}

.loading-wrapper,
.empty-wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 80px 0;

  .loading-text {
    margin-top: 12px;
    font-size: 16px;
    color: #999;
  }
}
</style>
