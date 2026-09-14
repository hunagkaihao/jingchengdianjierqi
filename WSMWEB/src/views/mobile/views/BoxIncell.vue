<template>
    <div class="box-incell-container">
        <Header numb="容器配送"></Header>
        <!-- 入库料箱输入框 -->
        <a-row class="input-row">
            <a-col :span="6">
                <div class="htext">
                    <h1 autofocus="autofocus">入库料箱:</h1>
                </div>
            </a-col>
            <a-col :span="17">
            <a-input v-model:value="boxCode" placeholder="扫描料箱码" @keyup.enter="scanboxCode" :allowClear="true"
                @focus="focusFn" class="modern-input">
                <template #suffix>
                    <scan-outlined class="scan-icon" />
                </template>
            </a-input>
        </a-col>
    </a-row>
    <!-- 目标库位输入框 -->
    <a-row class="input-row">
        <a-col :span="6">
            <div class="htext">
                <h1 autofocus="autofocus">目标库位:</h1>
            </div>
        </a-col>
        <a-col :span="18">
            <a-input v-model:value="endcellCode" placeholder="目标库位" :allowClear="true"
                @focus="focusFn" class="modern-input">
                <template #suffix>
                    <scan-outlined class="scan-icon" />
                </template>
            </a-input>
        </a-col>
    </a-row>

        <div v-show="showtable">
            <p style="margin-left: 20px">容器物料信息:{{ dataSource.length }}</p>
            <a-table ref="tableRef" :dataSource="dataSource" :columns="columns" :pagination="false"
                :scroll="{ x: screenWidth, y: 128 }">
                <template #bodyCell="{ column, record }">
                    <template v-if="column.key === 'operation'">
                        <span @click="OpenGoodsDetail(record)">查看</span>
                    </template>
                </template>
            </a-table>


            <div class="tab-bar">
                <a-button @click="incell" type="primary" class="modern-btn">
                    配送
                </a-button>

            </div>
        </div>
    </div>
    <GoodsDetail @register="registerGoodsDetailModal"></GoodsDetail>
</template>
<script lang="ts" setup>
import { ref,  onMounted } from 'vue';
import { ScanOutlined } from '@ant-design/icons-vue';
import { message } from 'ant-design-vue';
import { StockServiceProxy } from '/@/services/ServiceProxies';
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

// 下发配送任务
const createDeliveryTask = async (boxCode: string) => {
  try {
    const res = await stockService.createDeliveryTask(boxCode);
    return res;
  } catch (error) {
    message.error('下发配送任务失败');
    return null;
  }
};

// 已删除的AGVIncell.ts文件，使用内联实现
// const CreateCtuBasicIn = async (boxCode: string) => {
//   message.info('功能已禁用');
//   return null;
// };
const getByStock = async () => {
  message.info('功能已禁用');
  return null;
};
import Header from '../header/Header.vue'
import { useModal } from '/@/components/Modal';
// 已删除的IncellByPeople.ts文件，使用内联实现
const columns = [
  {
    title: '物料编码',
    dataIndex: 'materialCode',
    key: 'materialCode',
  },
  {
    title: '物料名称',
    dataIndex: 'materialName',
    key: 'materialName',
  },
  {
    title: '批次',
    dataIndex: 'batch',
    key: 'batch',
  },
  {
    title: '数量',
    dataIndex: 'qty',
    key: 'qty',
  },
  {
    title: '单位',
    dataIndex: 'unit',
    key: 'unit',
  },
  {
    title: '操作',
    key: 'operation',
  },
];
// 已删除的GoodsDetail.vue文件，使用简单的替代组件
const GoodsDetail = { template: '<div>功能已禁用</div>' };


const [registerGoodsDetailModal, { openModal: openGoodsDetailModal }] = useModal();
let boxCode = ref<string>('');
let endcellCode = ref<string>('');
var goods = ref([]
);
var dataSource = ref([]
);
let screenHeight = ref(window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight);
let screenWidth = ref((window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth) - 8)
let YHOne = ref();
let showtable = ref(true)
let goodheight = ref(36)
const tableRef = ref<any>();
onMounted(() => {
    if (tableRef.value && tableRef.value.$el) {
        console.log(tableRef.value.$el.querySelector('.ant-table-thead')?.clientHeight);
        YHOne.value = screenHeight.value - 42 - (tableRef.value.$el.querySelector('.ant-table-thead')?.clientHeight || 0);
    }
    window.onresize = () => {
        var showHeight = window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight
        console.log(showHeight - screenHeight.value)
        if (showHeight - screenHeight.value >= 0) {
            showtable.value = true
            goodheight.value = 36
        } else {
            showtable.value = false
            goodheight.value = 60
        }
    }
})

//扫码容器码
const scanboxCode = async () => {
    if(boxCode.value.length > 10){
        var le = boxCode.value.length
        boxCode.value = boxCode.value.slice(le-10,le)
    }
    await stocksGetInBox(boxCode.value).then((res) => {
        dataSource.value.length = 0
        res.forEach((e) => {
            dataSource.value.push(e)
        })
    }).catch((err) => {
        message.error('获取容器物料失败')
    })
    // 目标库位功能
    // if(endcellCode.value == ''){
    //     await getByStock('',boxCode.value).then((res)=>{
    //     endcellCode.value = res.cellCode
    //     })
    // }
}







const OpenGoodsDetail = (record: Recordable) => {
    openGoodsDetailModal(true, {
        record: record,
    });
};
const incell = async () => {

    try{
        await createDeliveryTask(boxCode.value).then((res) => {
            if (res && res.success == true) {
                message.success(res.message)
                goods.value.length = 0
                boxCode.value = ''
                endcellCode.value = ''
            } else if(res && res.success == false){
                message.error(res.message)
            }else{
                message.error("接口推送异常，配送失败")
            }
        }).catch((error) => {
            message.error('下发配送任务失败')
        })
    }catch(err){
        message.error('下发配送任务失败')
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
.box-incell-container {
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
</style>
