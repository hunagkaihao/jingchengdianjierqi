<template>
    <div class="empty-tray-return-container">
        <Header numb="空托送回"></Header>
        <!-- 空托送回输入框 -->
        <a-row class="input-row">
            <a-col :span="6">
                <div class="htext">
                    <h1 autofocus="autofocus">空托条码:</h1>
                </div>
            </a-col>
            <a-col :span="18">
                <a-input v-model:value="trayCode" placeholder="扫描空托条码" @keyup.enter="scanTrayCode" :allowClear="true"
                    @focus="focusFn" class="modern-input">
                    <template #suffix>
                        <scan-outlined class="scan-icon" />
                    </template>
                </a-input>
            </a-col>
        </a-row>

        <!-- 空托送回模式的底部操作栏 -->
        <div class="tab-bar">
            <a-button @click="returnEmptyTray" type="primary" class="modern-btn">
                送回空托
            </a-button>
        </div>
    </div>
</template>
<script lang="ts" setup>
import { ref } from 'vue';
import { ScanOutlined } from '@ant-design/icons-vue';
import { message } from 'ant-design-vue';
import { StockServiceProxy } from '/@/services/ServiceProxies';
// 创建服务代理实例
const stockService = new StockServiceProxy();

let trayCode = ref<string>('');

// 扫描空托条码
const scanTrayCode = async () => {
    if (trayCode.value.length > 10) {
        var le = trayCode.value.length
        trayCode.value = trayCode.value.slice(le-10, le)
    }
    // 这里可以添加扫描后的处理逻辑
};

// 送回空托
const returnEmptyTray = async () => {
  try {
    if (!trayCode.value) {
      message.error('请输入空托条码');
      return;
    }
    // 调用空托送回的后端接口
    const result = await stockService.returnEmptyBox(trayCode.value);
    if (result.success) {
      message.success('空托送回成功');
      trayCode.value = '';
    } else {
      message.error(result.message || '空托送回失败');
    }
  } catch (error) {
    message.error('空托送回失败');
  }
};

import Header from '../header/Header.vue';

//软件盘弹出屏蔽
function focusFn(e) {
    e.target.setAttribute('readonly', 'readonly');
    setTimeout(() => {
        e.target.removeAttribute('readonly');
    }, 200);
}

</script>

<style scoped lang="less">

/* 主容器样式 */
.empty-tray-return-container {
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

/* 现代化按钮样式 */
.modern-btn {
    margin: auto;
    height: 32px !important;
    border-radius: 6px !important;
    background: #1890ff !important;
    border: none !important;
    font-size: 14px !important;
    font-weight: 500 !important;
    color: white !important;
    box-shadow: none !important;
    transition: all 0.2s ease !important;
    
    &:hover {
        background: #40a9ff !important;
        box-shadow: none !important;
    }
    
    &:active {
        background: #096dd9 !important;
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
    padding-bottom: 10px;
    z-index: 1000;
}
</style>