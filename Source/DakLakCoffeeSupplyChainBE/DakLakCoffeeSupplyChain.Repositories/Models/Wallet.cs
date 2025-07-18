﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Repositories.Models;

public partial class Wallet
{
    public Guid WalletId { get; set; }

    public Guid? UserId { get; set; }

    public string WalletType { get; set; }

    public double TotalBalance { get; set; }

    public DateTime LastUpdated { get; set; }

    public bool IsDeleted { get; set; }

    public virtual UserAccount User { get; set; }

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}