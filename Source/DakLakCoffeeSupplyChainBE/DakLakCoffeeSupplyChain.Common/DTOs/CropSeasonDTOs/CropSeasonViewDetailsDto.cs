// File: Common/DTOs/CropSeasonDTOs/CropSeasonViewDetailsDto.cs
using System;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs
{
    public class CropSeasonViewDetailsDto
    {
        public Guid CropSeasonId { get; set; }
        public string SeasonName { get; set; } = string.Empty;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public double? Area { get; set; }
        public string Note { get; set; } = string.Empty;

        public Guid FarmerId { get; set; }
        public string FarmerName { get; set; } = string.Empty;

        public Guid CommitmentId { get; set; }
        public Guid RegistrationId { get; set; }
    }
}
