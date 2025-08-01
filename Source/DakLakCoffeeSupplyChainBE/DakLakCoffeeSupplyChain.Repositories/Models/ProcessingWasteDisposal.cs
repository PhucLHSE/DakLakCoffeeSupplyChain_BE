﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Repositories.Models;

public partial class ProcessingWasteDisposal
{
    public Guid DisposalId { get; set; }

    public string DisposalCode { get; set; }

    public Guid WasteId { get; set; }

    public string DisposalMethod { get; set; }

    public Guid? HandledBy { get; set; }

    public DateTime HandledAt { get; set; }

    public string Notes { get; set; }

    public bool? IsSold { get; set; }

    public decimal? Revenue { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ProcessingBatchWaste Waste { get; set; }
}