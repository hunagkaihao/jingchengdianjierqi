using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using TuTa.Wms.Application.Contracts.Shared;
using Wms.LogTool;
using TuTa.Wms.Materials.Aggregates;
using TuTa.Wms.Materials.Dtos;
using Microsoft.AspNetCore.Authorization;
using TuTa.Wms.Permissions;

namespace TuTa.Wms.Materials
{
    //[Authorize]
    public class MaterialService : WmsAppService, IMaterialService
    {
        private readonly IMaterialRepository _materialRepository;
        private readonly MaterialManager _materialManager;
        private readonly ILogger<MaterialService> _logger;

        public MaterialService(
            IMaterialRepository materialRepository,
            MaterialManager materialManager, 
            ILogger<MaterialService> logger)
        {
            _materialRepository = materialRepository;
            _materialManager = materialManager;
            _logger = logger;
        }

        //[Authorize(WmsPermissions.AddPermission)]
        public async Task<ResponseDto> CreateMaterialAsync(MaterialCreateDto para)
        {
            try
            {
                var goods = await _materialManager.CreateMaterialAsync(para.MaterialCode, para.MaterialName, para.Specs, para.Unit, para.TypeCode, para.TypeName,
                    para.IsHB, para.SafetyStock, para.FullBoxCount, para.ExpiryDate, para.IsQCPJ, para.IsPPAP,null,null,null,false,null).ConfigureAwait(false);

                await _materialRepository.InsertAsync(goods).ConfigureAwait(false);
                return new ResponseDto() { success = true, message = "添加物料定义成功" };
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw new UserFriendlyException(ex.Message);
            }
        }

        public async Task<ResponseDto> DeleteMaterialAsync(string materialCodeToDel)
        {
            try
            {
                var goodsExist = await _materialRepository.FindByMaterialCodeAsync(materialCodeToDel).ConfigureAwait(false);
                if (goodsExist == null)
                {
                    return new ResponseDto() { success = true, message = $"物料码为{materialCodeToDel}的物料不存在，默认删除成功" };
                }
                await _materialRepository.DeleteAsync(goodsExist).ConfigureAwait(false);
                return new ResponseDto() { success = true, message = "删除物料定义成功" };
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw new UserFriendlyException(ex.Message);
            }
        }

        public async Task<ResponseDto> UpdateMaterialAsync(Guid materialIdToUpdate, MaterialUpdateDto para)
        {
            try
            {
                Material materialExist = await _materialRepository.FindAsync(materialIdToUpdate).ConfigureAwait(false);
                if (materialExist == null)
                {
                    throw new Exception($"Id为{materialIdToUpdate}的物料定义不存在");
                }
                await _materialManager.ModifyMaterialAsync(
                    materialExist, 
                    para.MaterialCodeNew, 
                    para.MaterialNameNew, 
                    para.SpecsNew, 
                    para.UnitNew, 
                    para.TypeCodeNew, 
                    para.TypeNameNew,
                    para.IsHBNew, 
                    para.SafetyStockNew, 
                    para.FullBoxCount,
                    para.ExpiryDateNew, 
                    para.IsQCPJNew, 
                    para.IsPPAPNew,null,null,null,false,null).ConfigureAwait(false);

                await _materialRepository.UpdateAsync(materialExist).ConfigureAwait(false);
                return new ResponseDto() { success = true, message = "更新物料定义成功" };
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw new UserFriendlyException(ex.Message);
            }
        }

        public async Task<PagedResultDto<MaterialDto>> GetPagedMaterialsAsync(PagedMaterialsQueryDto para)
        {
            try
            {
                var pagedGoods = await _materialRepository.GetPagedMaterialsAsync(
                    para.MaterialCode,
                    para.MaterialName,
                    para.Specs,
                    para.Unit,
                    para.TypeCode,
                    para.TypeName,
                    para.IsHB,
                    para.SafetyStock,
                    para.ExpiryDate,
                    para.IsQCPJ,
                    para.IsPPAP,
                    para.SkipCount,
                    para.MaxResultCount).ConfigureAwait(false);

                PagedResultDto<MaterialDto> result = new PagedResultDto<MaterialDto>()
                {
                    TotalCount = pagedGoods.TotalCount,
                    Items = ObjectMapper.Map<List<Material>, List<MaterialDto>>(pagedGoods.Items)
                };
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw new UserFriendlyException(ex.Message);
            }
        }

        public async Task<List<MaterialDto>> GetMaterialsByMaterialCodeTipAsync(string materialCodeTip)
        {
            try
            {
                var materials = await _materialRepository.GetMaterialsByCodeTipAsync(materialCodeTip, false).ConfigureAwait(false);

                return ObjectMapper.Map<List<Material>, List<MaterialDto>>(materials);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                throw new UserFriendlyException(ex.Message);
            }
        }
    }
}
