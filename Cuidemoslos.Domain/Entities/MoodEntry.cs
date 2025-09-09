using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuidemoslos.Domain.Entities;
public class MoodEntry
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int Score { get; set; } // 1..5
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

