﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Repositories.Models;

public partial class FarmingCommitmentsDetail
{
    [Key]
    public Guid CommitmentDetailId { get; set; }

    public string CommitmentDetailCode { get; set; }

    public Guid CommitmentId { get; set; }

    public Guid RegistrationDetailId { get; set; }

    public Guid PlanDetailId { get; set; }

    public double? ConfirmedPrice { get; set; }

    public double? CommittedQuantity { get; set; }

    public DateOnly? EstimatedDeliveryStart { get; set; }

    public DateOnly? EstimatedDeliveryEnd { get; set; }

    public string Note { get; set; }

    public Guid? ContractDeliveryItemId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual FarmingCommitment Commitment { get; set; }

    public virtual ContractDeliveryItem ContractDeliveryItem { get; set; }

    public virtual ICollection<CropSeasonDetail> CropSeasonDetails { get; set; } = new List<CropSeasonDetail>();

    public virtual ProcurementPlansDetail PlanDetail { get; set; }

    public virtual CultivationRegistrationsDetail RegistrationDetail { get; set; }
}