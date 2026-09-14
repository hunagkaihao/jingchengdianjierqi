import type { AppRouteRecordRaw, AppRouteModule } from '/@/router/types';

import { PAGE_NOT_FOUND_ROUTE, REDIRECT_ROUTE } from '/@/router/routes/basic';
import { mainOutRoutes } from './mainOut';
import { PageEnum } from '/@/enums/pageEnum';
import { t } from '/@/hooks/web/useI18n';

const modules = import.meta.globEager('./modules/**/*.ts');

const routeModuleList: AppRouteModule[] = [];

Object.keys(modules).forEach((key) => {
  const mod = modules[key].default || {};
  const modList = Array.isArray(mod) ? [...mod] : [mod];
  routeModuleList.push(...modList);
});

export const asyncRoutes = [PAGE_NOT_FOUND_ROUTE, ...routeModuleList];

export const RootRoute: AppRouteRecordRaw = {
  path: '/',
  name: 'Root',
  redirect: PageEnum.BASE_HOME,
  meta: {
    title: 'Root',
  },
};

export const LoginRoute: AppRouteRecordRaw = {
  path: '/login',
  name: 'Login',
  component: () => import('/@/views/sys/login/Login.vue'),
  meta: {
    title: t('routes.basic.login'),
  },
};
//新增移动端登录
export const MobileLoginRoute: AppRouteRecordRaw = {
  path: '/mobilelogin',
  name: 'MobileLogin',
  component: () => import('/@/views/mobile/login/Login.vue'),
  meta: {
    title: t('routes.basic.login'),
    ignoreAuth: true,
  },
};

//新增移动端主页
export const MobileHomeRoute: AppRouteRecordRaw = {
  path: '/mobilehome',
  name: 'MobileHome',
  component: () => import('/@/views/mobile/home/home.vue'),
  meta: {
    title: t('主页'),
    ignoreAuth: true,
  },
};


//人工入库
export const IncellByPeople: AppRouteRecordRaw = {
  path: '/incellByPeople',
  name: 'IncellByPeople',
  component: () => import('/@/views/mobile/views/IncellByPeople.vue'),
  meta: {
    title: t('人工入库'),
    ignoreAuth: true,
  },
};
//超期复检入库
// IncellByOverdue路由已删除
// IncellByOverdueCall路由已删除
//领用通知
export const AcceptanceCall: AppRouteRecordRaw = {
  path: '/acceptanceCall',
  name: 'AcceptanceCall',
  component: () => import('/@/views/mobile/views/AcceptanceCall.vue'),
  meta: {
    title: t('领用通知'),
    ignoreKeepAlive: true,
    ignoreAuth: true,
  },
};
//检验入库领用通知
export const AcceptanceCall2: AppRouteRecordRaw = {
  path: '/acceptanceCall2',
  name: 'AcceptanceCall2',
  component: () => import('/@/views/mobile/views/AcceptanceCall2.vue'),
  meta: {
    title: t('检验入库领用通知'),
    ignoreKeepAlive: true,
    ignoreAuth: true,
  },
};
// AcceptanceOut路由已删除
//检验入库领用
// AcceptanceOut2路由已删除
// OverdueCall路由已删除
// OverdueOut路由已删除
// TiaoboIncell路由已删除
//移库
export const cellStock: AppRouteRecordRaw = {
  path: '/cellStock',
  name: 'cellStock',
  component: () => import('/@/views/mobile/views/Stock.vue'),
  meta: {
    title: t('移库'),
    ignoreAuth: true,
  },
};
//agv托盘组盘入库
export const AGVIncell: AppRouteRecordRaw = {
  path: '/agvIncell',
  name: 'AGVIncell',
  component: () => import('/@/views/mobile/views/AGVIncell.vue'),
  meta: {
    title: t('agv托盘组盘入库'),
    ignoreAuth: true,
  },
};

//设置库位状态
export const SetCellStatus: AppRouteRecordRaw = {
  path: '/setCellStatus',
  name: 'SetCellStatus',
  component: () => import('/@/views/mobile/views/SetCellStatus.vue'),
  meta: {
    title: t('设置库位状态'),
    ignoreAuth: true,
  },
};
//容器配送
export const BoxIncell: AppRouteRecordRaw = {
  path: '/boxIncell',
  name: 'BoxIncell',
  component: () => import('/@/views/mobile/views/BoxIncell.vue'),
  meta: {
    title: t('容器配送'),
    ignoreAuth: true,
  },
};
//物料抽检
// GoodSpotCheck路由已删除
// EmptyShelfEdit路由已删除
// GoodsDevan路由已删除
// GoodsAdd路由已删除
// EmptyBoxOut路由已删除
// BindStation路由已删除
// CreateAgvTask路由已删除
// CreateAgvBackTask路由已删除
// ZZOutCell路由已删除
//领用物料绑定
export const GoodsBind: AppRouteRecordRaw = {
  path: '/goodsBind',
  name: 'GoodsBind',
  component: () => import('/@/views/mobile/views/GoodsBind.vue'),
  meta: {
    title: t('领用物料绑定'),
    ignoreAuth: true,
  },
};
// ShelfIncellList路由已删除
//整车入库
// ShelfIncell路由已删除
// Tiaobo路由已删除

// Handincell路由已删除
export const Handoutcell: AppRouteRecordRaw = {
  path: '/handoutcell',
  name: 'HandOutCell',
  component: () => import('/@/views/mobile/views/HandOutCell.vue'),
  meta: {
    title: t('物料调拨'),
    ignoreAuth: true,
  },
};
// Boxoutcell路由已删除
// DPTiaobo路由已删除
// DPCall路由已删除
//检验查询
export const CheckNoGet: AppRouteRecordRaw = {
  path: '/checkNoGet',
  name: 'CheckNoGet',
  component: () => import('/@/views/mobile/views/CheckNoGet.vue'),
  meta: {
    title: t('整车清单'),
    ignoreAuth: true,
  },
};
//空托呼叫
export const EmptyTrayCall: AppRouteRecordRaw = {
  path: '/emptyTrayCall',
  name: 'EmptyTrayCall',
  component: () => import('/@/views/mobile/views/EmptyTrayCall.vue'),
  meta: {
    title: t('空托呼叫'),
    ignoreKeepAlive: true,
    ignoreAuth: true,
  },
};

//空托送回
export const EmptyTrayReturn: AppRouteRecordRaw = {
  path: '/emptyTrayReturn',
  name: 'EmptyTrayReturn',
  component: () => import('/@/views/mobile/views/EmptyTrayReturn.vue'),
  meta: {
    title: t('空托送回'),
    ignoreKeepAlive: true,
    ignoreAuth: true,
  },
};
//移动端任务管理
export const MobileTaskManage: AppRouteRecordRaw = {
  path: '/mobileTaskManage',
  name: 'MobileTaskManage',
  component: () => import('/@/views/mobile/views/MobileTaskManage.vue'),
  meta: {
    title: t('任务管理'),
    ignoreKeepAlive: true,
    ignoreAuth: true,
  },
};
//移动端机台状态
export const MobileMachineStatus: AppRouteRecordRaw = {
  path: '/mobileMachineStatus',
  name: 'MobileMachineStatus',
  component: () => import('/@/views/mobile/views/MobileMachineStatus.vue'),
  meta: {
    title: t('机台状态'),
    ignoreKeepAlive: true,
    ignoreAuth: true,
  },
};
//移动端机台配置
export const MobileMachineConfig: AppRouteRecordRaw = {
  path: '/mobileMachineConfig',
  name: 'MobileMachineConfig',
  component: () => import('/@/views/mobile/views/MobileMachineConfig.vue'),
  meta: {
    title: t('机台配置'),
    ignoreKeepAlive: true,
    ignoreAuth: true,
  },
};
// AcceptanceOuttest路由已删除
// AcceptanceOutByPerson路由已删除
// GoodsBindtest路由已删除
// ZZOutCelltest路由已删除
// HandOutCelltest路由已删除
// OutCellAndBindShelf路由已删除
// Basic routing without permission
export const basicRoutes = [
  LoginRoute,
  MobileLoginRoute,
  MobileHomeRoute,
  RootRoute,
  ...mainOutRoutes,
  REDIRECT_ROUTE,
  PAGE_NOT_FOUND_ROUTE,
  IncellByPeople,
  // IncellByOverdue已删除
  // IncellByOverdueCall已删除
  AcceptanceCall,
  // AcceptanceOut已删除
  // OverdueCall已删除
  CheckNoGet,
  // OverdueOut已删除
  // TiaoboIncell已删除
  cellStock,
  AGVIncell,
  BoxIncell,
  SetCellStatus,
  // Boxoutcell已删除
  // EmptyShelfEdit已删除
  // GoodSpotCheck已删除
  // GoodsDevan已删除
  // BindStation已删除
  // CreateAgvTask已删除
  // CreateAgvBackTask已删除
  // ZZOutCell已删除
  GoodsBind,
  AcceptanceCall2,
  MobileMachineStatus,
  MobileMachineConfig,
  // AcceptanceOut2已删除
  // ShelfIncellList已删除
  // ShelfIncell已删除
  // Tiaobo已删除
  // GoodsAdd已删除
  // EmptyBoxOut已删除
  // Handincell已删除
  Handoutcell,
  // DPTiaobo已删除
  // DPCall已删除
  // AcceptanceOuttest已删除
  // GoodsBindtest已删除
  // ZZOutCelltest已删除
  // HandOutCelltest已删除
  EmptyTrayCall,
  EmptyTrayReturn,
  MobileTaskManage,
  // GoodAndBoxBind已删除
  // AcceptanceOutByPerson已删除
  // OutCellAndBindShelf已删除
];
