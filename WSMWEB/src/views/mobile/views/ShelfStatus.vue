<template>
  <div class="shelf-status">
    <Header numb="货架状态" />
    <div class="toolbar"><span>最后刷新：{{ refreshedAt || '-' }}</span><a-button type="primary" :loading="loading" @click="loadStatus">刷新状态</a-button></div>
    <div class="legend"><span class="stocked">有货（0）</span><span class="empty">无货（1）</span></div>
    <a-spin :spinning="loading">
      <a-card v-for="row in rows" :key="row.rowName" :title="`${row.rowName}（${row.deviceAddress}）`" class="row-card">
        <a-alert v-if="!row.isSuccess" type="error" show-icon :message="`采集失败：${row.errorMessage || '设备离线或响应异常'}`" />
        <div v-else class="shelves">
          <div v-for="shelf in grouped(row.layers)" :key="shelf.number" class="shelf-column">
            <b>{{ shelf.number }}</b>
            <div v-for="layer in shelf.layers.slice().sort((left, right) => right.layer - left.layer)" :key="layer.shelfCode" :class="['layer', layer.status === 0 ? 'stocked' : 'empty']">{{ layer.layer }}层 {{ layer.status === 0 ? '有货' : '无货' }}</div>
          </div>
        </div>
      </a-card>
    </a-spin>
  </div>
</template>
<script lang="ts" setup>
import { onMounted, ref } from 'vue';
import { message } from 'ant-design-vue';
import Header from '../header/Header.vue';
import { defHttp } from '/@/utils/http/axios';
interface Layer { shelfCode: string; shelfNumber: string; layer: number; diIndex: number; status: number; }
interface Row { rowName: string; deviceAddress: string; isSuccess: boolean; errorMessage?: string; layers: Layer[]; }
const rows = ref<Row[]>([]); const refreshedAt = ref(''); const loading = ref(false);
const grouped = (layers: Layer[]) => Object.values(layers.reduce((map: Record<string, { number: string; layers: Layer[] }>, layer) => { (map[layer.shelfNumber] ||= { number: layer.shelfNumber, layers: [] }).layers.push(layer); return map; }, {}));
async function loadStatus() { loading.value = true; try { const data = await defHttp.get<any>({ url: '/wms/shelf-status/current' }, { isTransformResponse: false }); rows.value = data?.rows || []; refreshedAt.value = data?.refreshedAt || ''; } catch { message.error('货架状态读取失败'); } finally { loading.value = false; } }
onMounted(loadStatus);
</script>
<style scoped lang="less">
.shelf-status{min-height:100vh;background:#f5f6fa}.toolbar,.legend{display:flex;gap:12px;align-items:center;padding:12px;background:#fff}.toolbar .ant-btn{margin-left:auto}.row-card{margin:12px}.shelves{display:flex;gap:8px;overflow-x:auto}.shelf-column{min-width:92px;text-align:center}.layer{margin:5px 0;padding:7px 3px;border-radius:4px}.stocked{color:#237804;background:#f6ffed}.empty{color:#a8071a;background:#fff1f0}.legend span{padding:4px 8px;border-radius:4px}
</style>
