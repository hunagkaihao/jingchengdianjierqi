<template>
  <div class="hot-stamping-container">
    <div class="main-content">
      <!-- 左侧区域：烫金区标题、网格和创建任务按钮 -->
      <div class="left-section">
        <div class="section-header">
          <h1>{{ t('烫金区') }}</h1>
        </div>
        <div class="grid-container">
          <div 
            class="grid-item" 
            v-for="item in gridItems" 
            :key="item.id"
            :class="{ 'active': selectedGridItem === item.code }"
            @click="selectedGridItem = item.code"
          >
            {{ item.code }}
          </div>
        </div>
        <div class="action-button">
          <a-button type="primary" @click="createTask">{{ t('创建任务') }}</a-button>
        </div>
      </div>
      
      <!-- 右侧区域：搜索和物料表格 -->
      <div class="right-section">
        <BasicTable @register="registerTable" size="small">
          <template #toolbar>
            <a-button type="primary" style="margin-right: 10px">{{ t('重置') }}</a-button>
            <a-button type="primary" style="margin-left: 10px">{{ t('查询') }}</a-button>
          </template>
        </BasicTable>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, watch, onMounted } from 'vue';
import { useI18n } from '/@/hooks/web/useI18n';
import { BasicTable, useTable } from '/@/components/Table';
import { StockServiceProxy, CellServiceProxy } from '/@/services/ServiceProxies';
import { message } from 'ant-design-vue';

const { t } = useI18n();
const _stockService = new StockServiceProxy();
const _cellService = new CellServiceProxy();

// 网格数据
const gridItems = ref<any[]>([]);

// 加载库位数据
async function loadCellData() {
  try {
    const result = await _cellService.cellsGetByAreaId(2); // 烫金区的区域id是2
    gridItems.value = result.map((cell, index) => ({
      id: index + 1,
      code: cell.cellName || cell.cellCode
    }));
  } catch (error) {
    console.error('获取库位数据失败:', error);
    gridItems.value = [];
  }
}

// 页面加载时获取库位数据
onMounted(() => {
  loadCellData();
});

// 选中的网格项
const selectedGridItem = ref('');

// 表格列定义
const columns = [
  
  {
    title: t('物料编号'),
    dataIndex: 'materialCode',
    key: 'materialCode',
  },
  {
    title: t('物料名称'),
    dataIndex: 'materialName',
    key: 'materialName',
  },
  {
    title: t('产品米数'),
    dataIndex: 'productLength',
    key: 'productLength',
  },
  {
    title: t('状态'),
    dataIndex: 'status',
    key: 'status',
  },
  {
    title: t('组织名称'),
    dataIndex: 'organizationName',
    key: 'organizationName',
  },
  {
    title: t('位置'),
    dataIndex: 'cellCode',
    key: 'cellCode',
  },
];

// 表格配置
const [registerTable, { reload, getSelectRows }] = useTable({
  columns: columns,
  formConfig: {
    labelWidth: 70,
    schemas: [
      {
        field: 'productName',
        label: t('产品名称'),
        component: 'Input',
        colProps: {
          span: 8,
        },
      },
    ],
  },
  api: async (params) => {
    try {
      const result = await _stockService.stocksGetByAreas([4, 5]);
      return {
        items: result || [],
        total: (result || []).length,
      };
    } catch (error) {
      console.error('获取库存数据失败:', error);
      return {
        items: [],
        total: 0,
      };
    }
  },
  showTableSetting: true,
  useSearchForm: true,
  bordered: true,
  canResize: true,
  showIndexColumn: true,
  rowSelection: { type: 'checkbox' },
});

// 页面加载时获取数据 - 现在通过表格的 api 函数自动调用
// fetchStockData([4, 5]);

// 创建任务
function createTask() {
  // 校验是否选择库位
  if (!selectedGridItem.value) {
    message.warning('请选择库位');
    return;
  }
  
  // 校验是否选择物料
  const selectedRows = getSelectRows();
  if (selectedRows.length === 0) {
    message.warning('请选择物料');
    return;
  }
  
  // 校验只能选择一个库存
  if (selectedRows.length > 1) {
    message.warning('只能选择一个库存');
    return;
  }
  
  // 后续创建任务的逻辑
  console.log('创建任务:', {
    selectedGridItem: selectedGridItem.value,
    selectedMaterials: selectedRows
  });
  // 这里可以添加实际的创建任务逻辑
  message.success('任务创建成功');
}
</script>

<style scoped>
.hot-stamping-container {
  padding: 20px;
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.section-header {
  margin-bottom: 30px;
  text-align: center;
}

.section-header h1 {
  font-size: 24px;
  font-weight: bold;
  margin: 0;
}

.main-content {
  display: flex;
  gap: 30px;
  flex: 1;
}

.left-section {
  width: 300px;
  flex-shrink: 0;
}

.right-section {
  flex: 1;
  min-width: 0;
}

.grid-container {
  display: grid;
  grid-template-columns: repeat(4, 60px);
  gap: 10px;
  margin-bottom: 30px;
}

.grid-item {
  background: #ffffff;
  border: 1px solid #d9d9d9;
  padding: 12px;
  text-align: center;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.3s ease;
  font-size: 14px;
}

.grid-item:hover {
  border-color: #1890ff;
  box-shadow: 0 0 0 2px rgba(24, 144, 255, 0.2);
}

.grid-item.active {
  background: #1890ff;
  color: white;
  border-color: #1890ff;
}

.action-button {
  margin-bottom: 30px;
  text-align: center;
}

.action-button .ant-btn {
  width: 120px;
}

.right-section {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

/* 确保表格占满右侧区域 */
.right-section :deep(.ant-table) {
  flex: 1;
}

.right-section :deep(.ant-table-container) {
  height: calc(100vh - 200px);
}

/* 调整表格行高和字体大小 */
.right-section :deep(.ant-table-tbody > tr > td) {
  font-size: 14px;
  padding: 8px 12px;
}

.right-section :deep(.ant-table-thead > tr > th) {
  font-size: 14px;
  padding: 10px 12px;
}
</style>