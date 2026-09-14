import type { AppRouteModule } from '/@/router/types';
import { LAYOUT } from '/@/router/constant';
import { t } from '/@/hooks/web/useI18n';

const productDelivery: AppRouteModule = {
  path: '/productDelivery',
  name: 'ProductDelivery',
  component: LAYOUT,
  meta: {
    orderNo: 20,
    icon: 'ant-design:appstore-outlined',
    title: t('产品名称配送'),
    ignoreAuth: true,
  },
  children: [
    {
      path: 'hotStamping',
      name: 'HotStamping',
      component: () => import('/@/views/productDelivery/hotStamping/index.vue'),
      meta: {
        title: t('烫金区'),
        icon: 'ant-design:fire-outlined',
        ignoreAuth: true,
      },
    },
    {
      path: 'slitting',
      name: 'Slitting',
      component: () => import('/@/views/productDelivery/slitting/index.vue'),
      meta: {
        title: t('分切区'),
        icon: 'ant-design:scissor-outlined',
        ignoreAuth: true,
      },
    },
  ],
};

export default productDelivery;