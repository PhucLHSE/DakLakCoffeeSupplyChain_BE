﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Repositories.Models;

public partial class Contract
{
    [Key]
    public Guid ContractId { get; set; }

    public string ContractCode { get; set; }

    public Guid SellerId { get; set; }

    public Guid BuyerId { get; set; }

    public string ContractNumber { get; set; }

    public string ContractTitle { get; set; }

    public string ContractFileUrl { get; set; }

    public int? DeliveryRounds { get; set; }

    public double? TotalQuantity { get; set; }

    public double? TotalValue { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTime? SignedAt { get; set; }

    public string Status { get; set; }

    public string CancelReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual BusinessBuyer Buyer { get; set; }

    public virtual ICollection<ContractDeliveryBatch> ContractDeliveryBatches { get; set; } = new List<ContractDeliveryBatch>();

    public virtual ICollection<ContractItem> ContractItems { get; set; } = new List<ContractItem>();

    public virtual BusinessManager Seller { get; set; }
}