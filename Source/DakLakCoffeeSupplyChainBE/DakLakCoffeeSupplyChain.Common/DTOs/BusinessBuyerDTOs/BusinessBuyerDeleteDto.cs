﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.BusinessBuyerDTOs
{
    public class BusinessBuyerDeleteDto
    {
        [Required]
        public Guid BuyerId { get; set; }
    }
}
