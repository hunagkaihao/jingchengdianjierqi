using Microsoft.Extensions.DependencyInjection;
using TuTa.Wms.Backgrounds;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.TenantManagement;
using Wms;

namespace TuTa.Wms;

[DependsOn(
    typeof(ToolsModule),
    typeof(WmsDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(WmsApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule)
    )]
public class WmsApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<WmsApplicationModule>();  
        });
        //context.Services.AddHostedService<PickListBackGroundService>();
        context.Services.AddHostedService<MachineStatusBackgroundService>();
    }
}
