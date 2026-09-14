<template>
  <div>
    <BasicTable @register="registerTable" :clickToRowSelect="false" size="small">
      <template #toolbar>
        <a-button
          type="primary"
          @click="openModal"
        >
          {{ t('Excel导出') }}
        </a-button>
      </template>
      
    </BasicTable>

    <ExpExcelModal @register="register" @success="defaultHeader" />
  </div>
</template>

<script lang="ts" setup>
import { onMounted, onUnmounted } from 'vue';
import { useMessage } from '/@/hooks/web/useMessage';
import { BasicTable, useTable } from '/@/components/Table';
import {
  tableColumns,
  searchFormSchema,
  getStockAdjustmentsAsync,
  getStockAdjustmentsAllAsync
} from './StockAdjustment';
import { useI18n } from '/@/hooks/web/useI18n';
import { jsonToSheetXlsx, ExpExcelModal, ExportModalResult } from '/@/components/Excel';
import { useModal } from '/@/components/Modal';
import { StockAdjustmentResultDto } from '/@/services/ServiceProxies';
import { message } from 'ant-design-vue';

const [register, { openModal }] = useModal();
const { createConfirm } = useMessage();
const { t } = useI18n();

// table配置
const [registerTable, {getDataSource, reload,getSelectRows,clearSelectedRowKeys}] = useTable({
  columns: tableColumns,
  formConfig: {
    labelWidth: 70,
    schemas: searchFormSchema,
    fieldMapToTime: [
      ['adjustmentTimeRange', ['adjustmentTimeStart', 'adjustmentTimeEnd'], 'YYYY-MM-DD HH:mm:ss']
    ],
  },
  api: getTableData,
  showTableSetting: true,
  useSearchForm: true,
  bordered: true,
  canResize: true,
  rowKey: 'id',
  showIndexColumn: false,
});

var data : any[] = [];
var allData : StockAdjustmentResultDto[] = [];

async function getTableData(params){
  console.log(params)
  
  // 清理空值参数
  Object.keys(params).forEach(key => {
    if (params[key] === "" || params[key] === null || params[key] === undefined) {
      delete params[key];
    }
  });

  allData = await getStockAdjustmentsAllAsync(params);
  return await getStockAdjustmentsAsync(params);
}

function defaultHeader({ filename, bookType }: ExportModalResult) {
  // 默认Object.keys(data[0])作为header
  data.length = 0;
  for (let index = 0; index < allData.length; index++) {
    data.push({
      物料编码: allData[index].materialCode,
      物料名称: allData[index].materialName,
      规格: allData[index].specs,
      单位: allData[index].unit,
      收料条形码: allData[index].barcode,
      检验编号: allData[index].checkNo,
      检验单号: allData[index].checkOrderCode,
      调整前数量: allData[index].originalQuantity,
      调整数量: allData[index].adjustmentQuantity,
      调整类型: allData[index].adjustmentTypeDescription,
      调整原因: allData[index].adjustmentReason,
      操作人: allData[index].operatorName,
      调整时间: allData[index].adjustmentTime,
      供应商编码: allData[index].supplierCode,
      供应商名称: allData[index].supplierName,
      批次号: allData[index].batchCode,
      容器编号: allData[index].boxCode,
      库位编号: allData[index].cellCode,
      仓库编码: allData[index].warehouseCode,
      仓库名称: allData[index].warehouseName,
    });
  }
  console.log(data) 
  jsonToSheetXlsx({
    data,
    filename,
    write2excelOpts: {
      bookType,
    },
  });
}
</script>
