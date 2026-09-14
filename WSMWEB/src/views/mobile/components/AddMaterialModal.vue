<template>
  <a-modal
    :visible="visible"
    :title="title"
    :width="width"
    :footer="null"
    @cancel="handleCancel"
    class="add-material-modal"
  >
    <div class="modal-content">
      <a-form :model="formState" layout="vertical">
        <a-form-item label="产品名称">
          <a-input v-model:value="formState.productName" placeholder="请输入产品名称" />
        </a-form-item>
        
        <a-form-item label="产品编号">
          <a-input v-model:value="formState.productCode" placeholder="请输入产品编号" />
        </a-form-item>
        
        <a-form-item label="产品米数">
          <a-input-number v-model:value="formState.productMeters" placeholder="请输入产品米数" :min="0" />
        </a-form-item>
        
        <a-form-item label="纸病名称">
          <a-input v-model:value="formState.defectName" placeholder="请输入纸病名称" />
        </a-form-item>
        
        <a-form-item label="纸病位置">
          <a-input v-model:value="formState.defectPosition" placeholder="请输入纸病位置" />
        </a-form-item>
        
        <a-form-item label="辊号">
          <a-input v-model:value="formState.rollNumber" placeholder="请输入辊号" />
        </a-form-item>
        
        <a-form-item label="机号">
          <a-input v-model:value="formState.machineNumber" placeholder="请输入机号" />
        </a-form-item>
      </a-form>
    </div>
    
    <div class="modal-footer">
      <a-button @click="handleCancel" style="margin-right: 10px;">
        取消
      </a-button>
      <a-button type="primary" @click="handleSubmit">
        确认添加
      </a-button>
    </div>
  </a-modal>
</template>

<script lang="ts" setup>
import { ref, reactive } from 'vue';
import { message } from 'ant-design-vue';

const props = defineProps({
  visible: {
    type: Boolean,
    default: false
  },
  title: {
    type: String,
    default: '添加物料'
  },
  width: {
    type: [String, Number],
    default: '90%'
  }
});

const emit = defineEmits(['register', 'ok', 'cancel', 'update:visible']);

// 表单状态
const formState = reactive({
  productName: '',
  productCode: '',
  productMeters: 0,
  defectName: '',
  defectPosition: '',
  rollNumber: '',
  machineNumber: ''
});

// 处理取消
const handleCancel = () => {
  emit('update:visible', false);
  emit('cancel');
};

// 处理提交
const handleSubmit = () => {
  // 简单验证
  if (!formState.productName) {
    message.error('请输入产品名称');
    return;
  }
  
  if (!formState.productCode) {
    message.error('请输入产品编号');
    return;
  }
  
  if (!formState.productMeters) {
    message.error('请输入产品米数');
    return;
  }
  
  // 发送添加物料事件
  emit('ok', {
    ...formState
  });
  
  // 重置表单
  Object.keys(formState).forEach(key => {
    formState[key] = '';
  });
  formState.productMeters = 0;
  
  // 关闭模态框
  emit('update:visible', false);
};

// 注册组件
const register = (modalInstance) => {
  // 保存模态框实例
};

// 暴露方法
defineExpose({
  register,
  show: () => emit('update:visible', true),
  hide: () => emit('update:visible', false)
});

// 注册组件
emit('register', register);
</script>

<style scoped lang="less">
.add-material-modal {
  /* 为整个弹框内容添加内边距 */
  
  /* 调整弹框内容区域的边距 */
  :deep(.ant-modal-body) {
    padding: 24px;
  }
  
  .modal-content {
    padding: 20px;
    background: #fafafa;
    border-radius: 8px;
  }
  
  .modal-footer {
    display: flex;
    justify-content: flex-end;
    padding: 16px 0;
    margin-top: 20px;
  }
  
  .ant-form-item {
    margin-bottom: 16px;
  }
  
  .ant-form-item-label {
    font-weight: 500;
    margin-bottom: 8px;
    font-size: 14px;
    color: #333;
  }
  
  .ant-input,
  .ant-input-number {
    width: 100%;
    height: 36px;
    border-radius: 6px;
    border: 1px solid #d9d9d9;
    padding: 0 12px;
  }
  
  .ant-input::placeholder,
  .ant-input-number::placeholder {
    color: #999;
  }
  
  .ant-btn {
    height: 36px;
    padding: 0 16px;
    border-radius: 6px;
    font-size: 14px;
  }
  
  .ant-btn-primary {
    background: #1890ff;
    border-color: #1890ff;
  }
  
  .ant-btn-primary:hover {
    background: #40a9ff;
    border-color: #40a9ff;
  }
}
</style>