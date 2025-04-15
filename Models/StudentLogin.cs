using System;
using System.Collections.Generic;

namespace StudentERP.Models;

public partial class StudentLogin
{
    public Guid StudentId { get; set; }

    public string StudentMail { get; set; } = null!;

    public string HashPassword { get; set; } = null!;


    public Guid Token { get; set; }

    public bool IsActive { get; set; }

    public virtual AdmissionDetail? AdmissionDetail { get; set; }

    public virtual ContactDetail? ContactDetail { get; set; }

    public virtual ParentsDetails? ParentsDetail { get; set; }

    public virtual PersonalDetails? PersonalDetail { get; set; }

    public virtual StudentProfile? StudentProfile { get; set; }
}
