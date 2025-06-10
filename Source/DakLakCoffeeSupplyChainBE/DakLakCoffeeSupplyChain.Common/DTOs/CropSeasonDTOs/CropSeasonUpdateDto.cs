using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs
{
    public class CropSeasonUpdateDto
    {
        [Required]
        public Guid CropSeasonId { get; set; }

        [Required]
        public Guid FarmerId { get; set; }

        [Required]
        public Guid RegistrationId { get; set; }

        [Required]
        public Guid CommitmentId { get; set; }

        [Required]
        public string SeasonName { get; set; }

        public double? Area { get; set; }

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        public string? Note { get; set; }

        public List<CropSeasonDetailCreateDto> Details { get; set; } = new();
    }

}
