<template>
  <div>
    <BasicTable @register="registerTable" :clickToRowSelect="false" size="small">
      <template #toolbar>
        <a-button type="primary" @click="executeTask">执行任务</a-button>
        <a-button type="primary" @click="completeTask">完成任务</a-button>
        <a-button type="primary" @click="cancelTask">取消任务</a-button>
      </template>
    </BasicTable>
  </div>
</template>

<script lang="ts" setup>
import { ref, onMounted } from 'vue';
import { message } from 'ant-design-vue';
import { BasicTable, useTable } from '/@/components/Table';
import { tableColumns, searchFormSchema } from './AgvTask';
import { AgvTaskServiceProxy, PagingAgvTaskListInput } from '/@/services/ServiceProxies';
import moment from 'moment';

// 创建服务代理实例
const agvTasksService = new AgvTaskServiceProxy();

// 获取AGV任务列表
async function getTable(params: any) {
  // 打印完整的params对象
  //console.log('完整的params对象:', params);
  
  // 构建查询参数
  const input = new PagingAgvTaskListInput({
    pageIndex: params.pageIndex || params.current || params.page || 1, // 支持pageIndex、current和page参数
    pageSize: params.pageSize || 10, // 确保pageSize有默认值
    filter: params.taskId || undefined, // 任务编号为空时不传递
    startCreationTime: params.createTime?.[0] ? moment(params.createTime[0]) : undefined,
    endCreationTime: params.createTime?.[1] ? moment(params.createTime[1]).add(1, 'day') : undefined, // 结束时间往后推一天，包含当天
    agvTaskStatus: params.taskStatus === '全部' ? undefined : params.taskStatus
  });

  
  try {
    // 直接调用后端API并返回结果，像Cell.vue一样
    const result = await agvTasksService.page(input);
    console.log('后端返回数据:', result);
    return result;
  } catch (error) {
    message.error('获取AGV任务列表失败');
    console.error('获取AGV任务列表失败:', error);
    return {
      items: [],
      total: 0
    };
  }
}

// 表格配置
const [registerTable, { reload, getSelectRows }] = useTable({
  columns: tableColumns,
  formConfig: {
    labelWidth: 70,
    schemas: searchFormSchema,
  },
  api: getTable,
  showTableSetting: true,
  useSearchForm: true,
  bordered: true,
  canResize: true,
  showIndexColumn: true,
  rowSelection: { type: 'radio' }, // 改为单选模式
  pagination: {
    pageSize: 10,
    showSizeChanger: true,
    pageSizeOptions: ['10', '20', '50', '100'],
    showTotal: (total) => `共 ${total} 条记录`,
    showQuickJumper: true,
  },
});

// 执行任务
const executeTask = async () => {
  const selectedRows = getSelectRows();
  if (selectedRows.length === 0) {
    message.error('请选择要执行的任务');
    return;
  }
  
  // 只处理第一个选中的任务（单选模式下只会有一个）
  const task = selectedRows[0];
  
  try {
    console.log('执行任务:', task.id);
    await agvTasksService.setAsExecuting(task.id);
    message.success('执行任务成功');
    // 刷新任务列表
    reload();
  } catch (error: any) {
    // 处理后端错误信息
    let errorMessage = '执行任务失败';
    
    // 尝试从不同的错误结构中获取错误信息
    if (error.response?.data?.error?.message) {
      errorMessage = error.response.data.error.message;
    } else if (error.error?.message) {
      errorMessage = error.error.message;
    } else if (error.message) {
      errorMessage = error.message;
    } else if (error.response?.data?.message) {
      errorMessage = error.response.data.message;
    } else if (error.response?.data) {
      errorMessage = JSON.stringify(error.response.data);
    }
    
    message.error(errorMessage);
    console.error('执行任务失败:', error);
  }
};

// 完成任务
const completeTask = async () => {
  const selectedRows = getSelectRows();
  if (selectedRows.length === 0) {
    message.error('请选择要完成的任务');
    return;
  }
  
  // 只处理第一个选中的任务（单选模式下只会有一个）
  const task = selectedRows[0];
  
  try {
    console.log('完成任务:', task.id);
    await agvTasksService.setAsCompleted(task.id);
    message.success('完成任务成功');
    // 刷新任务列表
    reload();
  } catch (error: any) {
    // 处理后端错误信息
    let errorMessage = '完成任务失败';
    
    // 尝试从不同的错误结构中获取错误信息
    if (error.response?.data?.error?.message) {
      errorMessage = error.response.data.error.message;
    } else if (error.error?.message) {
      errorMessage = error.error.message;
    } else if (error.message) {
      errorMessage = error.message;
    } else if (error.response?.data?.message) {
      errorMessage = error.response.data.message;
    } else if (error.response?.data) {
      errorMessage = JSON.stringify(error.response.data);
    }
    
    message.error(errorMessage);
    console.error('完成任务失败:', error);
  }
};

// 取消任务
const cancelTask = async () => {
  const selectedRows = getSelectRows();
  if (selectedRows.length === 0) {
    message.error('请选择要取消的任务');
    return;
  }
  
  // 只处理第一个选中的任务（单选模式下只会有一个）
  const task = selectedRows[0];
  
  try {
    console.log('取消任务:', task.id);
    await agvTasksService.setAsCancel(task.id, false); // isSync设为false
    message.success('取消任务成功');
    // 刷新任务列表
    reload();
  } catch (error: any) {
    // 处理后端错误信息
    console.log('错误对象:', error);
    console.log('错误响应:', error.response);
    
    let errorMessage = '取消任务失败';
    
    // 尝试从不同的错误结构中获取错误信息
    if (error.response?.data?.error?.message) {
      errorMessage = error.response.data.error.message;
    } else if (error.error?.message) {
      errorMessage = error.error.message;
    } else if (error.message) {
      errorMessage = error.message;
    } else if (error.response?.data?.message) {
      errorMessage = error.response.data.message;
    } else if (error.response?.data) {
      errorMessage = JSON.stringify(error.response.data);
    }
    
    message.error(errorMessage);
    console.error('取消任务失败:', error);
  }
};



// 页面加载时执行
onMounted(() => {
  // 这里可以添加初始加载逻辑
});
</script>

