﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Repositories.Models;

public partial class WarehouseOutboundReceipt
{
    [Key]
    public Guid OutboundReceiptId { get; set; }

    public string OutboundReceiptCode { get; set; }

    public Guid OutboundRequestId { get; set; }

    public Guid WarehouseId { get; set; }

    public Guid InventoryId { get; set; }

    public Guid BatchId { get; set; }

    public double Quantity { get; set; }

    public Guid ExportedBy { get; set; }

    public DateTime? ExportedAt { get; set; }

    public string DestinationNote { get; set; }

    public string Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ProcessingBatch Batch { get; set; }

    public virtual BusinessStaff ExportedByNavigation { get; set; }

    public virtual Inventory Inventory { get; set; }

    public virtual WarehouseOutboundRequest OutboundRequest { get; set; }

    public virtual Warehouse Warehouse { get; set; }
}