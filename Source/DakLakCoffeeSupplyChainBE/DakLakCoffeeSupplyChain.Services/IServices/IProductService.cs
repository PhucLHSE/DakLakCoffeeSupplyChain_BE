﻿using DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IProductService
    {
        Task<IServiceResult> GetAll(Guid userId);

        Task<IServiceResult> GetById(Guid productId, Guid userId);

        Task<IServiceResult> Create(ProductCreateDto productCreateDto, Guid userId);

        Task<IServiceResult> Update(ProductUpdateDto productUpdateDto, Guid userId);

        Task<IServiceResult> DeleteProductById(Guid productId, Guid userId);

        Task<IServiceResult> SoftDeleteProductById(Guid productId, Guid userId);
    }
}
