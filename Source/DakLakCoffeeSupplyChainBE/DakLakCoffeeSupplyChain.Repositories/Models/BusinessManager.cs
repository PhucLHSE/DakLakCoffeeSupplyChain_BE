﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DakLakCoffeeSupplyChain.Repositories.Models;

public partial class BusinessManager
{
    [Key]
    public Guid ManagerId { get; set; }

    public Guid UserId { get; set; }

    public string ManagerCode { get; set; }

    public string CompanyName { get; set; }

    public string Position { get; set; }

    public string Department { get; set; }

    public string CompanyAddress { get; set; }

    public string TaxId { get; set; }

    public string Website { get; set; }

    public string ContactEmail { get; set; }

    public string BusinessLicenseUrl { get; set; }

    public bool? IsCompanyVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<BusinessBuyer> BusinessBuyers { get; set; } = new List<BusinessBuyer>();

    public virtual ICollection<BusinessStaff> BusinessStaffs { get; set; } = new List<BusinessStaff>();

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<CultivationRegistrationsDetail> CultivationRegistrationsDetails { get; set; } = new List<CultivationRegistrationsDetail>();

    public virtual ICollection<FarmingCommitment> FarmingCommitments { get; set; } = new List<FarmingCommitment>();

    public virtual ICollection<OrderComplaint> OrderComplaints { get; set; } = new List<OrderComplaint>();

    public virtual ICollection<ProcurementPlan> ProcurementPlans { get; set; } = new List<ProcurementPlan>();

    public virtual UserAccount User { get; set; }

    public virtual ICollection<WarehouseOutboundRequest> WarehouseOutboundRequests { get; set; } = new List<WarehouseOutboundRequest>();

    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}