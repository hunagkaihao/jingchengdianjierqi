import { FormSchema } from '/@/components/Table';
import { BasicColumn } from '/@/components/Table';
import { useI18n } from '/@/hooks/web/useI18n';
import moment from 'moment';

const { t } = useI18n();

// 表格列定义
export const tableColumns: BasicColumn[] = [
  {
    title: '任务编号',
    dataIndex: 'id',
    width: 100,
  },
  {
    title: '起始位置',
    dataIndex: 'startPosition',
    width: 100,
  },
  {
    title: '目标位置',
    dataIndex: 'targetPosition',
    width: 100,
  },
  {
    title: '容器编号',
    dataIndex: 'boxCode',
    width: 120,
  },
  {
    title: '任务类型',
    dataIndex: 'stockTyp',
    width: 120,
    customRender: ({ record }) => {
      const taskTypeMap: Record<number, string> = {
        0: 'CTU入库',
        1: 'CTU出库',
        2: '叉车入库',
        3: '叉车出库',
        4: 'agv移动料车',
        5: 'CTU调拨',
        6: '叉车调拨',
        7: '物料入库',
        8: '料车发送',
        9: '料车叫回',
        10: 'CTU输送线入库',
        11: 'CTU输送线出库',
        12: '叉车输送线入库',
        13: '叉车输送线出库',
        14: '空盒衬上机台',
        15: '成品下机台',
        16: '空盒衬下机台',
        17: '半成品上料',
      };
      return taskTypeMap[record.stockTyp] || record.stockTyp;
    },
  },
  {
    title: '任务状态',
    dataIndex: 'taskStatus',
    width: 120,
    customRender: ({ record }) => {
      const taskStatusMap: Record<number, string> = {
        0: '被创建',
        1: '等待执行',
        2: '执行中',
        3: '任务开始',
        4: '出储位',
        5: '等待任务继续',
        9: '任务完成',
        10: '调度删除任务',
      };
      return taskStatusMap[record.taskStatus] || record.taskStatus;
    },
  },
  {
    title: '创建时间',
    dataIndex: 'createTime',
    width: 180,
    customRender: ({ record }) => {
      if (record.createTime) {
        return new Date(record.createTime).toLocaleString('zh-CN');
      }
      return '';
    },
  },
];

// 搜索表单定义
export const searchFormSchema: FormSchema[] = [
  {
    field: 'taskId',
    label: '任务编号',
    component: 'Input',
    colProps: { span: 6 },
    componentProps: {
      placeholder: '输入任务编号',
    },
  },
  {
    field: 'createTime',
    label: '创建时间',
    component: 'RangePicker',
    colProps: { span: 8 },
    componentProps: {
      format: 'YYYY-MM-DD',
      placeholder: ['开始日期', '结束日期'],
    },
    defaultValue: [moment().subtract(7, 'days'), moment()],
  },
  {
    field: 'taskStatus',
    label: '任务状态',
    component: 'Select',
    colProps: { span: 6 },
    componentProps: {
      options: [
        { label: '全部', value: '全部' },
        { label: '等待执行', value: '1' },
        { label: '执行中', value: '2' },
        { label: '任务开始', value: '3' },
        { label: '出储位', value: '4' },
        { label: '任务完成', value: '9' },
        { label: '调度删除任务', value: '10' },
      ],
      placeholder: '选择任务状态',
    },
    defaultValue: '全部',
  },
];
