<template>
  <div>
    <BasicTable @register="registerTable" :clickToRowSelect="false" size="small">
      <template #toolbar>
        <a-button
          type="primary"
          @click="MoveWall"
        >
          {{ t('调拨下架') }}
        </a-button>
        <a-button
          type="primary"
          @click="openModal"
        >
          {{ t('Excel导出') }}
        </a-button>
      </template>
      <template #isActive="{ record }">
        <Tag :color="record.isActive ? 'green' : 'red'">
          {{ record.isActive ? t('common.enabled') : t('common.disEnabled') }}
        </Tag>
      </template>

    </BasicTable>


    <ExpExcelModal @register="register" @success="defaultHeader" />
    <StockAdjustmentModal @register="registerAdjustmentModal" />

  </div>
</template>

<script lang="ts" setup>
import { onMounted, onUnmounted } from 'vue';
import { useMessage } from '/@/hooks/web/useMessage';
import { BasicTable, useTable, TableAction } from '/@/components/Table';
import {
  tableColumns,
  searchFormSchema
} from './Good';
import { useI18n } from '/@/hooks/web/useI18n';
import { jsonToSheetXlsx, ExpExcelModal, ExportModalResult } from '/@/components/Excel';
//import ImportOut from './ExcelOut.vue';
import { useModal } from '/@/components/Modal';
import { StockDto } from '/@/services/ServiceProxies';
import { message } from 'ant-design-vue';
import StockAdjustmentModal from './StockAdjustmentModal.vue';
const [register, { openModal }] = useModal();
const [registerAdjustmentModal, { openModal: openAdjustmentModal }] = useModal();
const { createConfirm } = useMessage();
const { t } = useI18n();
// table配置
const [registerTable, {getDataSource, reload,getSelectRows,clearSelectedRowKeys}] = useTable({
  columns: tableColumns,
  formConfig: {
    labelWidth: 70,
    schemas: searchFormSchema,
    fieldMapToTime: [['time', ['stockInDateStart', 'stockInDateEnd'], 'YYYY-MM-DD HH:mm:ss'],
    ['productiontime', ['supplierProductionDateStart', 'supplierProductionDateEnd'], 'YYYY-MM-DD HH:mm:ss'],
    ['bztime', ['expiryDateStart', 'expiryDateEnd'], 'YYYY-MM-DD HH:mm:ss'],],
  },
  // api: gettable, // 移除API调用
  dataSource: [], // 使用静态数据
  showTableSetting: true,
  useSearchForm: true,
  bordered: true,
  canResize: true,
  rowKey: 'id', //设置选择项的key
  showIndexColumn: false,
  rowSelection: { type: 'checkbox' },
});
// 移除gettable函数，不再需要接口调用
async function MoveWall(){
  let b = getSelectRows() 
      console.log('Selected Rows:', b); // 打印选中行数据
      if (b.length == 0) {
        message.warn(t('请先选择物料'));
        return;
      }
      
      // 移除接口调用，只显示提示信息
      message.info('调拨下架功能已禁用，请等待后续更新');
      clearSelectedRowKeys();
}


function defaultHeader({ filename, bookType }: ExportModalResult) {
        // 移除接口调用，显示提示信息
        message.info('Excel导出功能已禁用，请等待后续更新');
        console.log('Excel导出功能已禁用');
      }

// 监听事件来打开调整弹窗
const handleOpenAdjustmentModal = () => {
  const recordData = sessionStorage.getItem('selectedStockRecord');
  
  if (recordData) {
    try {
      const record = JSON.parse(recordData);
      // 移除接口调用，显示提示信息
      message.info('库存调整功能已禁用，请等待后续更新');
      console.log('库存调整功能已禁用，选中记录:', record);
    } catch (error) {
      console.error('解析数据失败:', error);
    }
  }
};

// 监听库存调整成功事件，刷新数据
const handleStockAdjustmentSuccess = () => {
  // 移除数据刷新功能
  console.log('库存调整成功事件已禁用');
};

onMounted(() => {
  window.addEventListener('openStockAdjustment', handleOpenAdjustmentModal);
  window.addEventListener('stockAdjustmentSuccess', handleStockAdjustmentSuccess);
});

onUnmounted(() => {
  window.removeEventListener('openStockAdjustment', handleOpenAdjustmentModal);
  window.removeEventListener('stockAdjustmentSuccess', handleStockAdjustmentSuccess);
});

</script>