namespace DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs
{
    public class SystemConfigurationViewAllDto
    {
        //public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; } = string.Empty;

        public decimal? MinValue { get; set; }

        public decimal? MaxValue { get; set; }

        public string? Unit { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime EffectedDateFrom { get; set; }

        public DateTime? EffectedDateTo { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string TargetEntity { get; set; } = string.Empty;

        public string TargetField { get; set; } = string.Empty;

        public string Operator { get; set; } = string.Empty;

        public string ScopeType { get; set; } = string.Empty;

        public Guid? ScopeId { get; set; }

        public string Severity { get; set; } = string.Empty;

        public string RuleGroup { get; set; } = string.Empty;

        public int? VersionNo { get; set; }

        public Guid? CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }
    }
}
