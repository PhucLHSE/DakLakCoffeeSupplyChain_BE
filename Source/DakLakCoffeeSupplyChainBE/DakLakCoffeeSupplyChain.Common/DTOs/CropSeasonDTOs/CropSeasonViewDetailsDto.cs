using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs.DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CropSeasonEnums;
using System.Text.Json.Serialization;

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
    public string CommitmentCode { get; set; } = string.Empty;

    public Guid RegistrationId { get; set; }
    public string RegistrationCode { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CropSeasonStatus Status { get; set; }

    public List<CropSeasonDetailViewDto> Details { get; set; } = new();
}

