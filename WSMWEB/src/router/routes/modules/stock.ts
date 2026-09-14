import type { AppRouteModule } from '/@/router/types';
import { LAYOUT } from '/@/router/constant';
import { t } from '/@/hooks/web/useI18n';

const stock: AppRouteModule = {
  path: '/stock',
  name: 'Stock',
  component: LAYOUT,
  //redirect: '/admin/abpUser',
  meta: {
    orderNo: 50,
    icon: 'ant-design:database-outlined',
    title: t('routes.warehouse.stockManagement'),
  },
  children: [
    // {
    //   path: 'storageBoxDetail',
    //   name: 'StorageBoxDetail',
    //   component: () => import('/@/views/warehouse/storageBoxDetails/StorageBoxDetail.vue'),
    //   meta: {
    //     title: t('routes.warehouse.storageBoxDetailManagement'),
    //     policy: 'WarehouseManagement.StorageBoxManagement',
    //     icon: 'ant-design:file-search-outlined',
    //   },
    // },
  ],
};

export default stock;
