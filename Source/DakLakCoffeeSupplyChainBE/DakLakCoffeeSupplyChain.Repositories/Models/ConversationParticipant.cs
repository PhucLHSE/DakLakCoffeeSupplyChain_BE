﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace DakLakCoffeeSupplyChain.Repositories.Models;

public partial class ConversationParticipant
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public virtual Conversation Conversation { get; set; }

    public virtual UserAccount User { get; set; }
}