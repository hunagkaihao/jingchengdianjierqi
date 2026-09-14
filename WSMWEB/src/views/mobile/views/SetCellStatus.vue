<template>
  <div class="box-incell-container">
    <Header numb="设置库位状态"></Header>
    
    <!-- 库位编码输入框 -->
    <a-row class="input-row">
      <a-col :span="6">
        <div class="htext">
          <h1 autofocus="autofocus">库位编码:</h1>
        </div>
      </a-col>
      <a-col :span="17">
        <a-input v-model:value="currentCell" placeholder="扫描库位码" @keyup.enter="scanCellCode" :allowClear="true"
          @focus="focusFn" class="modern-input">
          <template #suffix>
            <ScanOutlined class="scan-icon" />
          </template>
        </a-input>
      </a-col>
    </a-row>
    
    <!-- 状态显示 -->
    <a-row class="input-row">
      <a-col :span="6">
        <div class="htext">
          <h1>目前状态:</h1>
        </div>
      </a-col>
      <a-col :span="17">
        <div class="status-badge-container">
          <span class="status-badge" :class="currentStatus ? 'status-available' : 'status-empty'">
            {{ currentStatus ? '有货' : '无货' }}
          </span>
        </div>
      </a-col>
    </a-row>
    
    <div class="content-wrapper">
      <div class="tab-bar">
        <a-button type="primary" danger class="modern-btn" @click="setStatus(1)">
          设为无货
        </a-button>
        <a-button type="primary" class="modern-btn" @click="setStatus(2)">
          设为有货
        </a-button>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, onMounted } from 'vue';
import { ScanOutlined } from '@ant-design/icons-vue';
import { message } from 'ant-design-vue';
import { router } from '/@/router';
import Header from '../header/Header.vue';
import { CellServiceProxy, CellStatusUpdateDto } from '/@/services/ServiceProxies';

// 创建服务代理实例
const cellService = new CellServiceProxy();

const currentCell = ref(''); // 当前库位
const currentStatus = ref(false); // 当前状态，true为有货，false为无货

const goBack = () => {
  router.back();
};

// 根据库位编码获取库位信息
const getByCellCode = async (cellCode: string) => {
  try {
    const res = await cellService.getByCellCode(cellCode);
    return res;
  } catch (error) {
    message.error('获取库位信息失败');
    return null;
  }
};

const scanCellCode = async () => {
  if (!currentCell.value) {
    message.warning('请输入库位编码');
    return;
  }
  
  // 调用后端接口获取库位信息
  const cellInfo = await getByCellCode(currentCell.value);
  
  if (cellInfo) {
    // 根据cellStatus判断有货无货状态，"Nohave"表示无货，其他表示有货
    currentStatus.value = cellInfo.cellStatus !== 'Nohave';
    message.success('获取库位信息成功');
  }
};

const setStatus = async (cellStatus: number) => {
  if (!currentCell.value) {
    message.warning('请输入库位编码');
    return;
  }
  
  const dto = new CellStatusUpdateDto();
  dto.cellCode = currentCell.value;
  dto.cellStatus = cellStatus;
  
  try {
    const res = await cellService.updateCellStatus(dto);
    
    if (res && res.success) {
      // 根据cellStatus设置当前状态显示，1为无货，2为有货
      currentStatus.value = cellStatus === 2;
      message.success(`设置成功，库位 ${currentCell.value} 已设为${cellStatus === 2 ? '有货' : '无货'}`);
    } else {
      message.error(res?.message || '设置失败');
    }
  } catch (error) {
    message.error('设置库位状态失败');
  }
};

//软件盘弹出屏蔽
function focusFn(e) {
  e.target.setAttribute('readonly', 'readonly');
  setTimeout(() => {
    e.target.removeAttribute('readonly');
  }, 200);
}

onMounted(() => {
  // 页面加载时的初始化逻辑
});
</script>

<style scoped lang="less">

/* 主容器样式 */
.box-incell-container {
    min-height: 100vh;
    background: #ffffff;
    padding: 0;
    position: relative;
    overflow-x: hidden;
}

/* 输入行样式 */
.input-row {
    margin: 10px 0;
    padding: 0 16px;
}

/* 文字标签样式 */
.htext {
    text-align: center;
    line-height: 32px;
    
    h1 {
        color: #333333;
        font-size: 14px;
        font-weight: 500;
        margin: 0;
        letter-spacing: 0.3px;
    }
}

/* 现代化输入框样式 */
.modern-input {
    height: 32px;
    border-radius: 6px !important;
    border: 1px solid #d9d9d9 !important;
    background: #ffffff !important;
    transition: all 0.2s ease !important;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1) !important;
    
    &:focus,
    &:hover {
        border-color: #1890ff !important;
        box-shadow: 0 1px 6px rgba(24, 144, 255, 0.2) !important;
    }
    
    &::placeholder {
        color: rgba(0, 0, 0, 0.45) !important;
        font-weight: 400;
    }
}

/* 扫描图标样式 */
.scan-icon {
    color: #1890ff !important;
    font-size: 18px;
    transition: all 0.2s ease;
    
    &:hover {
        color: #40a9ff !important;
    }
}

/* 内容包装器 */
.content-wrapper {
    padding: 16px;
}

/* 状态徽章容器 */
.status-badge-container {
    display: flex;
    align-items: center;
    height: 32px;
}

/* 状态徽章 */
.status-badge {
    padding: 6px 16px;
    border-radius: 16px;
    font-size: 14px;
    font-weight: 600;
    transition: all 0.3s ease;
}

.status-available {
    background: rgba(82, 196, 26, 0.1);
    color: #52c41a;
    border: 1px solid rgba(82, 196, 26, 0.3);
}

.status-empty {
    background: rgba(255, 77, 79, 0.1);
    color: #ff4d4f;
    border: 1px solid rgba(255, 77, 79, 0.3);
}

/* 现代化按钮样式 */
.modern-btn {
    margin: 0 8px;
    height: 32px !important;
    border-radius: 6px !important;
    font-size: 14px !important;
    font-weight: 500 !important;
    border: none !important;
    transition: all 0.2s ease !important;
    flex: 1;
    
    &:hover {
        box-shadow: none !important;
    }
    
    &:active {
        box-shadow: none !important;
    }
}

/* 底部操作栏样式 */
.tab-bar {
    display: flex;
    align-items: center;
    position: fixed;
    left: 0;
    right: 0;
    bottom: 0;
    height: 60px;
    background: #ffffff;
    border-top: 1px solid #f0f0f0;
    box-shadow: none;
    padding: 0 16px;
    z-index: 1000;
}
</style>