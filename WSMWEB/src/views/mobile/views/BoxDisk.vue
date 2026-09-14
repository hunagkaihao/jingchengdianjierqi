<template>
    <div class="box-disk-container">
        <Header numb="容器组盘"></Header>
        <a-row class="input-row">
            <a-col :span="6">
                <div class="htext">
                    <h1 autofocus="autofocus">当前容器:</h1>
                </div>
            </a-col>
            <a-col :span="17">
                <a-input v-model:value="boxCode" placeholder="扫描容器" @keyup.enter="scanboxCode" ref="Ref"
                    :allowClear="true" @focus="focusFn" class="modern-input">
                    <template #suffix>
                        <scan-outlined class="scan-icon" />
                    </template>
                </a-input>
            </a-col>
        </a-row>
      
        <!-- 已组盘信息区域 -->
        <div class="box-info-section">
            <p style="margin-left: 20px">已组盘信息:{{ materials.length }}</p>
            
            <!-- 物料信息显示 -->
            <div class="materials-section">
                <div v-if="materials.length > 0">
                    <div v-for="(material, index) in materials" :key="index" class="material-card">
                        <div class="material-header">
                            <span>产品名称: {{ material.materialName }}</span>
                            <a-button type="text" danger @click="removeMaterial(index)" class="delete-btn">
                                删除
                            </a-button>
                        </div>
                        <div class="material-content">
                            <div class="material-row">
                                <span>产品编号: {{ material.materialCode }}</span>
                                <span>产品米数: {{ material.productLength }}</span>
                            </div>
                            <div class="material-row">
                                <span>纸病名称: {{ material.defectName }}</span>
                                <span>纸病位置: {{ material.defectPosition }}</span>
                            </div>
                            <div class="material-row">
                                <span>辊号: {{ material.batchCode }}</span>
                                <span>机号: {{ material.supplierBatchCode }}</span>
                            </div>
                        </div>
                    </div>
                </div>
                <div v-else class="no-materials">
                    <div class="empty-icon">
                        <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="anticon anticon-inbox"><polyline points="22 12 16 12 14 15 10 15 8 12 2 12"></polyline><path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"></path></svg>
                    </div>
                    <p>暂无数据</p>
                </div>
                <div class="add-material-btn">
                    <a-button type="primary" @click="addMaterial">
                        + 添加物料
                    </a-button>
                </div>
            </div>
        </div>

        <div class="tab-bar">
            <a-button @click="openCancelModal" type="primary" class="modern-btn">
                组盘取消
            </a-button>
            <a-button @click="incell" type="primary" class="modern-btn">
                组盘确认
            </a-button>
        </div>
    </div>
    <GoodsDetail @register="registerGoodsDetailModal"></GoodsDetail>
    <BoxDiskCancelModal @register="registerCancelModal"></BoxDiskCancelModal>
    <AddMaterialModal v-model:visible="addMaterialModalVisible" @register="registerAddMaterialModal" @ok="handleAddMaterialOk"></AddMaterialModal>
</template>
<script lang="ts" setup>
import { ref, h, onMounted, onUnmounted} from 'vue';
import { ScanOutlined } from '@ant-design/icons-vue';
import { message } from 'ant-design-vue';
// 导入服务代理
import { StockServiceProxy, StockInBoxDto } from '/@/services/ServiceProxies';

// 创建服务代理实例
const stockService = new StockServiceProxy();



// 获取容器内物料
const stocksGetInBox = async (boxCode: string) => {
  try {
    const res = await stockService.stocksGetInBox(boxCode);
    return res;
  } catch (error: any) {
    const errorMessage = error.error?.message || '获取容器物料失败';
    message.error(errorMessage);
    return [];
  }
};

// 容器解绑
const stocksDisBindBox = async (boxCode: string) => {
  try {
    const res = await stockService.stocksDisBindBox(boxCode);
    return res;
  } catch (error: any) {
    const errorMessage = error.error?.message || '解绑失败';
    message.error(errorMessage);
    return null;
  }
};
import Header from '../header/Header.vue'
import { useModal } from '/@/components/Modal';
// 已删除的IncellByPeople.ts文件，使用内联实现
const diskcolumns = [];
// 已删除的GoodsDetail.vue文件，使用简单的替代组件
const GoodsDetail = { template: '<div>功能已禁用</div>' };
// 已删除的BoxDiskCancelModal.vue文件，使用简单的替代组件
const BoxDiskCancelModal = { template: '<div>功能已禁用</div>' };
// 导入添加物料模态框
import AddMaterialModal from '../components/AddMaterialModal.vue';
const [registerGoodsDetailModal, { openModal: openGoodsDetailModal }] = useModal();
const [registerCancelModal, { openModal: openCancelModalModal }] = useModal();
const [registerAddMaterialModal, { openModal: openAddMaterialModal }] = useModal();

// 控制添加物料模态框显示
const addMaterialModalVisible = ref(false);
let boxCode = ref<string>('');
var dataSource = ref([]
);

// 物料信息数组
const materials = ref<StockInBoxDto[]>([]);

// 添加物料
const addMaterial = () => {
  addMaterialModalVisible.value = true;
};

// 处理添加物料确认
const handleAddMaterialOk = (material: any) => {
  // 转换为 StockInBoxDto 格式
  const stockInBoxDto: StockInBoxDto = {
    materialName: material.productName,
    materialCode: material.productCode,
    productLength: material.productMeters,
    defectName: material.defectName,
    defectPosition: material.defectPosition,
    batchCode: material.rollNumber,
    supplierBatchCode: material.machineNumber
  };
  materials.value.push(stockInBoxDto);
  message.success('物料添加成功');
};

// 删除物料
const removeMaterial = (index) => {
  materials.value.splice(index, 1);
  message.success('物料删除成功');
};
let screenHeight = ref(window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight);
let screenWidth = ref((window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth) - 8)
let showtable = ref(true)
onMounted(() => {
    // 监听组盘取消成功事件
    window.addEventListener('boxDiskCancelSuccess', handleBoxDiskCancelSuccess);
})

onUnmounted(() => {
    window.removeEventListener('boxDiskCancelSuccess', handleBoxDiskCancelSuccess);
})

// 处理组盘取消成功事件
const handleBoxDiskCancelSuccess = () => {
    // 清空数据
    materials.value = [];
    dataSource.value = [];
    boxCode.value = '';
    // 不需要调用scanboxCode()，因为已经清空了相关数据
}

const scanboxCode = async () => {
    if(boxCode.value.length > 10){
        var le = boxCode.value.length
        boxCode.value = boxCode.value.slice(le-10,le)
    }
    await stocksGetInBox(boxCode.value).then((res) => {
        dataSource.value = res
        materials.value = res
    }).catch((err) => {
        message.error(err.error?.message || "获取容器物料失败")
    })
}

const OpenGoodsDetail = (record: Recordable) => {
    openGoodsDetailModal(true, {
        record: record,
    });
};

const incell = async () => {
    if (boxCode.value == '') {
        message.error("没有容器信息")
        return
    }
    if (materials.value.length === 0) {
        message.error("请先添加物料信息")
        return
    }
    try{
        // 准备数据格式
        const stockDirectCreateDtoList = materials.value.map(material => ({
            MaterialName: material.materialName,
            MaterialCode: material.materialCode,
            ProductLength: material.productLength,
            DefectName: material.defectName,
            DefectPosition: material.defectPosition,
            BatchCode: material.batchCode,
            SupplierBatchCode: material.supplierBatchCode
        }));
        
        // 清空组盘信息数组
        materials.value = []
        dataSource.value = []
        
        await stockService.createStockDirectAndBindBox(boxCode.value, stockDirectCreateDtoList).then((res) => {
            if (res.success == true) {
                message.success(res.message)
                // 清空容器编号
                boxCode.value = ''
                // 不需要调用scanboxCode()，因为已经清空了相关数据
            } else if(res.success == false){
                message.error(res.message)
            }else{
                message.error("接口推送异常，组盘失败")
            }
        }).catch((error) => {
            message.error(error.error?.message || "组盘失败")
        })
    }catch(err){
        message.error(err)
    }
}
// 打开组盘取消弹窗
const openCancelModal = async () => {
    if (boxCode.value == '') {
        message.error("没有容器信息")
        return
    }
    
    if (dataSource.value.length === 0) {
        message.error("没有已组盘信息")
        return
    }
    
    try{
        await stocksDisBindBox(boxCode.value).then((res) => {
            if (res.success == true) {
                message.success(res.message)
                // 清空数据
                materials.value = []
                dataSource.value = []
                boxCode.value = ''
            } else if(res.success == false){
                message.error(res.message)
            }else{
                message.error("接口推送异常，解绑失败")
            }
        }).catch((error) => {
            message.error(error.error?.message || "解绑失败")
        })
    }catch(err){
        message.error(err)
    }
}
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
.box-disk-container {
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

::v-deep(.ant-table-thead > tr > th) {
    padding: 5px 0px;

}

::v-deep(.ant-table-tbody > tr > td) {
    padding: 5px 0px;
}

::v-deep(.ant-card-head) {
    padding: 0px;
    font-size: 14px;
    min-height: 0px;
}

::v-deep(.ant-card-head-title) {
    padding: 0px;
    white-space: normal;
}

::v-deep(.ant-card-extra) {
    padding: 0px;
}

::v-deep(.ant-card-body) {
    padding: 0 2px;
}

::v-deep(.ant-input-number-input) {
    height: 22px;
}

::v-deep(.ant-table-header.ant-table-hide-scrollbar) {
    margin-bottom: -20px;
    padding-bottom: 10px;
    overflow: scroll;
    opacity: 1;
}

::v-deep(.ant-table-hide-scrollbar) {
    scrollbar-color: initial !important;
}
::v-deep(.ant-table-placeholder){
    padding: 0 0px;
}
p {
    margin-bottom: 0em
}

/* 已组盘信息区域样式 */
.box-info-section {
    margin: 10px;
    padding-bottom: 70px;
}

/* 物料信息区域样式 */
.materials-section {
    margin-top: 10px;
}

.material-card {
    background: #ffffff;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    margin-bottom: 15px;
    overflow: hidden;
}

.material-header {
    background: #f5f5f5;
    padding: 10px 15px;
    border-bottom: 1px solid #e8e8e8;
    font-weight: 500;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.delete-btn {
    font-size: 12px !important;
    padding: 0 !important;
    color: #ff4d4f !important;
}

.delete-btn:hover {
    color: #ff7875 !important;
}

.material-content {
    padding: 15px;
}

.material-row {
    display: flex;
    justify-content: space-between;
    margin-bottom: 8px;
    font-size: 14px;
}

.material-row:last-child {
    margin-bottom: 0;
}

/* 无物料信息时的样式 */
.no-materials {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 40px 0;
    color: #999;
}

.empty-icon {
    margin-bottom: 10px;
    color: #d9d9d9;
}

/* 添加物料按钮样式 */
.add-material-btn {
    display: flex;
    justify-content: center;
    margin-top: 15px;
}

.add-material-btn .ant-btn {
    height: 40px;
    font-size: 14px;
    width: 80%;
    max-width: 200px;
}

/* 底部按钮样式调整 */
.tab-bar {
    display: flex;
    justify-content: space-around;
    align-items: center;
    position: fixed;
    left: 0;
    right: 0;
    bottom: 0;
    height: 60px;
    background: #ffffff;
    border-top: 1px solid #f0f0f0;
    box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.1);
    padding: 0 20px;
    z-index: 1000;
}

.tab-bar .modern-btn {
    width: 45%;
    max-width: 150px;
}
</style>
