import type { AppRouteModule } from '/@/router/types';
import { LAYOUT } from '/@/router/constant';
import { t } from '/@/hooks/web/useI18n';

const material: AppRouteModule = {
  path: '/material',
  name: 'Material',
  component: LAYOUT,
  //redirect: '/admin/abpUser',
  meta: {
    orderNo: 20,
    icon: 'ant-design:profile-outlined',
    title: t('routes.material.materialManagement'),
  },
  children: [
    {
      path: 'goods',
      name: 'Material',
      component: () => import('/@/views/warehouse/goods/Good.vue'),
      meta: {
        title: t('routes.material.goodsManagement'),
        policy: 'Wms.Read',
        icon: 'ant-design:profile-outlined',
      },
    },
  ],
};

export default material;
