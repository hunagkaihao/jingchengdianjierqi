<template>
    <div class="box-bind-container">
        <Header numb="容器绑定"></Header>
        <a-row class="input-row">
            <a-col :span="6">
                <div class="htext">
                    <h1 autofocus="autofocus">绑定容器:</h1>
                </div>
            </a-col>
            <a-col :span="17">
                <a-input v-model:value="boxCode" placeholder="扫描容器码" @keyup.enter="scanboxCode" ref="Ref1" :allowClear="true"
                    @focus="focusFn" class="modern-input">
                    <template #suffix>
                        <scan-outlined class="scan-icon" />
                    </template>
                </a-input>
            </a-col>
        </a-row>
        <a-row class="input-row">
            <a-col :span="6">
                <div class="htext">
                    <h1 autofocus="autofocus">绑定库位:</h1>
                </div>
            </a-col>
            <a-col :span="17">
                <a-input v-model:value="cellCode" placeholder="扫描库位" @keyup.enter="scancellCode" ref="Ref2"
                    :allowClear="true" @focus="focusFn" class="modern-input">
                    <template #suffix>
                        <scan-outlined class="scan-icon" />
                    </template>
                </a-input>
            </a-col>
        </a-row>

        <div v-show="showtable">
            <p style="margin-left: 20px">容器物料信息:{{ dataSource.length }}</p>
            
            <!-- 物料信息显示 -->
            <div class="materials-section">
                <div v-if="dataSource.length > 0">
                    <div v-for="(material, index) in dataSource" :key="index" class="material-card">
                        <div class="material-header">
                            <span>产品名称: {{ material.materialName }}</span>
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
            </div>

            <div class="tab-bar">
                <a-button @click="unbindcell" type="primary" class="modern-btn">
                    解绑确认
                </a-button>
                <a-button @click="boxbindcell" type="primary" class="modern-btn">
                    绑定确认
                </a-button>
            </div>
        </div>
    </div>
    <GoodsDetail @register="registerGoodsDetailModal"></GoodsDetail>
</template>
<script lang="ts" setup>
import { ref, onMounted } from 'vue';
import { ScanOutlined } from '@ant-design/icons-vue';
import { message } from 'ant-design-vue';
import { StockServiceProxy } from '/@/services/ServiceProxies';
import Header from '../header/Header.vue'
import { useModal } from '/@/components/Modal';

// 创建服务代理实例
const stockService = new StockServiceProxy();

// 获取容器内物料
const stocksGetInBox = async (boxCode: string) => {
  try {
    const res = await stockService.stocksGetInBox(boxCode);
    return res;
  } catch (error) {
    message.error('获取容器物料失败');
    return [];
  }
};

// 容器绑定库位
const boxBindCell = async (boxCode: string, cellCode: string) => {
  try {
    const res = await stockService.boxBindCell(boxCode, cellCode);
    return res;
  } catch (error) {
    message.error('绑定失败');
    return null;
  }
};

// 容器解绑库位
const boxDisBindCell = async (boxCode: string, cellCode: string) => {
  try {
    const res = await stockService.boxDisBindCell(boxCode, cellCode);
    return res;
  } catch (error) {
    message.error('解绑失败');
    return null;
  }
};



// 临时的GoodsDetail组件
const GoodsDetail = {
  template: '<div class="goods-detail-modal"><slot></slot></div>',
  props: ['record'],
  emits: ['register'],
};

const [registerGoodsDetailModal, { openModal: openGoodsDetailModal }] = useModal();
let boxCode = ref<string>('');
let cellCode = ref<string>('');
var dataSource = ref([]
);
let screenHeight = ref(window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight);
let screenWidth = ref((window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth) - 8)
let showtable = ref(true)
const Ref1 = ref()
const Ref2 = ref()
onMounted(() => {
    Ref1.value.focus()
})

//扫码容器码
const scanboxCode = async () => {
    await stocksGetInBox(boxCode.value).then((res) => {
        dataSource.value = res
        Ref2.value.focus()
    }).catch((err) => {
        message.error(err.error?.message || '获取容器物料失败')
    })
}

const scancellCode = async () => {


}
const boxbindcell = async()=>{
    let res = await boxBindCell(boxCode.value,cellCode.value)
    if(res.success == true){
        message.success(res.message)
        boxCode.value = ''
        cellCode.value = ''
        dataSource.value = []
        Ref1.value.focus()
    }else
    {
        message.error(res.message)
    }
}
const unbindcell = async()=>{
    let res = await boxDisBindCell(boxCode.value,cellCode.value)
    if(res.success == true){
        message.success(res.message)
        boxCode.value = ''
        cellCode.value = ''
        dataSource.value = []
        Ref1.value.focus()
    }else
    {
        message.error(res.message)
    }
};


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
.box-bind-container {
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
    padding-bottom: 70px;
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
