﻿using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ProcessingEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchDTOs
{
    public class ProcessingBatchViewDto
    {
        public Guid BatchId { get; set; }
        public string BatchCode { get; set; }
        public string SystemBatchCode { get; set; }
        public Guid CropSeasonId { get; set; }
        public string CropSeasonName { get; set; }  
        public Guid FarmerId { get; set; }
        public string FarmerName { get; set; }

        public int MethodId { get; set; }
        public string MethodName { get; set; }

        public int StageCount { get; set; }
        public double? TotalInputQuantity { get; set; }
        public double TotalOutputQuantity { get; set; }

        public ProcessingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProcessingBatchProgressViewAllDto> Progresses { get; set; }
    }

}
