using System;
using System.Collections.Generic;

namespace StudentERP.Models;

public partial class StudentBatch
{
    public int BatchId { get; set; }

    public int BatchYear { get; set; }

    public string Period { get; set; } = null!;

    public string? ProfileStatus { get; set; }
}
