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
      <template #isActive="{ record }">
        <Tag :color="record.isActive ? 'green' : 'red'">
          {{ record.isActive ? t('common.enabled') : t('common.disEnabled') }}
        </Tag>
      </template>

    </BasicTable>


<ExpExcelModal @register="register" @success="defaultHeader" />

  </div>
</template>

<script lang="ts" setup>
import { useMessage } from '/@/hooks/web/useMessage';
import { BasicTable, useTable, TableAction } from '/@/components/Table';
import { useModal } from '/@/components/Modal';
import { jsonToSheetXlsx, ExpExcelModal, ExportModalResult } from '/@/components/Excel';
import {StockInHistoryDto}from '/@/services/ServiceProxies';
import moment from 'moment';
import {
  tableColumns,
  searchFormSchema
} from './IncellHis';
import { useI18n } from '/@/hooks/web/useI18n';
const [register, { openModal }] = useModal();
const { createConfirm } = useMessage();
const { t } = useI18n();
// table配置
const [registerTable, { reload }] = useTable({
  columns: tableColumns,
  formConfig: {
    labelWidth: 70,
    schemas: searchFormSchema,
    fieldMapToTime: [['time', ['stockInTimeStart', 'stockInTimeEnd'], 'YYYY-MM-DD HH:mm:ss']],
  },
  // api: gettable, // 移除API调用
  dataSource: [], // 使用静态数据
  showTableSetting: true,
  useSearchForm: true,
  bordered: true,
  canResize: true,
  showIndexColumn: false,
  rowSelection: { type: 'checkbox' },
});

// 移除gettable函数，不再需要接口调用

function defaultHeader({ filename, bookType }: ExportModalResult) {
        // 移除接口调用，显示提示信息
        message.info('Excel导出功能已禁用，请等待后续更新');
        console.log('Excel导出功能已禁用');
      }
</script>
