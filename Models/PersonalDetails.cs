using System;
using System.Collections.Generic;

namespace StudentERP.Models;

public partial class PersonalDetails
{
    public Guid StudentId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public virtual StudentLogin Student { get; set; } = null!;
}
