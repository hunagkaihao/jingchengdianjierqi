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
      <template #action="{ record }">
            <TableAction
              :actions="[
                {
                  //icon: 'ant-design:edit-outlined',
                  auth: 'Wms.Edit',
                  label: t('重新入库'),
                  onClick: handleDelete.bind(null, record),
                },
              ]"
            />
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
import {StockOutHistoryDto}from '/@/services/ServiceProxies';
import {
  tableColumns,
  searchFormSchema
} from './OutcellHis';
import moment from 'moment';
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
    fieldMapToTime: [['time', ['stockOutTimeMin', 'stockOutTimeMax'], 'YYYY-MM-DD HH:mm:ss']],
  },
  // api: gettable, // 移除API调用
  dataSource: [], // 使用静态数据
  showTableSetting: true,
  useSearchForm: true,
  bordered: true,
  canResize: true,
  showIndexColumn: false,
  //rowSelection: { type: 'checkbox' },
  actionColumn: {
          width: 150,
          title: t('common.action'),
          dataIndex: 'action',
          slots: { customRender: 'action' },
        },
});

// 移除gettable函数，不再需要接口调用

// 删除用户
const handleDelete = async (record: Recordable) => {
        // 移除接口调用，只显示提示信息
        message.info('重新入库功能已禁用，请等待后续更新');
        console.log('重新入库功能已禁用，选中记录:', record);
      };

      function defaultHeader({ filename, bookType }: ExportModalResult) {
        // 移除接口调用，显示提示信息
        message.info('Excel导出功能已禁用，请等待后续更新');
        console.log('Excel导出功能已禁用');
      }
</script>
