﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Repositories.Models;

public partial class CropSeason
{
    [Key]
    public Guid CropSeasonId { get; set; }

    public string CropSeasonCode { get; set; }

    public Guid FarmerId { get; set; }

    public Guid CommitmentId { get; set; }

    public string SeasonName { get; set; }

    public double? Area { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Note { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual FarmingCommitment Commitment { get; set; }

    public virtual ICollection<CropSeasonDetail> CropSeasonDetails { get; set; } = new List<CropSeasonDetail>();

    public virtual Farmer Farmer { get; set; }

    public virtual ICollection<ProcessingBatch> ProcessingBatches { get; set; } = new List<ProcessingBatch>();
}